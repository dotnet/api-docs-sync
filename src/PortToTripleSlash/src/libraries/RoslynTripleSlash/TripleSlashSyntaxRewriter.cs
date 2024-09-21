// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ApiDocsSync.PortToTripleSlash.Docs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.Net.Mime.MediaTypeNames;

namespace ApiDocsSync.PortToTripleSlash.Roslyn;

/*
 * According to the Roslyn Quoter: https://roslynquoter.azurewebsites.net/
 * This code:

public class MyClass
{
    /// <summary>MySummary</summary>
    /// <param name="x">MyParameter</param>
    public void MyMethod(int x) { }
}

 * Can be generated using:

SyntaxFactory.CompilationUnit()
.WithMembers(
    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
        SyntaxFactory.ClassDeclaration("MyClass")
        .WithModifiers(
            SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
        .WithMembers(
            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.PredefinedType(
                        SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    SyntaxFactory.Identifier("MyMethod"))
                .WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(
                            SyntaxFactory.TriviaList(
                                SyntaxFactory.Trivia(
                                    SyntaxFactory.DocumentationCommentTrivia(
                                        SyntaxKind.SingleLineDocumentationCommentTrivia,
                                        SyntaxFactory.List<XmlNodeSyntax>(
                                            new XmlNodeSyntax[]{
                                                SyntaxFactory.XmlText()
                                                .WithTextTokens(
                                                    SyntaxFactory.TokenList(
                                                        SyntaxFactory.XmlTextLiteral(
                                                            SyntaxFactory.TriviaList(
                                                                SyntaxFactory.DocumentationCommentExterior("///")),
                                                            " ",
                                                            " ",
                                                            SyntaxFactory.TriviaList()))),
                                                SyntaxFactory.XmlExampleElement(
                                                    SyntaxFactory.SingletonList<XmlNodeSyntax>(
                                                        SyntaxFactory.XmlText()
                                                        .WithTextTokens(
                                                            SyntaxFactory.TokenList(
                                                                SyntaxFactory.XmlTextLiteral(
                                                                    SyntaxFactory.TriviaList(),
                                                                    "MySummary",
                                                                    "MySummary",
                                                                    SyntaxFactory.TriviaList())))))
                                                .WithStartTag(
                                                    SyntaxFactory.XmlElementStartTag(
                                                        SyntaxFactory.XmlName(
                                                            SyntaxFactory.Identifier("summary"))))
                                                .WithEndTag(
                                                    SyntaxFactory.XmlElementEndTag(
                                                        SyntaxFactory.XmlName(
                                                            SyntaxFactory.Identifier("summary")))),
                                                SyntaxFactory.XmlText()
                                                .WithTextTokens(
                                                    SyntaxFactory.TokenList(
                                                        new []{
                                                            SyntaxFactory.XmlTextNewLine(
                                                                SyntaxFactory.TriviaList(),
                                                                "\n",
                                                                "\n",
                                                                SyntaxFactory.TriviaList()),
                                                            SyntaxFactory.XmlTextLiteral(
                                                                SyntaxFactory.TriviaList(
                                                                    SyntaxFactory.DocumentationCommentExterior("    ///")),
                                                                " ",
                                                                " ",
                                                                SyntaxFactory.TriviaList())})),
                                                SyntaxFactory.XmlExampleElement(
                                                    SyntaxFactory.SingletonList<XmlNodeSyntax>(
                                                        SyntaxFactory.XmlText()
                                                        .WithTextTokens(
                                                            SyntaxFactory.TokenList(
                                                                SyntaxFactory.XmlTextLiteral(
                                                                    SyntaxFactory.TriviaList(),
                                                                    "MyParameter",
                                                                    "MyParameter",
                                                                    SyntaxFactory.TriviaList())))))
                                                .WithStartTag(
                                                    SyntaxFactory.XmlElementStartTag(
                                                        SyntaxFactory.XmlName(
                                                            SyntaxFactory.Identifier(
                                                                SyntaxFactory.TriviaList(),
                                                                SyntaxKind.ParamKeyword,
                                                                "param",
                                                                "param",
                                                                SyntaxFactory.TriviaList())))
                                                    .WithAttributes(
                                                        SyntaxFactory.SingletonList<XmlAttributeSyntax>(
                                                            SyntaxFactory.XmlNameAttribute(
                                                                SyntaxFactory.XmlName(
                                                                    SyntaxFactory.Identifier("name")),
                                                                SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
                                                                SyntaxFactory.IdentifierName("x"),
                                                                SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken)))))
                                                .WithEndTag(
                                                    SyntaxFactory.XmlElementEndTag(
                                                        SyntaxFactory.XmlName(
                                                            SyntaxFactory.Identifier(
                                                                SyntaxFactory.TriviaList(),
                                                                SyntaxKind.ParamKeyword,
                                                                "param",
                                                                "param",
                                                                SyntaxFactory.TriviaList())))),
                                                SyntaxFactory.XmlText()
                                                .WithTextTokens(
                                                    SyntaxFactory.TokenList(
                                                        SyntaxFactory.XmlTextNewLine(
                                                            SyntaxFactory.TriviaList(),
                                                            "\n",
                                                            "\n",
                                                            SyntaxFactory.TriviaList())))})))),
                            SyntaxKind.PublicKeyword,
                            SyntaxFactory.TriviaList())))
                .WithParameterList(
                    SyntaxFactory.ParameterList(
                        SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(
                            SyntaxFactory.Parameter(
                                SyntaxFactory.Identifier("x"))
                            .WithType(
                                SyntaxFactory.PredefinedType(
                                    SyntaxFactory.Token(SyntaxKind.IntKeyword))))))
                .WithBody(
                    SyntaxFactory.Block())))))
.NormalizeWhitespace()
*/

