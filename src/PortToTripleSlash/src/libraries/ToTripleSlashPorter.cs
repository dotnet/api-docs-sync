// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ApiDocsSync.Libraries.Docs;
using ApiDocsSync.Libraries.RoslynTripleSlash;
using Microsoft.CodeAnalysis;

namespace ApiDocsSync.Libraries
{
    public class ToTripleSlashPorter
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
            CollectFiles();

            if (!_docsComments.Types.Any())
            {
                Log.Error("No Docs Type APIs found. Is the Docs xml path correct? Exiting.");
                Environment.Exit(0);
            }

            foreach (DocsType docsType in _docsComments.Types.Values)
            {
                Log.Info($"Looking for symbol locations for {docsType.TypeName}...");
                docsType.SymbolLocations = await CollectSymbolLocationsAsync(docsType.TypeName, cancellationToken).ConfigureAwait(false);
                Log.Info($"Finished looking for symbol locations for {docsType.TypeName}. Now attempting to port...");
                await PortAsync(docsType, throwOnSymbolsNotFound: false, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///  Collects the docs xml files.
        /// </summary>
        public void CollectFiles()
        {
            _docsComments.CollectFiles();
            if (!_docsComments.Types.Any())
            {
                throw new Exception("No docs type APIs found.");
            }
        }

        /// <summary>
        /// Iterates through all the xml files and collects symbols from the found projects for each one.
        /// </summary>
        public async Task MatchSymbolsAsync(bool throwOnSymbolsNotFound, CancellationToken cancellationToken)
        {
            Debug.Assert(_docsComments.Types.Any());
            Log.Info("Looking for symbol locations for all Docs types...");
            foreach (DocsType docsType in _docsComments.Types.Values)
            {
                docsType.SymbolLocations = await CollectSymbolLocationsAsync(docsType.TypeName, cancellationToken).ConfigureAwait(false);
                if (throwOnSymbolsNotFound)
                {
                    VerifySymbolLocations(docsType);
                }
            }
        }

        /// <summary>
        /// Iterates through all the found xml files and ports their documentation to the locations of all its found symbols.
        /// </summary>
        public async Task PortAsync(bool throwOnSymbolsNotFound, CancellationToken cancellationToken)
        {
            Debug.Assert(_docsComments.Types.Any());
            Log.Info($"Now attempting to port all found symbols...");
            foreach (DocsType docsType in _docsComments.Types.Values)
            {
                await PortAsync(docsType, throwOnSymbolsNotFound, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Ports the documentation of the specified xml file to the locations of all its found symbols.
        /// </summary>
        internal async Task PortAsync(DocsType docsType, bool throwOnSymbolsNotFound, CancellationToken cancellationToken)
        {
            if (throwOnSymbolsNotFound)
            {
                VerifySymbolLocations(docsType);
            }
            else if (docsType.SymbolLocations == null || !docsType.SymbolLocations.Any())
            {
                Log.Warning($"No symbols found for '{docsType.TypeName}'. Skipping.");
                return;
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

        private static void VerifySymbolLocations(DocsType docsType)
        {
            if (docsType.SymbolLocations == null)
            {
                throw new Exception($"Symbol locations null for '{docsType.TypeName}'.");
            }
            else if (!docsType.SymbolLocations.Any())
            {
                throw new Exception($"No symbols found for '{docsType.TypeName}'");
            }
        }

        private async Task<List<ResolvedLocation>?> CollectSymbolLocationsAsync(string docsTypeName, CancellationToken cancellationToken)
        {
            Debug.Assert(_config.Loader != null);
            ResolvedProject? mainProject = _config.Loader.MainProject;
            Debug.Assert(mainProject != null);

            // If the symbol is not found in the current compilation, nothing to do - It means the Docs
            // for APIs from an unrelated namespace were loaded for this compilation's assembly
            if (!TryGetNamedSymbol(mainProject.Compilation, docsTypeName, out INamedTypeSymbol? symbol))
            {
                Log.Info($"Type symbol '{docsTypeName}' not found in compilation for '{_config.CsProj}'.");
                return null;
            }

            // Make sure at least one syntax tree of this symbol can be found in the current project's compilation
            if (!symbol.Locations.Any())
            {
                throw new Exception($"The symbol for the type '{docsTypeName}' had no locations in '{_config.CsProj}'.");
            }

            Log.Cyan($"Type symbol '{docsTypeName}' found in compilation for '{_config.CsProj}'.");

            // Otherwise, port the exact same comments in each location
            List<ResolvedLocation> resolvedLocations = new();
            GetMatchingLocationsForSymbolInProject(resolvedLocations, mainProject, symbol.Locations, docsTypeName);

            Log.Info($"Also trying to find '{symbol.Name}' in the referenced projects of project '{_config.CsProj}'...");
            await FindSymbolInReferencedProjectsAsync(resolvedLocations, docsTypeName, mainProject.Project.ProjectReferences, cancellationToken).ConfigureAwait(false);

            return resolvedLocations;
        }

        private static void GetMatchingLocationsForSymbolInProject(List<ResolvedLocation> resolvedLocations, ResolvedProject resolvedProject, ImmutableArray<Location> symbolLocations, string docsTypeName)
        {
            int n = 0;
            foreach (Location location in symbolLocations)
            {
                if (location.SourceTree == null)
                {
                    Log.Error($"Location tree was null for {docsTypeName}. Skipping...");
                }
                else if (IsLocationTreeInCompilationTrees(location.SourceTree, resolvedProject.Compilation))
                {
                    Log.Info($"Symbol '{docsTypeName}' located in '{location.SourceTree.FilePath}'.");
                    if (resolvedLocations.Any(rl => rl.Tree.FilePath == location.SourceTree.FilePath))
                    {
                        Log.Info($"Tree '{location.SourceTree.FilePath}' was already added for symbol '{docsTypeName}'. Skipping.");
                    }
                    else
                    {
                        ResolvedLocation resolvedLocation = new(docsTypeName, resolvedProject, location, location.SourceTree);
                        Log.Success($"Adding tree '{resolvedLocation.Tree.FilePath}' for '{docsTypeName}'...");
                        resolvedLocations.Add(resolvedLocation);
                    }
                }
                else
                {
                    Log.Info(false, $"Symbol '{docsTypeName}' not found in compilation trees of '{location.SourceTree.FilePath}'.");
                    if (n < symbolLocations.Length)
                    {
                        Log.Info(true, " Trying the next location...");
                    }
                }
                n++;
            }
        }

        // Tries to find the specified type among the source code files of all the specified projects.
        // If not found, logs a warning message.
        private async Task FindSymbolInReferencedProjectsAsync(List<ResolvedLocation> resolvedLocations, string docsTypeName, IEnumerable<ProjectReference> projectReferences, CancellationToken cancellationToken)
        {
            int n = 0;
            foreach (ProjectReference projectReference in projectReferences)
            {
                string projectPath = GetProjectPath(projectReference);
                string projectNamespace = Path.GetFileNameWithoutExtension(projectPath);
                string projectNamespaceToUpper = projectNamespace.ToUpperInvariant();

                // Skip looking in projects whose namespace that were explicitly excluded or not explicitly included
                // The only exception is System.Private.CoreLib, which we should always explore
                if (projectNamespaceToUpper != _systemPrivateCoreLib)
                {
                    if (_config.ExcludedNamespaces.Any(x => x.StartsWith(projectNamespace)))
                    {
                        Log.Info($"Skipping project '{projectPath}' which was added to -ExcludedNamespaces.");
                        continue;
                    }
                    else if (!_config.IncludedNamespaces.Any(x => x.StartsWith(projectNamespace)))
                    {
                        Log.Info($"Skipping project '{projectPath}' which was not added to -IncludedNamespaces.");
                        continue;
                    }
                }

                (ResolvedProject? resolvedProject, INamedTypeSymbol? symbol) = await TryFindSymbolInReferencedProjectAsync(projectPath, docsTypeName, isMono: false, cancellationToken).ConfigureAwait(false);
                if (resolvedProject != null && symbol != null)
                {
                    Log.Cyan($"Symbol '{docsTypeName}' found in referenced project '{projectPath}'.");

                    // Do not look in referenced projects
                    Log.Info($"Looking for symbol '{symbol.Name}' in all locations of '{projectPath}'...");
                    GetMatchingLocationsForSymbolInProject(resolvedLocations, resolvedProject, symbol.Locations, docsTypeName);

                    string monoProjectPath = Regex.Replace(projectPath, @"src(?<separator>[\\\/]{1})coreclr", "src${separator}mono");

                    // If the symbol was found in corelib, try to also find it in mono
                    if (projectNamespaceToUpper == _systemPrivateCoreLib &&
                        projectPath.Contains(_pathSrcCoreclr))
                    {
                        (ResolvedProject? monoProject, INamedTypeSymbol? monoSymbol) = await TryFindSymbolInReferencedProjectAsync(monoProjectPath, docsTypeName, isMono: true, cancellationToken).ConfigureAwait(false);
                        if (monoProject != null && monoSymbol != null)
                        {
                            Log.Info($"Symbol '{monoSymbol.Name}' was also found in Mono locations of project '{monoProject.Project.FilePath}'.");
                            GetMatchingLocationsForSymbolInProject(resolvedLocations, monoProject, monoSymbol.Locations, docsTypeName);
                        }
                    }
                }
                else
                {
                    Log.Info(false, $"Symbol for '{docsTypeName}' not found in referenced project '{projectPath}'.");
                    if (n < projectReferences.Count())
                    {
                        Log.Info(true, $" Trying the next project...");
                    }
                }
                n++;
            }
        }

        // Checks if the specified tree can be found among the collection of trees of the specified compilation.
        private static bool IsLocationTreeInCompilationTrees(SyntaxTree tree, Compilation compilation) =>
            compilation.SyntaxTrees.FirstOrDefault(x => x.FilePath == tree.FilePath) is not null;

        // Tries to find the specified type among the source code files of the specified project.
        // Returns false if not found.
        private async Task<(ResolvedProject?, INamedTypeSymbol?)> TryFindSymbolInReferencedProjectAsync(
            string projectPath, string apiFullName, bool isMono, CancellationToken cancellationToken)
        {
            Debug.Assert(_config.Loader != null);
            ResolvedProject? project = await _config.Loader.LoadProjectAsync(projectPath, isMono, cancellationToken).ConfigureAwait(false);
            INamedTypeSymbol? symbol = null;
            if (project != null)
            {
                TryGetNamedSymbol(project.Compilation, apiFullName, out symbol);
            }
            return (project, symbol);
        }

        // Retrieves the location of the specified path using reflection.
        // There is no public API available to retrieve this information.
        private static string GetProjectPath(ProjectReference projectReference)
        {
            PropertyInfo prop = typeof(ProjectId).GetProperty("DebugName", BindingFlags.NonPublic | BindingFlags.Instance)
                            ?? throw new NullReferenceException("ProjectId.DebugName private property not found.");

            string projectPath = prop.GetValue(projectReference.ProjectId)?.ToString()
                ?? throw new NullReferenceException("ProjectId.DebugName value was null.");

            if (string.IsNullOrWhiteSpace(projectPath))
            {
                throw new Exception("Project path was empty.");
            }

            return projectPath;
        }

        // Tries to retrieve the specified symbol from the specified compilation.
        // Returns true if found.
        private static bool TryGetNamedSymbol(
            Compilation compilation,
            string symbolFullName,
            [NotNullWhen(returnValue: true)] out INamedTypeSymbol? actualSymbol)
        {
            // Try to find the symbol in the current compilation
            actualSymbol = compilation.GetTypeByMetadataName(symbolFullName) ??
                           compilation.Assembly.GetTypeByMetadataName(symbolFullName);

            return actualSymbol != null;
        }
    }
}
