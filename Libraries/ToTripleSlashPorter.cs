#nullable enable
using Libraries.Docs;
using Libraries.RoslynTripleSlash;
using Microsoft.Build.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Libraries
{
    public class ToTripleSlashPorter
    {
        private readonly Configuration Config;
        private readonly DocsCommentsContainer DocsComments;

        public ToTripleSlashPorter(Configuration config)
        {
            if (config.Direction != Configuration.PortingDirection.ToTripleSlash)
            {
                throw new InvalidOperationException($"Unexpected porting direction: {config.Direction}");
            }
            Config = config;
            DocsComments = new DocsCommentsContainer(config);
        }

        public void Start()
        {
            DocsComments.CollectFiles();
            if (!DocsComments.Types.Any())
            {
                Log.ErrorAndExit("No Docs Type APIs found.");
            }

            Log.Info("Porting from Docs to triple slash...");

            MSBuildWorkspace workspace;
            try
            {
                workspace = MSBuildWorkspace.Create();
            }
            catch (ReflectionTypeLoadException)
            {
                Log.ErrorAndExit("The MSBuild directory was not found in PATH. Use '-MSBuild <directory>' to specify it.");
                return;
            }

            BinaryLogger? binLogger = null;
            if (Config.BinLogger)
            {
                binLogger = new BinaryLogger()
                {
                    Parameters = Path.Combine(Environment.CurrentDirectory, Config.BinLogPath),
                    Verbosity = Microsoft.Build.Framework.LoggerVerbosity.Diagnostic,
                    CollectProjectImports = BinaryLogger.ProjectImportsCollectionMode.Embed
                };
            }

            Project? project = workspace.OpenProjectAsync(Config.CsProj!.FullName, msbuildLogger: binLogger).Result;
            if (project == null)
            {
                Log.ErrorAndExit("Could not find a project.");
                return;
            }

            Compilation? compilation = project.GetCompilationAsync().Result;
            if (compilation == null)
            {
                throw new NullReferenceException("The project's compilation was null.");
            }

            ImmutableList<WorkspaceDiagnostic> diagnostics = workspace.Diagnostics;
            if (diagnostics.Any())
            {
                foreach (var diagnostic in diagnostics)
                {
                    Log.Error($"{diagnostic.Kind} - {diagnostic.Message}");
                }
                Log.ErrorAndExit("Exiting due to diagnostic errors found.");
            }

            PortCommentsForAPIs(compilation!);
        }

        private void PortCommentsForAPIs(Compilation compilation)
        {
            foreach (DocsType docsType in DocsComments.Types)
            {
                INamedTypeSymbol? typeSymbol =
                    compilation.GetTypeByMetadataName(docsType.FullName) ??
                    compilation.Assembly.GetTypeByMetadataName(docsType.FullName);

                if (typeSymbol == null)
                {
                    Log.Warning($"Type symbol not found in compilation: {docsType.DocId}");
                    continue;
                }

                PortAPI(compilation, docsType, typeSymbol);
            }
        }

        private void PortAPI(Compilation compilation, IDocsAPI api, ISymbol symbol)
        {
            bool useBoilerplate = false;
            foreach (Location location in symbol.Locations)
            {
                SyntaxTree? tree = location.SourceTree;
                if (tree == null)
                {
                    Log.Warning($"Tree not found for location of {symbol.Name}");
                    continue;
                }

                SemanticModel model = compilation.GetSemanticModel(tree);
                var rewriter = new TripleSlashSyntaxRewriter(DocsComments, model, location, tree, useBoilerplate);
                SyntaxNode? newRoot = rewriter.Visit(tree.GetRoot());
                if (newRoot == null)
                {
                    Log.Warning($"New returned root is null for {api.DocId} in {tree.FilePath}");
                    continue;
                }

                File.WriteAllText(tree.FilePath, newRoot.ToFullString());
                useBoilerplate = true;
            }
        }
    }
}
