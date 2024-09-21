// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.MSBuild;

namespace ApiDocsSync.PortToTripleSlash
{
    public class ResolvedWorkspace
    {
        public MSBuildWorkspace Workspace { get; }
        public List<ResolvedProject> ResolvedProjects { get; }
        public SyntaxGenerator Generator { get; }

        public ResolvedWorkspace(MSBuildWorkspace workspace)
        {
            Workspace = workspace;
            ResolvedProjects = new List<ResolvedProject>();
            Generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        }
    }
}
