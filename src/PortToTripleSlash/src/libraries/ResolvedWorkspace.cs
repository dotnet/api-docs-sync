// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.MSBuild;

namespace ApiDocsSync.Libraries
{
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
}
