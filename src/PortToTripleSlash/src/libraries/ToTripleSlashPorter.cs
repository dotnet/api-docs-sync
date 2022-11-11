// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ApiDocsSync.Libraries.Docs;
using ApiDocsSync.Libraries.RoslynTripleSlash;
using Microsoft.CodeAnalysis;

namespace ApiDocsSync.Libraries
{
    public partial class ToTripleSlashPorter
    {
        private readonly string _pathSrcCoreclr = Path.Combine("src", "coreclr");
        private readonly string _systemPrivateCoreLib = "SYSTEM.PRIVATE.CORELIB";

        private readonly Configuration _config;
        private readonly DocsCommentsContainer _docsComments;

        // Initializes a new porter instance with the specified configuration.
        public ToTripleSlashPorter(Configuration config)
        {
            _config = config;
            _docsComments = new DocsCommentsContainer(config);
        }

        /// <summary>
        /// Performs the full porting process:
        /// - Collects the docs xml files.
        /// - For every xml file, collects the symbols from the found projects.
        /// - Ports the DocsType documentation to every symbol found.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryCollectFiles())
            {
                Log.Error("No docs files found.");
                return;
            }
            await MatchSymbolsAsync(cancellationToken).ConfigureAwait(false);
            await PortAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///  Collects the docs xml files.
        /// </summary>
        public bool TryCollectFiles()
        {
            _docsComments.CollectFiles();
            return _docsComments.Types.Any();
        }

        /// <summary>
        /// Iterates through all the xml files and collects symbols from the found projects for each one.
        /// </summary>
        public async Task MatchSymbolsAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(_docsComments.Types.Any());
            cancellationToken.ThrowIfCancellationRequested();

