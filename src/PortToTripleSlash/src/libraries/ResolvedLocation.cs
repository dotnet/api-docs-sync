// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace ApiDocsSync.PortToTripleSlash
{
    public class ResolvedLocation
    {
        public string TypeName { get; }
        public Compilation Compilation { get; }
        public Location Location { get; }
        public SyntaxTree Tree { get; }
        public SemanticModel Model { get; }
        public SyntaxNode? NewNode { get; set; }

        public ResolvedLocation(string typeName, Compilation compilation, Location location, SyntaxTree tree)
        {
            TypeName = typeName;
            Compilation = compilation;
            Location = location;
            Tree = tree;
            Model = Compilation.GetSemanticModel(Tree);
        }
    }
}
