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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Libraries
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

        private class SymbolInformation
        {
            public DocsType Api { get; private set; }
            public SyntaxTree Tree { get; set; }
            public SemanticModel Model { get; set; }

            public SymbolInformation(DocsType api, Location location, Compilation compilation)
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
        private readonly Dictionary<string, SymbolInformation> ResolvedSymbols = new();
#pragma warning restore RS1024 // Compare symbols correctly

        private const string _allowedWarningMessage = "Found project reference without a matching metadata reference";

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

        private ToTripleSlashPorter(Configuration config)
        {
            if (config.Direction != Configuration.PortingDirection.ToTripleSlash)
            {
                throw new InvalidOperationException($"Unexpected porting direction: {config.Direction}");
            }

            Config = config;
            DocsComments = new DocsCommentsContainer(config);
        }

        public static void Start(Configuration config)
        {
            // IMPORTANT: Need to load the MSBuild property before calling the ToTripleSlashPorter constructor.
            LoadVSInstance();

            var porter = new ToTripleSlashPorter(config);
            porter.Port();
        }

        private void Port()
        {
            DocsComments.CollectFiles();
            if (!DocsComments.Types.Any())
            {
                Log.Error("No Docs Type APIs found.");
            }

            Log.Info("Reading source code projects...");

            // Load and store the main project
            ProjectInformation mainProjectInfo = GetProjectInfo(Config.CsProj!.FullName, isMono: false);

            foreach (DocsType docsType in DocsComments.Types)
            {
                // If the symbol is not found in the current compilation, nothing to do - It means the Docs
                // for APIs from an unrelated namespace were loaded for this compilation's assembly
                if (!TryGetNamedSymbol(mainProjectInfo.Compilation, docsType.FullName, out INamedTypeSymbol? symbol))
                {
                    Log.Warning($"Type symbol '{docsType.FullName}' not found in compilation for '{Config.CsProj!.FullName}'.");
                    continue;
                }

                // Make sure at least one syntax tree of this symbol can be found in the current project's compilation
                if (!symbol.Locations.Any())
                {
                    throw new NullReferenceException($"No locations found for {docsType.FullName}.");
                }

                // Otherwise, port the exact same comments in each location
                foreach (Location location in symbol.Locations)
                {
                    if (IsLocationTreeInCompilationTrees(location, mainProjectInfo.Compilation))
                    {
                        Log.Info($"Found '{symbol.Name}' in '{location}'.");
                        var symbolInfo = new SymbolInformation(docsType, location, mainProjectInfo.Compilation);
                        AddToResolvedSymbols(symbolInfo);
                    }
                    // Then it should be located in one of the referenced projects
                    else
                    {
                        Log.DarkYellow($"Symbol '{docsType.FullName}' not found in main project. Looking in referenced projects...");
                        FindSymbolInReferencedProjects(docsType, mainProjectInfo.Project.ProjectReferences);
                    }
                }
            }

            PortResolvedSymbols();
        }

        private void FindSymbolInReferencedProjects(DocsType docsType, IEnumerable<ProjectReference> projectReferences)
        {
            foreach (ProjectReference projectReference in projectReferences)
            {
                string projectPath = GetProjectPath(projectReference);

                // Can't reuse the existing Workspace or an exception is thrown saying we already have the project loaded in this workspace.
                // Unfortunately, there is no way to retrieve a references project as a Project instance from the existing workspace.

                if (TryFindSymbolInReferencedProject(projectPath, docsType.FullName, out ProjectInformation? pi, out INamedTypeSymbol? symbol))
                {
                    foreach (Location location in symbol.Locations)
                    {
                        if (IsLocationTreeInCompilationTrees(location, pi.Compilation))
                        {
                            Log.Info($"Found '{symbol.Name}' in '{location}'.");
                            var symbolInfo = new SymbolInformation(docsType, location, pi.Compilation);
                            AddToResolvedSymbols(symbolInfo);
                        }
                        // Stop here, instead of attempting looking in referenced projects
                        else
                        {
                            Log.DarkYellow($"Tree for '{docsType.FullName}' not found in referenced project '{pi.Project.FilePath}'. Skipping.");
                        }
                    }
                }
                else
                {
                    Log.DarkYellow($"Symbol '{docsType.FullName}' not found. Skipping.");
                }

            }
        }

        private bool IsLocationTreeInCompilationTrees(Location location, Compilation compilation)
        {
            return location.SourceTree is not null &&
                   compilation.SyntaxTrees.FirstOrDefault(x => x.FilePath == location.SourceTree.FilePath) is not null;
        }

        private void AddToResolvedSymbols(SymbolInformation symbolInfo)
        {
            string key = symbolInfo.Tree.FilePath;
            if (!ResolvedSymbols.ContainsKey(symbolInfo.Tree.FilePath))
            {
                ResolvedSymbols.Add(key, symbolInfo);
            }
            else
            {
                Log.DarkYellow($"Symbol tree had already been added for '{key}'.");
            }
        }

        private bool TryFindSymbolInReferencedProject(
            string projectPath,
            string apiFullName,
            [NotNullWhen(returnValue: true)] out ProjectInformation? pi,
            [NotNullWhen(returnValue: true)] out INamedTypeSymbol? symbol)
        {
            symbol = null;

            if (TryGetProjectInfo(projectPath, isMono: false, out pi) &&
                TryGetNamedSymbol(pi.Compilation, apiFullName, out symbol))
            {
                Log.Success($"Symbol '{apiFullName}' found in '{projectPath}'");
                return true;
            }
            // If this is CoreLib, but the symbol was not found in the libraries runtime, try to find the symbol in mono
            else if (Path.GetFileNameWithoutExtension(projectPath) == "System.Private.CoreLib" &&
                     TryGetProjectInfo(projectPath, isMono: true, out pi) &&
                     TryGetNamedSymbol(pi.Compilation, apiFullName, out symbol))
            {
                Log.Success($"Symbol '{apiFullName}' found as Mono in '{projectPath}'");
                return true;
            }

            return false;
        }

        private void PortResolvedSymbols()
        {
            Log.Info("Porting comments from Docs to triple slash...");

            foreach ((string filePath, SymbolInformation info) in ResolvedSymbols)
            {
                Log.Info($"Porting comments for '{filePath}'...");
                TripleSlashSyntaxRewriter rewriter = new(DocsComments, info.Model);
                SyntaxNode newRoot = rewriter.Visit(info.Tree.GetRoot())
                    ?? throw new NullReferenceException($"Returned null root node for {info.Api.FullName} in {info.Tree.FilePath}");

                File.WriteAllText(info.Tree.FilePath, newRoot.ToFullString());
            }
        }

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
                        Log.Magenta($"{stepName} - {diagnostic.Kind} - {diagnostic.Message}");
                    }
                }

                if (allMsgs.Count > 1)
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

        private ProjectInformation GetProjectInfo(string projectPath, bool isMono)
        {
            var workspaceProperties = new Dictionary<string, string>();

            // If project has implementations in mono,
            // we need to port docs to mono-specific locations too
            if (isMono)
            {
                workspaceProperties.Add("RuntimeFlavor", "Mono");
            }

            MSBuildWorkspace workspace;
            try
            {
                workspace = MSBuildWorkspace.Create();
            }
            catch
            {
                Log.Error("The MSBuild directory was not found in PATH. Use '-MSBuild <directory>' to specify it.");
                throw;
            }

            // Prevents exception when trying to load C# projects that are not csproj
            workspace.AssociateFileExtensionWithLanguage("ilproj", LanguageNames.CSharp);

            CheckDiagnostics(workspace, $"MSBuildWorkspace.Create - isMono: {isMono}");

            Project project = workspace.OpenProjectAsync(projectPath, msbuildLogger: BinLogger).Result
                ?? throw new NullReferenceException($"Could not find the project: {projectPath}");
           
            CheckDiagnostics(workspace, $"workspace.OpenProjectAsync - {projectPath}");

            Compilation compilation = project.GetCompilationAsync().Result
                ?? throw new NullReferenceException("The project's compilation was null.");

            CheckDiagnostics(workspace, $"project.GetCompilationAsync - {projectPath}");

            var pd = new ProjectInformation(project, compilation, isMono);

            return pd;
        }

        #region MSBuild loading logic

        private static readonly Dictionary<string, Assembly> s_pathsToAssemblies = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Assembly> s_namesToAssemblies = new();

        private static readonly object s_guard = new();

        // Loads the external VS instance using the correct MSBuild dependency, which differs from the one used by this process.
        public static VisualStudioInstance LoadVSInstance()
        {
            VisualStudioInstance vsBuildInstance = MSBuildLocator.QueryVisualStudioInstances().First();
            RegisterAssemblyLoader(vsBuildInstance.MSBuildPath);
            MSBuildLocator.RegisterInstance(vsBuildInstance);
            return vsBuildInstance;
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
