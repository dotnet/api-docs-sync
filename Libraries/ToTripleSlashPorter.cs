#nullable enable
using Libraries.Docs;
using Libraries.RoslynTripleSlash;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Libraries
{
    public class ToTripleSlashPorter
    {
        private readonly Configuration Config;
        private readonly DocsCommentsContainer DocsComments;
        private VisualStudioInstance MSBuildInstance;

        public ToTripleSlashPorter(Configuration config)
        {
            if (config.Direction != Configuration.PortingDirection.ToTripleSlash)
            {
                throw new InvalidOperationException($"Unexpected porting direction: {config.Direction}");
            }
            Config = config;
            DocsComments = new DocsCommentsContainer(config);

            // This ensures we can load MSBuild property before calling the ToTripleSlashPorter constructor
            MSBuildInstance = MSBuildLocator.QueryVisualStudioInstances().First();
            Register(MSBuildInstance.MSBuildPath);
            MSBuildLocator.RegisterInstance(MSBuildInstance);
        }

        public void Start()
        {
            DocsComments.CollectFiles();
            if (!DocsComments.Types.Any())
            {
                Log.Error("No Docs Type APIs found.");
            }

            Log.Info("Porting from Docs to triple slash...");

            MSBuildWorkspace workspace;
            try
            {
                workspace = MSBuildWorkspace.Create();
            }
            catch (ReflectionTypeLoadException)
            {
                throw new Exception("The MSBuild directory was not found in PATH. Use '-MSBuild <directory>' to specify it.");
            }

            CheckDiagnostics(workspace, "MSBuildWorkspace.Create");

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
                throw new Exception("Could not find a project.");
            }

            CheckDiagnostics(workspace, "workspace.OpenProjectAsync");

            Compilation? compilation = project.GetCompilationAsync().Result;
            if (compilation == null)
            {
                throw new NullReferenceException("The project's compilation was null.");
            }

            CheckDiagnostics(workspace, "project.GetCompilationAsync");

            PortCommentsForProject(compilation!);
        }

        private void CheckDiagnostics(MSBuildWorkspace workspace, string stepName)
        {
            ImmutableList<WorkspaceDiagnostic> diagnostics = workspace.Diagnostics;
            if (diagnostics.Any())
            {
                string initialMsg = $"Diagnostic messages found in {stepName}:";
                Log.Error(initialMsg);

                List<string> allMsgs = new() { initialMsg };

                foreach (var diagnostic in diagnostics)
                {
                    string msg = $"{diagnostic.Kind} - {diagnostic.Message}";
                    Log.Error(msg);

                    if (!msg.Contains("Warning - Found project reference without a matching metadata reference"))
                    {
                        allMsgs.Add(msg);
                    }
                }

                if (allMsgs.Count > 1)
                {
                    throw new Exception("Exiting due to diagnostic errors found: " + Environment.NewLine + string.Join(Environment.NewLine, allMsgs));
                }
            }
        }

        private void PortCommentsForProject(Compilation compilation)
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

                PortCommentsForType(compilation, docsType, typeSymbol);
            }
        }

        private void PortCommentsForType(Compilation compilation, IDocsAPI api, ISymbol symbol)
        {
            foreach (Location location in symbol.Locations)
            {
                SyntaxTree? tree = location.SourceTree;
                if (tree == null)
                {
                    Log.Warning($"Tree not found for location of {symbol.Name}");
                    continue;
                }

                SemanticModel model = compilation.GetSemanticModel(tree);
                var rewriter = new TripleSlashSyntaxRewriter(DocsComments, model, location, tree);
                SyntaxNode? newRoot = rewriter.Visit(tree.GetRoot());
                if (newRoot == null)
                {
                    Log.Warning($"New returned root is null for {api.DocId} in {tree.FilePath}");
                    continue;
                }

                File.WriteAllText(tree.FilePath, newRoot.ToFullString());
            }
        }

        #region MSBuild loading logic

        private static readonly Dictionary<string, Assembly> s_pathsToAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Assembly> s_namesToAssemblies = new Dictionary<string, Assembly>();

        private static readonly object s_guard = new object();

        /// <summary>
        /// Register an assembly loader that will load assemblies with higher version than what was requested.
        /// </summary>
        private static void Register(string searchPath)
        {
            AssemblyLoadContext.Default.Resolving += (AssemblyLoadContext context, AssemblyName assemblyName) =>
            {
                lock (s_guard)
                {
                    if (s_namesToAssemblies.TryGetValue(assemblyName.FullName, out var cachedAssembly))
                    {
                        return cachedAssembly;
                    }

                    var assembly = TryResolveAssemblyFromPaths(context, assemblyName, searchPath, s_pathsToAssemblies);

                    // Cache assembly
                    if (assembly != null)
                    {
                        var name = assembly.FullName;
                        if (name is null)
                        {
                            throw new Exception($"Could not get name for assembly '{assembly}'");
                        }

                        s_pathsToAssemblies[assembly.Location] = assembly;
                        s_namesToAssemblies[name] = assembly;
                    }

                    return assembly;
                }
            };
        }

        private static Assembly? TryResolveAssemblyFromPaths(AssemblyLoadContext context, AssemblyName assemblyName, string searchPath, Dictionary<string, Assembly>? knownAssemblyPaths = null)
        {
            foreach (var cultureSubfolder in string.IsNullOrEmpty(assemblyName.CultureName)
                // If no culture is specified, attempt to load directly from
                // the known dependency paths.
                ? new[] { string.Empty }
                // Search for satellite assemblies in culture subdirectories
                // of the assembly search directories, but fall back to the
                // bare search directory if that fails.
                : new[] { assemblyName.CultureName, string.Empty })
            {
                foreach (var extension in new[] { "ni.dll", "ni.exe", "dll", "exe" })
                {
                    var candidatePath = Path.Combine(searchPath, cultureSubfolder, $"{assemblyName.Name}.{extension}");

                    var isAssemblyLoaded = knownAssemblyPaths?.ContainsKey(candidatePath) == true;
                    if (isAssemblyLoaded || !File.Exists(candidatePath))
                    {
                        continue;
                    }

                    var candidateAssemblyName = AssemblyLoadContext.GetAssemblyName(candidatePath);
                    if (candidateAssemblyName.Version < assemblyName.Version)
                    {
                        continue;
                    }

                    try
                    {
                        var assembly = context.LoadFromAssemblyPath(candidatePath);
                        return assembly;
                    }
                    catch
                    {
                        if (assemblyName.Name != null)
                        {
                            // We were unable to load the assembly from the file path. It is likely that
                            // a different version of the assembly has already been loaded into the context.
                            // Be forgiving and attempt to load assembly by name without specifying a version.
                            return context.LoadFromAssemblyName(new AssemblyName(assemblyName.Name));
                        }
                    }
                }
            }

            return null;
        }

        #endregion

    }
}
