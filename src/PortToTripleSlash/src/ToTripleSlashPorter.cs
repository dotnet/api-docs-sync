using DocsPortingTool.Libraries.Docs;
using DocsPortingTool.Libraries.RoslynTripleSlash;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

namespace DocsPortingTool.Libraries
{
    public class ToTripleSlashPorter
    {
        private class ProjectInformation
        {
            public Project Project { get; private set; }
            public Compilation Compilation { get; private set; }
            public bool IsMono { get; private set; }

            public ProjectInformation(Project project, Compilation compilation, bool isMono)
            {
                Project = project;
                Compilation = compilation;
                IsMono = isMono;
            }
        }

        private class LocationInformation
        {
            public DocsType Api { get; private set; }
            public SyntaxTree Tree { get; set; }
            public SemanticModel Model { get; set; }

            public LocationInformation(DocsType api, Location location, Compilation compilation)
            {
                Api = api;
                Tree = location.SourceTree ?? throw new NullReferenceException($"Tree null for '{api.FullName}'");
                Model = compilation.GetSemanticModel(Tree);
            }
        }

        private readonly Configuration Config;
        private readonly DocsCommentsContainer DocsComments;

#pragma warning disable RS1024 // Compare symbols correctly
        // Bug fixed https://github.com/dotnet/roslyn-analyzers/pull/4571
        private readonly Dictionary<string, LocationInformation> ResolvedLocations = new();
#pragma warning restore RS1024 // Compare symbols correctly

        private const string _allowedWarningMessage = "Found project reference without a matching metadata reference";
        private static readonly string _pathSrcCoreclr = Path.Combine("src", "coreclr");

        private static readonly string SystemPrivateCoreLib = "SYSTEM.PRIVATE.CORELIB";

        private static readonly Dictionary<string, string> WorkspaceProperties = new() { { "RuntimeFlavor", "Mono" } };

        private static readonly Dictionary<string,MSBuildWorkspace> Workspaces = new();
        private static readonly Dictionary<string, MSBuildWorkspace> WorkspacesMono = new();

        private static readonly Dictionary<string, Project> Projects = new();
        private static readonly Dictionary<string, Compilation> Compilations = new();

        private static readonly Dictionary<string, Project> ProjectsMono = new();
        private static readonly Dictionary<string, Compilation> CompilationsMono = new();

