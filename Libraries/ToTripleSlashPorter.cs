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
        private struct ProjectData
        {
            public MSBuildWorkspace Workspace;
            public Project Project;
            public Compilation Compilation;
        }

        private struct SymbolData
        {
            public ProjectData ProjectData;
            public DocsType Api;
        }

        private readonly Configuration Config;
        private readonly DocsCommentsContainer DocsComments;
        private readonly VisualStudioInstance MSBuildInstance;

        private List<ProjectData> ProjectDatas = new();
#pragma warning disable RS1024 // Compare symbols correctly
        // Bug fixed https://github.com/dotnet/roslyn-analyzers/pull/4571
        private Dictionary<ISymbol, SymbolData> ResolvedSymbols = new();
#pragma warning restore RS1024 // Compare symbols correctly

        BinaryLogger? _binLogger = null;
        private BinaryLogger? BinLogger
        {
            get
            {
                if (Config.BinLogger)
                {
                    if (_binLogger == null)
                    {
                        _binLogger = new BinaryLogger()
                        {
                            Parameters = Path.Combine(Environment.CurrentDirectory, Config.BinLogPath),
                            Verbosity = Microsoft.Build.Framework.LoggerVerbosity.Diagnostic,
                            CollectProjectImports = BinaryLogger.ProjectImportsCollectionMode.Embed
                        };
                    }
                }

                return _binLogger;
            }
        }

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

            // Load and store the main project
            ProjectDatas.Add(GetProjectData(Config.CsProj!.FullName));

            foreach (DocsType docsType in DocsComments.Types)
            {
                foreach (ProjectData pd in ProjectDatas)
                {
                    // Try to find the symbol in the current compilation
                    INamedTypeSymbol? symbol =
                        pd.Compilation.GetTypeByMetadataName(docsType.FullName) ??
                        pd.Compilation.Assembly.GetTypeByMetadataName(docsType.FullName);

                    // If not found, nothing to do - It means that the Docs for APIs
                    // from an unrelated namespace were loaded for this compilation's assembly
                    if (symbol == null)
                    {
                        Log.Warning($"Type symbol not found in compilation: {docsType.DocId}.");
                        continue;
                    }

                    // Make sure at least one syntax tree of this symbol can be found in the current project's compilation
                    // Otherwise, retrieve the correct project where this symbol is supposed to be found
                    
                    Location location = symbol.Locations.FirstOrDefault()
                        ?? throw new NullReferenceException($"No locations found for {docsType.FullName}.");

                    SyntaxTree tree = location.SourceTree
                        ?? throw new NullReferenceException($"No tree found in the location of {docsType.FullName}.");

                    if (pd.Compilation.SyntaxTrees.FirstOrDefault(x => x.FilePath == tree.FilePath) is null)
                    {
                        // The symbol has to live in one of the current project's referenced projects
                        foreach (ProjectReference projectReference in pd.Project.ProjectReferences)
                        {
                            PropertyInfo prop = typeof(ProjectId).GetProperty("DebugName", BindingFlags.NonPublic | BindingFlags.Instance)
                                ?? throw new NullReferenceException("ProjectId.DebugName private property not found.");

                            string projectPath = prop.GetValue(projectReference.ProjectId)?.ToString()
                                ?? throw new NullReferenceException("ProjectId.DebugName value was null.");

                            if (string.IsNullOrWhiteSpace(projectPath))
                            {
                                throw new Exception("Project path was empty.");
                            }

                            // Can't reuse the existing Workspace or exception thrown saying we already have the project loaded in this workspace.
                            // Unfortunately, there is no way to retrieve a references project as a Project instance from the existing workspace.
                            ProjectData pd2 = GetProjectData(projectPath);
                            ProjectDatas.Add(pd2);
                            ResolvedSymbols.Add(symbol, new SymbolData { Api = docsType, ProjectData = pd2 });
                        }
                    }
                    else
                    {
                        ResolvedSymbols.Add(symbol, new SymbolData { Api = docsType, ProjectData = pd });
                    }
                }
            }


            foreach ((ISymbol symbol, SymbolData data) in ResolvedSymbols)
            {
                ProjectData t = data.ProjectData;
                foreach (Location location in symbol.Locations)
                {
                    SyntaxTree tree = location.SourceTree
                        ?? throw new NullReferenceException($"Tree null for {data.Api.FullName}");

                    SemanticModel model = t.Compilation.GetSemanticModel(tree);
                    TripleSlashSyntaxRewriter rewriter = new(DocsComments, model, location, location.SourceTree);
                    SyntaxNode newRoot = rewriter.Visit(tree.GetRoot())
                        ?? throw new NullReferenceException($"Returned null root node for {data.Api.FullName} in {tree.FilePath}");

                    File.WriteAllText(tree.FilePath, newRoot.ToFullString());
                }
            }

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

        private ProjectData GetProjectData(string csprojPath)
        {
            ProjectData t = new ProjectData();

            try
            {
                t.Workspace = MSBuildWorkspace.Create();
            }
            catch (ReflectionTypeLoadException)
            {
                Log.Error("The MSBuild directory was not found in PATH. Use '-MSBuild <directory>' to specify it.");
                throw;
            }

            CheckDiagnostics(t.Workspace, "MSBuildWorkspace.Create");

            t.Project = t.Workspace.OpenProjectAsync(csprojPath, msbuildLogger: BinLogger).Result
                ?? throw new NullReferenceException($"Could not find the project: {csprojPath}");

            CheckDiagnostics(t.Workspace, $"workspace.OpenProjectAsync - {csprojPath}");

            t.Compilation = t.Project.GetCompilationAsync().Result
                ?? throw new NullReferenceException("The project's compilation was null.");

            CheckDiagnostics(t.Workspace, $"project.GetCompilationAsync - {csprojPath}");

            return t;
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
