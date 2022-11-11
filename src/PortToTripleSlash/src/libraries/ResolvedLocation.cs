// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace ApiDocsSync.PortToTripleSlash
{
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