        // If enabled via CLI arguments, a binlog file will be generated when running this tool.
        BinaryLogger? _binLogger = null;
        private BinaryLogger? BinLogger
        {
            get
            {
                if (Config.BinLogger)
                {
                    if (_binLogger == null)
                    {
                        Log.Info("Enabling the collection of a binlog file...");
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

        // Initializes a new porter instance with the specified configuration.
        private ToTripleSlashPorter(Configuration config)
        {
            Config = config;
            DocsComments = new DocsCommentsContainer(config);
        }

        /// <summary>
        /// Starts the porting process using the specified CLI arguments.
        /// This includes:
        /// - Loading a Visual Studio instance.
        /// - Loading the assembly loader.
        /// - Creating an instance of this porter.
        /// - Loading the Docs xmls.
        /// - Linding the source code for each one of the loaded xml types.
        /// - Porting the xml docs to triple slash.
        /// </summary>
        /// <param name="config">The configuration collected from the CLI arguments.</param>
        public static void Start(Configuration config)
        {
            // IMPORTANT: Need to load the MSBuild property BEFORE calling the ToTripleSlashPorter constructor.
            LoadVSInstance();

            var porter = new ToTripleSlashPorter(config);
            porter.Port();
        }

        // Collects the Docs xml files, the source code files, and ports the xml comments to triple slash.
        private void Port()
        {
            DocsComments.CollectFiles();
            if (!DocsComments.Types.Any())
            {
                Log.Error("No Docs Type APIs found. Is the Docs xml path correct? Exiting.");
                Environment.Exit(0);
            }
            Log.Info("Reading source code projects...");

            // Load and store the main project
            ProjectInformation mainProjectInfo = GetProjectInfo(Config.CsProj!.FullName, isMono: false);

            foreach (DocsType docsType in DocsComments.Types.Values)
            {
                // If the symbol is not found in the current compilation, nothing to do - It means the Docs
                // for APIs from an unrelated namespace were loaded for this compilation's assembly
                if (!TryGetNamedSymbol(mainProjectInfo.Compilation, docsType.TypeName, out INamedTypeSymbol? symbol))
                {
                    Log.Info($"Type symbol '{docsType.FullName}' not found in compilation for '{Config.CsProj.FullName}'.");
                    continue;
                }

                // Make sure at least one syntax tree of this symbol can be found in the current project's compilation
                if (!symbol.Locations.Any())
                {
                    throw new NullReferenceException($"The symbol for the type '{docsType.FullName}' had no locations in '{Config.CsProj.FullName}'.");
                }

                Log.Cyan($"Type symbol '{docsType.FullName}' found in compilation for '{Config.CsProj.FullName}'.");

                // Otherwise, port the exact same comments in each location
                AddSymbolLocationsToResolvedLocations(mainProjectInfo, symbol, docsType);

                Log.Info($"Also trying to find '{symbol.Name}' in the referenced projects of project '{Config.CsProj.FullName}'...");
                FindSymbolInReferencedProjects(docsType, mainProjectInfo.Project.ProjectReferences);
            }

            PortDocsForResolvedSymbols();
        }

        private void AddSymbolLocationsToResolvedLocations(ProjectInformation projectInfo, INamedTypeSymbol symbol, DocsType docsType)
        {
            int n = 0;
            foreach (Location location in symbol.Locations)
            {
                string path = location.SourceTree != null ? location.SourceTree.FilePath : location.ToString();
                if (IsLocationTreeInCompilationTrees(location, projectInfo.Compilation))
                {
                    Log.Info($"Symbol '{symbol.Name}' found in location '{path}'.");
                    var info = new LocationInformation(docsType, location, projectInfo.Compilation);
                    AddToResolvedSymbols(info);
                }
                else
                {
                    Log.Info(false, $"Symbol '{symbol.Name}' not found in locations of project '{path}'.");
                    if (n < symbol.Locations.Count())
                    {
                        Log.Info(true, " Trying the next location...");
                    }
                }
                n++;
            }
        }

        // Tries to find the specified type among the source code files of all the specified projects.
        // If not found, logs a warning message.
        private void FindSymbolInReferencedProjects(DocsType docsType, IEnumerable<ProjectReference> projectReferences)
        {
            int n = 0;
            foreach (ProjectReference projectReference in projectReferences)
            {
                string projectPath = GetProjectPath(projectReference);
                string projectNamespace = Path.GetFileNameWithoutExtension(projectPath);
                string projectNamespaceToUpper = projectNamespace.ToUpperInvariant();

                // Skip looking in projects whose namespace that were explicitly excluded or not explicitly included
                // The only exception is System.Private.CoreLib, which we should always explore
                if (projectNamespaceToUpper != SystemPrivateCoreLib)
                {
                    if (Config.ExcludedNamespaces.Any(x => x.StartsWith(projectNamespace)))
                    {
                        Log.Info($"Skipping project '{projectPath}' which was added to -ExcludedNamespaces.");
                        continue;
                    }
                    else if (!Config.IncludedNamespaces.Any(x => x.StartsWith(projectNamespace)))
                    {
                        Log.Info($"Skipping project '{projectPath}' which was not added to -IncludedNamespaces.");
                        continue;
                    }
                }

                if (TryFindSymbolInReferencedProject(projectPath, docsType.TypeName, isMono: false, out ProjectInformation? projectInfo, out INamedTypeSymbol? symbol))
                {
                    Log.Cyan($"Symbol '{docsType.FullName}' found in referenced project '{projectPath}'.");

                    // Do not look in referenced projects
                    Log.Info($"Looking for symbol '{symbol.Name}' in all locations of '{projectPath}'...");
                    AddSymbolLocationsToResolvedLocations(projectInfo, symbol, docsType);

                    string monoProjectPath = Regex.Replace(projectPath, @"src(?<separator>[\\\/]{1})coreclr", "src${separator}mono");

                    // If the symbol was found in corelib, try to also find it in mono
                    if (projectNamespaceToUpper == SystemPrivateCoreLib &&
                        projectPath.Contains(_pathSrcCoreclr) &&
                        TryFindSymbolInReferencedProject(monoProjectPath, docsType.TypeName, isMono: true, out ProjectInformation? monoProjectInfo, out INamedTypeSymbol? monoSymbol))
                    {
                        Log.Info($"Symbol '{monoSymbol.Name}' was also found in Mono locations of project '{monoProjectInfo.Project.FilePath}'.");
                        AddSymbolLocationsToResolvedLocations(monoProjectInfo, monoSymbol, docsType);
                    }
                }
                else
                {
                    Log.Info(false, $"Symbol for '{docsType.FullName}' not found in referenced project '{projectPath}'.");
                    if (n < projectReferences.Count())
                    {
                        Log.Info(true, $" Trying the next project...");
                    }
                }
                n++;
            }
        }

        // Checks if the specified tree can be found among the collection of trees of the specified compilation.
        private bool IsLocationTreeInCompilationTrees(Location location, Compilation compilation)
        {
            return location.SourceTree is not null &&
                   compilation.SyntaxTrees.FirstOrDefault(x => x.FilePath == location.SourceTree.FilePath) is not null;
        }

        // Adds the specified SymbolInformation object to the ResolvedSymbols dictionary, if it has not yet been added.
        private void AddToResolvedSymbols(LocationInformation info)
        {
            string key = info.Tree.FilePath; // This ensures we have a unique key for symbols that are also found in mono
            if (!ResolvedLocations.TryAdd(key, info))
            {
                Log.Info($"Skipping symbol tree already added for '{key}'.");
            }
            else
            {
                Log.Success($"Symbol tree added for '{key}'.");
            }
        }

        // Tries to find the specified type among the source code files of the specified project.
        // Returns false if not found.
        private bool TryFindSymbolInReferencedProject(
            string projectPath,
            string apiFullName,
            bool isMono,
            [NotNullWhen(returnValue: true)] out ProjectInformation? pi,
            [NotNullWhen(returnValue: true)] out INamedTypeSymbol? symbol)
        {
            symbol = null;
            return TryGetProjectInfo(projectPath, isMono: isMono, out pi) &&
                   TryGetNamedSymbol(pi.Compilation, apiFullName, out symbol);
        }

        // Copies the Docs xml documentation of all the found symbols to their respective source code locations.
        private void PortDocsForResolvedSymbols()
        {
            Log.Info("Porting comments from Docs to triple slash...");
            foreach ((string filePath, LocationInformation info) in ResolvedLocations)
            {
                Log.Info($"Porting docs for '{filePath}'...");
                TripleSlashSyntaxRewriter rewriter = new(DocsComments, info.Model);
                SyntaxNode newRoot = rewriter.Visit(info.Tree.GetRoot())
                    ?? throw new NullReferenceException($"Returned null root node for {info.Api.FullName} in {info.Tree.FilePath}");

                File.WriteAllText(info.Tree.FilePath, newRoot.ToFullString());
                Log.Success($"Docs ported to '{filePath}'.");
            }
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
            actualSymbol =
                compilation.GetTypeByMetadataName(symbolFullName) ??
                compilation.Assembly.GetTypeByMetadataName(symbolFullName);

            return actualSymbol != null;
        }

        // Tries to retrieve the project info for the specified path.
        // Returns true if found. Logs a warning message and returns false otherwise.
        private bool TryGetProjectInfo(
            string projectPath,
            bool isMono,
            [NotNullWhen(returnValue: true)] out ProjectInformation? pi)
        {
            pi = null;
            try
            {
                pi = GetProjectInfo(projectPath, isMono);
                return true;
            }
            catch (Exception e)
            {
                Log.DarkYellow(e.Message);
            }
            return false;
        }

        // Retrieves the project info for the specified path.
        // If any diagnostic error messages are captured after each step (workspace load, project load, compilation load), an exception is thrown.
        private ProjectInformation GetProjectInfo(string projectPath, bool isMono)
        {
            MSBuildWorkspace workspace = GetOrAddWorkspace(
                isMono ? WorkspacesMono : Workspaces,
                isMono ? WorkspaceProperties : null,
                projectPath);

            Project project = GetOrAddProject(
                workspace,
                isMono ? ProjectsMono : Projects,
                projectPath);

            Compilation compilation = GetOrAddCompilation(
                workspace,
                project,
                isMono ? CompilationsMono : Compilations,
                projectPath);

            var pd = new ProjectInformation(project, compilation, isMono);

            return pd;
        }

        private MSBuildWorkspace GetOrAddWorkspace(Dictionary<string, MSBuildWorkspace> workspaces, Dictionary<string, string>? properties, string projectPath)
        {
            MSBuildWorkspace workspace;

            if (!workspaces.ContainsKey(projectPath))
            {
                if (properties == null)
                {
                    workspace = MSBuildWorkspace.Create();
                }
                else
                {
                    workspace = MSBuildWorkspace.Create(properties);
                }
                workspace.AssociateFileExtensionWithLanguage("ilproj", LanguageNames.CSharp);
                CheckDiagnostics(workspace, $"MSBuildWorkspace.Create" + properties == null ? "" : " - Mono");
                workspaces[projectPath] = workspace;
            }
            else
            {
                workspace = workspaces[projectPath];
            }

            return workspace;
        }

        private Project GetOrAddProject(MSBuildWorkspace workspace, Dictionary<string, Project> projects, string projectPath)
        {
            Project project;
            if (!projects.ContainsKey(projectPath))
            {
                project = workspace.OpenProjectAsync(projectPath, msbuildLogger: BinLogger).Result
                    ?? throw new NullReferenceException($"Could not find the project: {projectPath}");
                Log.Info($"Opened the project '{projectPath}'.");

                CheckDiagnostics(workspace, $"workspace.OpenProjectAsync - {projectPath}");

                projects[projectPath] = project;
            }
            else
            {
                project = projects[projectPath];
            }
            return project;
        }

        private Compilation GetOrAddCompilation(MSBuildWorkspace workspace, Project project, Dictionary<string, Compilation> compilations, string projectPath)
        {
            Compilation compilation;
            if (!compilations.ContainsKey(projectPath))
            {
                compilation = project.GetCompilationAsync().Result
                    ?? throw new NullReferenceException("The project's compilation was null.");
                Log.Info($"Obtained the compilation for '{projectPath}'.");

                CheckDiagnostics(workspace, $"project.GetCompilationAsync - {projectPath}");

                compilations[projectPath] = compilation;
            }
            else
            {
                compilation = compilations[projectPath];
            }
            return compilation;
        }

        // If the workspace captured any error diagnostic messages, prints all of them and then throws.
        private static void CheckDiagnostics(MSBuildWorkspace workspace, string stepName)
        {
            ImmutableList<WorkspaceDiagnostic> diagnostics = workspace.Diagnostics;
            if (diagnostics.Any())
            {
                var allMsgs = new List<string>();

                foreach (var diagnostic in diagnostics)
                {
                    if (!diagnostic.Message.Contains(_allowedWarningMessage))
                    {
                        allMsgs.Add($"    {diagnostic.Kind} - {diagnostic.Message}");
                    }
                    else
                    {
                        Log.Info($"Acceptable diagnostic message found in '{stepName}': {diagnostic.Kind} - {diagnostic.Message}");
                    }
                }

                if (allMsgs.Count > 0)
                {
                    Log.Error($"Diagnostic messages found in {stepName}:");
                    foreach (string msg in allMsgs)
                    {
                        Log.Error(msg);
                    }

                    throw new Exception("Diagnostic errors found.");
                }
            }
        }

        #region MSBuild loading logic

        private static readonly Dictionary<string, Assembly> s_pathsToAssemblies = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Assembly> s_namesToAssemblies = new();

        private static readonly object s_guard = new();

        // Loads the external VS instance using the correct MSBuild dependency, which differs from the one used by this process.
        public static VisualStudioInstance LoadVSInstance()
        {
            var vsBuildInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();

            // https://github.com/carlossanlop/DocsPortingTool/issues/69
            // Prefer the latest stable instance if there is one
            var instance = vsBuildInstances
                .Where(b => !b.MSBuildPath.Contains("-preview")).OrderByDescending(b => b.Version).FirstOrDefault() ??
                vsBuildInstances.First();

            // Unit tests execute this multiple times
            // Ensure we only register once
            if (MSBuildLocator.CanRegister)
            {
                RegisterAssemblyLoader(instance.MSBuildPath);
                MSBuildLocator.RegisterInstance(instance);
            }

            return instance;
        }

        // Register an assembly loader that will load assemblies with higher version than what was requested.
        private static void RegisterAssemblyLoader(string searchPath)
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

        // Tries to find and return the specified assembly by looking in all the known locations where it could be found.
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
