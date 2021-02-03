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
            public bool IsMono { get; private set; }
            public MSBuildWorkspace Workspace { get; private set; }
            public Project Project { get; private set; }
            public Compilation Compilation { get; private set; }

            public ProjectInformation(bool isMono, MSBuildWorkspace workspace, Project project, Compilation compilation)
            {
                IsMono = isMono;
                Workspace = workspace;
                Project = project;
                Compilation = compilation;
            }
        }

        private class SymbolInformation
        {
            public ProjectInformation ProjectInfo { get; private set; }
            public DocsType Api { get; private set; }

            public SymbolInformation(DocsType api, ProjectInformation projectInfo)
            {
                Api = api;
                ProjectInfo = projectInfo;
            }
        }

        private readonly Configuration Config;
        private readonly DocsCommentsContainer DocsComments;

#pragma warning disable RS1024 // Compare symbols correctly
        // Bug fixed https://github.com/dotnet/roslyn-analyzers/pull/4571
        private readonly Dictionary<INamedTypeSymbol, SymbolInformation> ResolvedSymbols = new();
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

            Log.Info("Porting from Docs to triple slash...");

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
                // Otherwise, retrieve the correct project where this symbol is supposed to be found

                Location location = symbol.Locations.FirstOrDefault()
                    ?? throw new NullReferenceException($"No locations found for {docsType.FullName}.");

                SyntaxTree? tree = location.SourceTree;
                if (tree == null)
                {
                    Log.Warning($"No tree found in the location of {docsType.FullName}. Skipping.");
                    continue;
                }

                // If the symbol's tree can't be found in the main project
                if (mainProjectInfo.Compilation.SyntaxTrees.FirstOrDefault(x => x.FilePath == tree.FilePath) is not null)
                {
                    var symbolInfo = new SymbolInformation(docsType, mainProjectInfo);
                    ResolvedSymbols.Add(symbol, symbolInfo);
                }
                // Then it should be located in one of the referenced projects
                else
                {
                    FindSymbolInReferencedProjects(docsType, mainProjectInfo.Project.ProjectReferences);
                }
            }

            PortResolvedSymbols();
        }

        private void FindSymbolInReferencedProjects(DocsType docsType, IEnumerable<ProjectReference> projectReferences)
        {
            foreach (ProjectReference projectReference in projectReferences)
            {
                string projectPath = GetProjectPath(projectReference);
                string projectFileName = Path.GetFileNameWithoutExtension(projectPath);


                // Can't reuse the existing Workspace or an exception is thrown saying we already have the project loaded in this workspace.
                // Unfortunately, there is no way to retrieve a references project as a Project instance from the existing workspace.

                ProjectInformation? pd = null;
                INamedTypeSymbol? symbol = null;

                if (TryFindSymbolInReferencedProject(projectPath, docsType.FullName, isMono: false, out pd, out symbol) ||
                    // If this is CoreLib, but the symbol was not found in the libraries runtime, try to find the symbol in mono
                    (projectFileName == "System.Private.CoreLib" &&
                     TryFindSymbolInReferencedProject(projectPath, docsType.FullName, isMono: true, out pd, out symbol)))
                {
                    var symbolInfo = new SymbolInformation(docsType, pd);
                    ResolvedSymbols.Add(symbol, symbolInfo);
                }
                else
                {
                    Log.Error($"Type symbol '{docsType.FullName}' not found in compilations for referenced project '{projectPath}'.");
                }

            }
        }

        private bool TryFindSymbolInReferencedProject(
            string projectPath,
            string apiFullName,
            bool isMono,
            [NotNullWhen(returnValue: true)] out ProjectInformation? pd,
            [NotNullWhen(returnValue: true)] out INamedTypeSymbol? symbol)
        {
            pd = null;
            symbol = null;
            try
            {
                pd = GetProjectInfo(projectPath, isMono: isMono);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }

            if (pd != null && TryGetNamedSymbol(pd.Compilation, apiFullName, out symbol))
            {
                Log.Success($"Symbol '{apiFullName}' found with mono={isMono} in '{projectPath}'");
                return true;
            }
            else
            {
                Log.Warning($"Symbol '{apiFullName}' not found with mono={isMono} in '{projectPath}'.");
            }

            return false;
        }

        private void PortResolvedSymbols()
        {
            foreach ((ISymbol symbol, SymbolInformation symbolInfo) in ResolvedSymbols)
            {
                foreach (Location location in symbol.Locations)
                {
                    SyntaxTree tree = location.SourceTree
                        ?? throw new NullReferenceException($"Tree null for {symbolInfo.Api.FullName}");

                    SemanticModel model = symbolInfo.ProjectInfo.Compilation.GetSemanticModel(tree);
                    TripleSlashSyntaxRewriter rewriter = new(DocsComments, model);
                    SyntaxNode newRoot = rewriter.Visit(tree.GetRoot())
                        ?? throw new NullReferenceException($"Returned null root node for {symbolInfo.Api.FullName} in {tree.FilePath}");

                    File.WriteAllText(tree.FilePath, newRoot.ToFullString());
                }
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

        private ProjectInformation GetProjectInfo(string csprojPath, bool isMono)
        {
            var workspaceProperties = new Dictionary<string, string>();

            if (isMono)
            {
                workspaceProperties.Add("RuntimeFlavor", "Mono");
            }

            MSBuildWorkspace workspace;
            try
            {
                workspace = MSBuildWorkspace.Create();
            }
            catch (ReflectionTypeLoadException)
            {
                Log.Error("The MSBuild directory was not found in PATH. Use '-MSBuild <directory>' to specify it.");
                throw;
            }

            CheckDiagnostics(workspace, $"MSBuildWorkspace.Create - isMono: {isMono}");

            Project project = workspace.OpenProjectAsync(csprojPath, msbuildLogger: BinLogger).Result
                ?? throw new NullReferenceException($"Could not find the project: {csprojPath}");

            CheckDiagnostics(workspace, $"workspace.OpenProjectAsync - {csprojPath}");

            Compilation compilation = project.GetCompilationAsync().Result
                ?? throw new NullReferenceException("The project's compilation was null.");

            CheckDiagnostics(workspace, $"project.GetCompilationAsync - {csprojPath}");

            var pd = new ProjectInformation(isMono, workspace, project, compilation);

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
            Register(vsBuildInstance.MSBuildPath);
            MSBuildLocator.RegisterInstance(vsBuildInstance);
            return vsBuildInstance;
        }

        // Register an assembly loader that will load assemblies with higher version than what was requested.
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
