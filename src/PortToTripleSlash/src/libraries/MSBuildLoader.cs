// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Build.Logging;
using System.IO;

namespace ApiDocsSync.Libraries
{
    // Per the documentation: https://docs.microsoft.com/en-us/visualstudio/msbuild/updating-an-existing-application
    // Do not call any of these APIs from the same context where MSBuildLocator is being called.
    public class MSBuildLoader
    {
        private const string AllowedWarningMessage = "Found project reference without a matching metadata reference";

        private readonly Dictionary<string, string> _monoWorkspaceProperties = new() { { "RuntimeFlavor", "Mono" } };

        private BinaryLogger? _binLog = null;
        public BinaryLogger? BinLog
        {
            get
            {
                if (!string.IsNullOrEmpty(BinLogPath))
                {
                    if (_binLog == null)
                    {
                        Log.Info($"Enabling the collection of a binlog file: {BinLogPath}");
                        _binLog = new BinaryLogger()
                        {
                            Parameters = Path.Combine(Environment.CurrentDirectory, BinLogPath),
                            Verbosity = LoggerVerbosity.Diagnostic,
                            CollectProjectImports = BinaryLogger.ProjectImportsCollectionMode.Embed
                        };
                    }
                }

                return _binLog;
            }
        }

        public string? BinLogPath { get; private set; }

        public List<ResolvedWorkspace> ResolvedWorkspaces { get; private set; } = new();

        public ResolvedProject? MainProject { get; private set; }

        public MSBuildLoader(string? binLogPath)
        {
            BinLogPath = binLogPath;
        }

