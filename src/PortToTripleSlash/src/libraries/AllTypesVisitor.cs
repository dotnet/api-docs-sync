// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace ApiDocsSync.Libraries
{

    internal class AllTypesVisitor : SymbolVisitor
    {
        public readonly List<ISymbol> AllTypesSymbols = new();
        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            AllTypesSymbols.Add(symbol);
            foreach (INamedTypeSymbol typeMember in symbol.GetTypeMembers())
            {
                Visit(typeMember);
            }
        }
        public override void VisitNamespace(INamespaceSymbol symbol) => Parallel.ForEach(symbol.GetMembers(), s => s.Accept(this));
        public override void VisitDynamicType(IDynamicTypeSymbol symbol) => AllTypesSymbols.Add(symbol);
        public override void VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol) => AllTypesSymbols.Add(symbol);
        public override void VisitAlias(IAliasSymbol symbol) => AllTypesSymbols.Add(symbol);
    }
}