internal class TripleSlashSyntaxRewriter : CSharpSyntaxRewriter
{
    private DocsCommentsContainer DocsComments { get; }
    private ResolvedLocation Location { get; }
    private SemanticModel Model => Location.Model;

    public TripleSlashSyntaxRewriter(DocsCommentsContainer docsComments, ResolvedLocation resolvedLocation) : base(visitIntoStructuredTrivia: false)
    {
        DocsComments = docsComments;
        Location = resolvedLocation;
    }

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node) => VisitType(node, base.VisitClassDeclaration(node));

    public override SyntaxNode? VisitDelegateDeclaration(DelegateDeclarationSyntax node) => VisitType(node, base.VisitDelegateDeclaration(node));

    public override SyntaxNode? VisitEnumDeclaration(EnumDeclarationSyntax node) => VisitType(node, base.VisitEnumDeclaration(node));

    public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) => VisitType(node, base.VisitInterfaceDeclaration(node));

    public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node) => VisitType(node, base.VisitRecordDeclaration(node));

    public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node) => VisitType(node, base.VisitStructDeclaration(node));

    public override SyntaxNode? VisitEventFieldDeclaration(EventFieldDeclarationSyntax node) => VisitVariableDeclaration(node, base.VisitEventFieldDeclaration(node));

    public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node) => VisitVariableDeclaration(node, base.VisitFieldDeclaration(node));

    public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node) => VisitBaseMethodDeclaration(node, base.VisitConstructorDeclaration(node));

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node) => VisitBaseMethodDeclaration(node, base.VisitMethodDeclaration(node));

    // TODO: Add test
    public override SyntaxNode? VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node) => VisitBaseMethodDeclaration(node, base.VisitConversionOperatorDeclaration(node));

    // TODO: Add test
    public override SyntaxNode? VisitIndexerDeclaration(IndexerDeclarationSyntax node) => VisitBaseMethodDeclaration(node, base.VisitIndexerDeclaration(node));

    public override SyntaxNode? VisitOperatorDeclaration(OperatorDeclarationSyntax node) => VisitBaseMethodDeclaration(node, base.VisitOperatorDeclaration(node));

    public override SyntaxNode? VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node) => VisitMemberDeclaration(node, base.VisitEnumMemberDeclaration(node));

    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node) => VisitBasePropertyDeclaration(node, base.VisitPropertyDeclaration(node));

    private SyntaxNode? VisitType(SyntaxNode originalNode, SyntaxNode? baseNode)
    {
        if (!TryGetType(originalNode, out DocsType? type) || baseNode == null)
        {
            return originalNode;
        }
        return Generate(baseNode, type);
    }

    private SyntaxNode? VisitBaseMethodDeclaration(SyntaxNode originalNode, SyntaxNode? baseNode)
    {
        // The Docs files only contain docs for public elements,
        // so if no comments are found, we return the node unmodified
        if (!TryGetMember(originalNode, out DocsMember? member) || baseNode == null)
        {
            return originalNode;
        }
        return Generate(baseNode, member);
    }

    private SyntaxNode? VisitBasePropertyDeclaration(SyntaxNode originalNode, SyntaxNode? baseNode)
    {
        if (!TryGetMember(originalNode, out DocsMember? member) || baseNode == null)
        {
            return originalNode;
        }
        return Generate(baseNode, member);
    }

    private SyntaxNode? VisitMemberDeclaration(SyntaxNode originalNode, SyntaxNode? baseNode)
    {
        if (!TryGetMember(originalNode, out DocsMember? member) || baseNode == null)
        {
            return originalNode;
        }
        return Generate(baseNode, member);
    }

    private SyntaxNode? VisitVariableDeclaration(SyntaxNode originalNode, SyntaxNode? baseNode)
    {
        if (!TryGetMember(originalNode, out DocsMember? member) || baseNode == null)
        {
            return originalNode;
        }

        return Generate(baseNode, member);
    }

    private bool TryGetMember(SyntaxNode originalNode, [NotNullWhen(returnValue: true)] out DocsMember? member)
    {
        member = null;

        SyntaxNode nodeWithSymbol;
        if (originalNode is BaseFieldDeclarationSyntax fieldDecl)
        {
            // Special case: fields could be grouped in a single line if they all share the same data type
            if (!IsPublic(fieldDecl))
            {
                return false;
            }

            VariableDeclarationSyntax variableDecl = fieldDecl.Declaration;
            if (variableDecl.Variables.Count != 1) // TODO: Add test
            {
                // Only port docs if there is only one variable in the declaration
                return false;
            }

            nodeWithSymbol = variableDecl.Variables.First();
        }
        else
        {
            // All members except enum values can have visibility modifiers
            if (originalNode is not EnumMemberDeclarationSyntax && !IsPublic(originalNode))
            {
                return false;
            }

            nodeWithSymbol = originalNode;
        }
        

        if (Model.GetDeclaredSymbol(nodeWithSymbol) is ISymbol symbol)
        {
            string? docId = symbol.GetDocumentationCommentId();
            if (!string.IsNullOrWhiteSpace(docId))
            {
                DocsComments.Members.TryGetValue(docId, out member);
            }
        }

        return member != null;
    }

    private bool TryGetType(SyntaxNode originalNode, [NotNullWhen(returnValue: true)] out DocsType? type)
    {
        type = null;

        if (originalNode == null || !IsPublic(originalNode))
        {
            return false;
        }

        if (Model.GetDeclaredSymbol(originalNode) is ISymbol symbol)
        {
            string? docId = symbol.GetDocumentationCommentId();
            if (!string.IsNullOrWhiteSpace(docId))
            {
                DocsComments.Types.TryGetValue(docId, out type);
            }
        }

        return type != null;
    }

    private static bool IsPublic([NotNullWhen(returnValue: true)] SyntaxNode? node) =>
        node != null &&
        node is MemberDeclarationSyntax baseNode &&
        baseNode.Modifiers.Any(t => t.IsKind(SyntaxKind.PublicKeyword));

    public SyntaxNode Generate(SyntaxNode node, IDocsAPI api)
    {
        List<SyntaxTrivia> updatedLeadingTrivia = new();

        bool replacedExisting = false;
        SyntaxTriviaList leadingTrivia = node.GetLeadingTrivia();

        SyntaxTrivia? indentationTrivia = leadingTrivia.Count > 0 ? leadingTrivia.Last(x => x.IsKind(SyntaxKind.WhitespaceTrivia)) : null;

        DocumentationUpdater updater = new(DocsComments.Config, api, indentationTrivia);

        for (int index = 0; index < leadingTrivia.Count; index++)
        {
            SyntaxTrivia originalTrivia = leadingTrivia[index];

            if (index == leadingTrivia.Count - 1)
            {
                // Skip the last one because it will be added at the end
                break;
            }

            if (originalTrivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                // Avoid re-adding existing whitespace trivia, it will always be added later
                continue;
            }

            if (!originalTrivia.HasStructure)
            {
                // Double slash comments do not have a structure but must be preserved with the original indentation
                // Only add indentation if the current trivia is not a new line
                if ((SyntaxKind)originalTrivia.RawKind != SyntaxKind.EndOfLineTrivia && indentationTrivia.HasValue)
                {
                    updatedLeadingTrivia.Add(indentationTrivia.Value);
                }
                updatedLeadingTrivia.Add(originalTrivia);
                
                continue;
            }

            SyntaxNode? structuredTrivia = originalTrivia.GetStructure();
            Debug.Assert(structuredTrivia != null);

            if (!structuredTrivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
            {
                // Unsure if there are other structured comments, but must preserve them with the original indentation
                if (indentationTrivia.HasValue)
                {
                    updatedLeadingTrivia.Add(indentationTrivia.Value);
                }
                updatedLeadingTrivia.Add(originalTrivia);
                continue;
            }

            // We know there is at least one xml element
            SyntaxList<XmlNodeSyntax> existingDocs = ((DocumentationCommentTriviaSyntax)structuredTrivia).Content;
            SyntaxTriviaList triviaList = SyntaxFactory.TriviaList(SyntaxFactory.Trivia(updater.GetUpdatedDocs(existingDocs)));
            updatedLeadingTrivia.AddRange(triviaList);

            replacedExisting = true;
        }

        // Either there was no pre-existing trivia or there were no
        // existing triple slash, so it must be built from scratch
        if (!replacedExisting)
        {
            SyntaxTriviaList triviaList = SyntaxFactory.TriviaList(SyntaxFactory.Trivia(updater.GetNewDocs()));
            updatedLeadingTrivia.AddRange(triviaList);
        }

        // The last trivia is the spacing before the actual node (usually before the visibility keyword)
        // must be replaced in its original location
        if (indentationTrivia.HasValue)
        {
            updatedLeadingTrivia.Add(indentationTrivia.Value);
        }

        return node.WithLeadingTrivia(updatedLeadingTrivia);
    }
}
