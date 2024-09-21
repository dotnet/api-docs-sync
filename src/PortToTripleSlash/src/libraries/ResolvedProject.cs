// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace ApiDocsSync.PortToTripleSlash
{
    public class ResolvedProject
    {
        public ResolvedWorkspace ResolvedWorkspace { get; }
        public Project Project { get; }
        public Compilation Compilation { get; }
        public string ProjectPath { get; }

        public ResolvedProject(ResolvedWorkspace resolvedWorkspace, string projectPath, Project project, Compilation compilation)
        {
            ResolvedWorkspace = resolvedWorkspace;
            Project = project;
            Compilation = compilation;
            ProjectPath = projectPath;
        }
    }
}