            Log.Info("Looking for symbol locations for all Docs types...");
            foreach (DocsType docsType in _docsComments.Types.Values)
            {
                await CollectSymbolLocationsAsync(docsType, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Iterates through all the found xml files and ports their documentation to the locations of all its found symbols.
        /// </summary>
        public async Task PortAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(_docsComments.Types.Any());
            cancellationToken.ThrowIfCancellationRequested();

            Log.Info($"Now attempting to port all found symbols...");
            foreach (DocsType docsType in _docsComments.Types.Values)
            {
                if (docsType.SymbolLocations == null || !docsType.SymbolLocations.Any())
                {
                    Log.Warning($"No symbols found for '{docsType.DocId}'. Skipping.");
                    continue;
                }

                Log.Cyan($"Porting comments for '{docsType.TypeName}'. Locations: {docsType.SymbolLocations!.Count}...");
                foreach (ResolvedLocation resolvedLocation in docsType.SymbolLocations)
                {
                    Log.Info($"Porting docs for tree '{resolvedLocation.Tree.FilePath}'...");
                    TripleSlashSyntaxRewriter rewriter = new(_docsComments, resolvedLocation.Model);
                    SyntaxNode? newRoot = rewriter.Visit(resolvedLocation.Tree.GetRoot(cancellationToken));
                    if (newRoot == null)
                    {
                        throw new Exception($"Returned null root node for {docsType.TypeName} in {resolvedLocation.Tree.FilePath}");
                    }

                    await File.WriteAllTextAsync(resolvedLocation.Tree.FilePath, newRoot.ToFullString(), cancellationToken).ConfigureAwait(false);
                    Log.Success($"Docs ported to '{docsType.TypeName}'.");
                }
            }
        }

        private async Task CollectSymbolLocationsAsync(DocsType docsType, CancellationToken cancellationToken)
        {
            Debug.Assert(_config.Loader != null);
            Debug.Assert(_config.Loader.MainProject != null);
            cancellationToken.ThrowIfCancellationRequested();

            docsType.SymbolLocations = new List<ResolvedLocation>();

            FindLocationsOfSymbolInResolvedProject(docsType, _config.Loader.MainProject);

            foreach (ProjectReference projectReference in _config.Loader.MainProject.Project.ProjectReferences)
            {
                if (TryGetProjectPath(projectReference, out string? referencedProjectPath, out string? referencedProjectNamespace))
                {
                    await FindLocationsOfSymbolInProjectPathAsync(docsType, referencedProjectPath, cancellationToken).ConfigureAwait(false);

                    // If the symbol was found in corelib, try to also find it in mono
                    if (referencedProjectNamespace == _systemPrivateCoreLib && referencedProjectPath.Contains(_pathSrcCoreclr))
                    {
                        string monoProjectPath = MonoProjectPathReplacementRegex().Replace(referencedProjectPath, "src${separator}mono");
                        await FindLocationsOfSymbolInProjectPathAsync(docsType, monoProjectPath, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            if (!docsType.SymbolLocations.Any())
            {
                Log.Error($"No symbols found for docs type '{docsType.DocId}'.");
            }
        }

        private bool TryGetProjectPath(ProjectReference projectReference, [NotNullWhen(returnValue: true)] out string? projectPath, [NotNullWhen(returnValue: true)] out string? projectNamespace)
        {
            projectNamespace = null;

            projectPath = typeof(ProjectId).GetProperty("DebugName", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(projectReference.ProjectId)?.ToString();

            if (projectPath != null)
            {
                string ns = Path.GetFileNameWithoutExtension(projectPath).ToUpperInvariant();
                projectNamespace = ns;

                // Skip looking in projects whose namespace that were explicitly excluded or not explicitly included
                // The only exception is System.Private.CoreLib, which we should always explore
                if (projectNamespace != _systemPrivateCoreLib)
                {
                    if (!_config.IncludedNamespaces.Any(x => x.ToUpperInvariant().StartsWith(ns)) ||
                         _config.ExcludedNamespaces.Any(x => x.ToUpperInvariant().StartsWith(ns)))
                    {
                        Log.Warning($"Skipping project '{projectPath}' because namespace '{projectNamespace}' was not added to -IncludedNamespaces or was added to -ExcludedNamespaces.");
                        projectPath = null;
                        projectNamespace = null;
                    }
                }
            }

            return projectPath != null;
        }

        private async Task FindLocationsOfSymbolInProjectPathAsync(DocsType docsType, string projectPath, CancellationToken cancellationToken)
        {
            Debug.Assert(_config.Loader != null);
            cancellationToken.ThrowIfCancellationRequested();

            ResolvedProject project = await _config.Loader.LoadProjectAsync(projectPath, isMono: false, cancellationToken).ConfigureAwait(false);

            FindLocationsOfSymbolInResolvedProject(docsType, project);
        }

        private static void FindLocationsOfSymbolInResolvedProject(DocsType docsType, ResolvedProject project)
        {
            // First, collect all types in the current compilation
            AllTypesVisitor visitor = new();
            visitor.Visit(project.Compilation.GlobalNamespace);

            // Next, filter types that match the current docsType
            IEnumerable<ISymbol> currentTypeSymbols = visitor.AllTypesSymbols.Where(x => x != null && x.GetDocumentationCommentId() == docsType.DocId);
            if (!currentTypeSymbols.Any())
            {
                throw new Exception($"The symbol for the type '{docsType.DocId}' had no locations in '{project.ProjectPath}'.");
            }

            foreach (ISymbol symbol in currentTypeSymbols)
            {
                docsType.SymbolLocations = GetSymbolLocations(project, symbol);
            }
        }

        private static List<ResolvedLocation> GetSymbolLocations(ResolvedProject resolvedProject, ISymbol symbol)
        {
            List<ResolvedLocation> resolvedLocations = new();

            int n = 0;
            string docId = symbol.GetDocumentationCommentId() ?? throw new NullReferenceException($"DocID was null for symbol '{symbol}'");
            foreach (Location location in symbol.Locations)
            {
                SyntaxTree? tree = location.SourceTree;
                if (tree == null)
                {
                    Log.Error($"Location tree was null for {docId}. Skipping...");
                }
                // Verify that this location tree is among the compilation trees
                else if (resolvedProject.Compilation.SyntaxTrees.FirstOrDefault(x => x.FilePath == tree.FilePath) is not null)
                {
                    Log.Info($"Symbol '{docId}' located in '{tree.FilePath}'.");
                    if (resolvedLocations.Any(rl => rl.Tree.FilePath == tree.FilePath))
                    {
                        Log.Info($"Tree '{tree.FilePath}' was already added for symbol '{docId}'. Skipping.");
                    }
                    else
                    {
                        ResolvedLocation resolvedLocation = new(docId, resolvedProject, location, tree);
                        Log.Success($"Adding tree '{resolvedLocation.Tree.FilePath}' for '{docId}'...");
                        resolvedLocations.Add(resolvedLocation);
                    }
                }
                else
                {
                    Log.Info(false, $"Symbol '{docId}' not found in compilation trees of '{tree.FilePath}'.");
                    if (n < symbol.Locations.Length)
                    {
                        Log.Info(true, " Trying the next location...");
                    }
                }
                n++;
            }

            return resolvedLocations;
        }

        [GeneratedRegex("src(?<separator>[\\\\\\/]{1})coreclr")]
        private static partial Regex MonoProjectPathReplacementRegex();
    }
}