        public async Task LoadMainProjectAsync(string projectPath, bool isMono, CancellationToken cancellationToken)
        {
            MainProject = await LoadProjectAsync(projectPath, isMono, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ResolvedProject> LoadProjectAsync(string projectPath, bool isMono, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Debug.Assert(projectPath != null);
            if (!TryGetResolvedWorkspace(projectPath, isMono, out ResolvedWorkspace? resolvedWorkspace))
            {
                throw new Exception("Workspace not created.");
            }
            ThrowIfDiagnosticsFound(resolvedWorkspace, $"MSBuildWorkspace.Create - {projectPath}", isMono);

            ResolvedProject? resolvedProject = await TryGetResolvedProjectAsync(resolvedWorkspace, projectPath, cancellationToken).ConfigureAwait(false);
            if (resolvedProject == null)
            {
                throw new Exception("Project not created.");
            }
            ThrowIfDiagnosticsFound(resolvedWorkspace, $"Project.OpenProjectAsync - {projectPath}", isMono);

            return resolvedProject;
        }

        private bool TryGetResolvedWorkspace(string projectPath, bool isMono, [NotNullWhen(returnValue: true)] out ResolvedWorkspace? resolvedWorkspace)
        {
            ResolvedProject? resolvedProject = FindResolvedProjectInResolvedWorkspaces(projectPath);

            if (resolvedProject == null)
            {
                Log.Info($"Did not find an existing resolved project for path {projectPath}{(isMono ? " (Mono)" : "")}. Creating a workspace for it...");
                MSBuildWorkspace msBuildWorkspace = isMono ? MSBuildWorkspace.Create(_monoWorkspaceProperties) : MSBuildWorkspace.Create();
                msBuildWorkspace.AssociateFileExtensionWithLanguage("ilproj", LanguageNames.CSharp);
                resolvedWorkspace = new ResolvedWorkspace(msBuildWorkspace);
                ResolvedWorkspaces.Add(resolvedWorkspace);
            }
            else
            {
                Log.Info($"Found existing resolved project for path {projectPath}. Returning its workspace...");
                resolvedWorkspace = resolvedProject.ResolvedWorkspace;
            }

            return resolvedWorkspace != null;
        }

        private ResolvedProject? FindResolvedProjectInResolvedWorkspaces(string projectPath)
        {
            Log.Info($"Looking for a resolved workspace that contains the project '{projectPath}'...");
            foreach (ResolvedWorkspace resolvedWorkspace in ResolvedWorkspaces)
            {
                foreach (ResolvedProject resolvedProject in resolvedWorkspace.ResolvedProjects)
                {
                    if (resolvedProject.Project.FilePath == projectPath)
                    {
                        return resolvedProject;
                    }
                }
            }
            return null;
        }

        private async Task<ResolvedProject?> TryGetResolvedProjectAsync(ResolvedWorkspace resolvedWorkspace, string projectPath,  CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Log.Info($"Looking for a resolved project that contains the project '{projectPath}'...");
            ResolvedProject? resolvedProject = resolvedWorkspace.ResolvedProjects.SingleOrDefault(p => p.Project.FilePath == projectPath);
            if (resolvedProject == null)
            {
                Log.Info($"Did not find an existing resolved project for path '{projectPath}'. Attempting to find the project in this workspace...");
                Project project = await resolvedWorkspace.Workspace.OpenProjectAsync(projectPath, BinLog, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (project != null)
                {
                    Log.Info($"Found the project in this workspace. Attempting to get compilation...");
                    Compilation? compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                    if (compilation != null)
                    {
                        Log.Info($"Found the compilation for this project. Creating the resolved project...");
                        resolvedProject = new ResolvedProject(resolvedWorkspace, project, compilation);
                        resolvedWorkspace.ResolvedProjects.Add(resolvedProject);
                    }
                    else
                    {
                        Log.Error($"Did not find a compilation for this project.");
                    }
                }
                else
                {
                    Log.Error($"Could not find the project in this workspace.");
                }
            }

            return resolvedProject;
        }

        private static void ThrowIfDiagnosticsFound(ResolvedWorkspace resolvedWorkspace, string origin, bool isMono)
        {
            if (resolvedWorkspace.Workspace.Diagnostics.Any())
            {
                List<string> throwableErrors = new();

                foreach (WorkspaceDiagnostic diagnostic in resolvedWorkspace.Workspace.Diagnostics)
                {
                    if (!diagnostic.Message.Contains(AllowedWarningMessage))
                    {
                        throwableErrors.Add($"    {diagnostic.Kind} - {diagnostic.Message}");
                    }
                }

                if (throwableErrors.Any())
                {
                    string message = $"{(isMono ? "Mono:" : "")}{Environment.NewLine}{origin}{Environment.NewLine}{string.Join(Environment.NewLine, throwableErrors)}";
                    throw new Exception(message);
                }
            }
        }
    }

    public class ResolvedWorkspace
    {
        public MSBuildWorkspace Workspace { get; private set; }
        public List<ResolvedProject> ResolvedProjects { get; }
        public ResolvedWorkspace(MSBuildWorkspace workspace)
        {
            Workspace = workspace;
            ResolvedProjects = new List<ResolvedProject>();
        }
    }

    public class ResolvedProject
    {
        public ResolvedWorkspace ResolvedWorkspace { get; private set; }
        public Project Project { get; private set; }
        public Compilation Compilation { get; private set; }
        public ResolvedProject(ResolvedWorkspace resolvedWorkspace, Project project, Compilation compilation)
        {
            ResolvedWorkspace = resolvedWorkspace;
            Project = project;
            Compilation = compilation;
        }
    }

    public class ResolvedLocation
    {
        public string TypeName { get; private set; }
        public ResolvedProject ResolvedProject { get; private set; }
        public Location Location { get; private set; }
        public SyntaxTree Tree { get; set; }
        public SemanticModel Model { get; set; }
        public ResolvedLocation(string typeName, ResolvedProject resolvedProject, Location location, SyntaxTree tree)
        {
            TypeName = typeName;
            ResolvedProject = resolvedProject;
            Location = location;
            Tree = tree;
            Model = resolvedProject.Compilation.GetSemanticModel(Tree);
        }
    }
}
