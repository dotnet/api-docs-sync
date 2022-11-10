// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace ApiDocsSync.Libraries
{
    public class ResolvedProject
    {
        public ResolvedWorkspace ResolvedWorkspace { get; private set; }
        public Project Project { get; private set; }
        public Compilation Compilation { get; private set; }
        public string ProjectPath { get; private set; }
        public ResolvedProject(ResolvedWorkspace resolvedWorkspace, string projectPath, Project project, Compilation compilation)
        {
            ResolvedWorkspace = resolvedWorkspace;
            Project = project;
            Compilation = compilation;
            ProjectPath = projectPath;
        }
    }
}
