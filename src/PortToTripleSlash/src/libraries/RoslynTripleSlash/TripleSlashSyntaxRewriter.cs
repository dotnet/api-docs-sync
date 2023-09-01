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

namespace ApiDocsSync.PortToTripleSlash.Roslyn
{
    internal class TripleSlashSyntaxRewriter : CSharpSyntaxRewriter
    {
        private const string SummaryTag = "summary";
        private const string ValueTag = "value";
        private const string TypeParamTag = "typeparam";
        private const string ParamTag = "param";
        private const string ReturnsTag = "returns";
        private const string RemarksTag = "remarks";
        private const string ExceptionTag = "exception";
        private const string NameAttributeName = "name";
        private const string CrefAttributeName = "cref";
        private const string TripleSlash = "///";
        private const string Space = " ";
        private const string NewLine = "\n";

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
                DocumentationCommentTriviaSyntax documentationCommentTrivia = (DocumentationCommentTriviaSyntax)structuredTrivia;

                SyntaxList<SyntaxNode> updatedNodeList = GetOrCreateXmlNodes(api, documentationCommentTrivia.Content, indentationTrivia, DocsComments.Config.SkipRemarks);

                Debug.Assert(updatedNodeList.Any());

                DocumentationCommentTriviaSyntax updatedDocComments = SyntaxFactory.DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia, updatedNodeList);

                updatedLeadingTrivia.Add(SyntaxFactory.Trivia(updatedDocComments));

                replacedExisting = true;
            }

            // Either there was no pre-existing trivia or there were no
            // existing triple slash, so it must be built from scratch
            if (!replacedExisting)
            {
                updatedLeadingTrivia.Add(CreateXmlSectionFromScratch(api, indentationTrivia));
            }

            // The last trivia is the spacing before the actual node (usually before the visibility keyword)
            // must be replaced in its original location
            if (indentationTrivia.HasValue)
            {
                updatedLeadingTrivia.Add(indentationTrivia.Value);
            }

            return node.WithLeadingTrivia(updatedLeadingTrivia);
        }

        private SyntaxTrivia CreateXmlSectionFromScratch(IDocsAPI api, SyntaxTrivia? indentationTrivia)
        {
            // TODO: Add all the empty items needed for this API and wrap them in their expected greater items
            SyntaxList<SyntaxNode> newNodeList = GetOrCreateXmlNodes(api, SyntaxFactory.List<XmlNodeSyntax>(), indentationTrivia, DocsComments.Config.SkipRemarks);

            DocumentationCommentTriviaSyntax newDocComments = SyntaxFactory.DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia, newNodeList);

            return SyntaxFactory.Trivia(newDocComments);
        }

        internal static SyntaxList<SyntaxNode> GetOrCreateXmlNodes(IDocsAPI api, SyntaxList<XmlNodeSyntax> originalXmls, SyntaxTrivia? indentationTrivia, bool skipRemarks)
        {
            List<SyntaxNode> updated = new();

            if(TryGetOrCreateXmlNode(originalXmls, SummaryTag, api.Summary, attributeValue: null, out XmlNodeSyntax? summaryNode, out _))
            {
                updated.AddRange(GetXmlRow(summaryNode, indentationTrivia));
            }

           if (TryGetOrCreateXmlNode(originalXmls, ValueTag, api.Value, attributeValue: null, out XmlNodeSyntax? valueNode, out _))
            {
                updated.AddRange(GetXmlRow(valueNode, indentationTrivia));
            }

            foreach (DocsTypeParam typeParam in api.TypeParams)
            {
                if (TryGetOrCreateXmlNode(originalXmls, TypeParamTag, typeParam.Value, attributeValue: typeParam.Name,  out XmlNodeSyntax? typeParamNode, out _))
                {
                    updated.AddRange(GetXmlRow(typeParamNode, indentationTrivia));
                }
            }

            foreach (DocsParam param in api.Params)
            {
                if (TryGetOrCreateXmlNode(originalXmls, ParamTag, param.Value, attributeValue: param.Name, out XmlNodeSyntax? paramNode, out _))
                {
                    updated.AddRange(GetXmlRow(paramNode, indentationTrivia));
                }
            }

            if (TryGetOrCreateXmlNode(originalXmls, ReturnsTag, api.Returns, attributeValue: null, out XmlNodeSyntax? returnsNode, out _))
            {
                updated.AddRange(GetXmlRow(returnsNode, indentationTrivia));
            }

            foreach (DocsException exception in api.Exceptions)
            {
                if (TryGetOrCreateXmlNode(originalXmls, ExceptionTag, exception.Value, attributeValue: exception.Cref[2..], out XmlNodeSyntax? exceptionNode, out _))
                {
                    updated.AddRange(GetXmlRow(exceptionNode, indentationTrivia));
                }
            }

            if (TryGetOrCreateXmlNode(originalXmls, RemarksTag, api.Remarks, attributeValue: null, out XmlNodeSyntax? remarksNode, out bool isBackported) &&
                (!isBackported || (isBackported && !skipRemarks)))
            {
                updated.AddRange(GetXmlRow(remarksNode!, indentationTrivia));
            }

            return new SyntaxList<SyntaxNode>(updated);
        }

        private static IEnumerable<XmlNodeSyntax> GetXmlRow(XmlNodeSyntax item, SyntaxTrivia? indentationTrivia)
        {
            yield return GetIndentationNode(indentationTrivia);
            yield return GetTripleSlashNode();
            yield return item;
            yield return GetNewLineNode();
        }

        private static bool TryGetOrCreateXmlNode(SyntaxList<XmlNodeSyntax> originalXmls, string tagName,
            string apiDocsText, string? attributeValue, [NotNullWhen(returnValue: true)] out XmlNodeSyntax? node, out bool isBackported)
        {
            SyntaxTokenList contentTokens;

            isBackported = false;

            if (!apiDocsText.IsDocsEmpty())
            {
                isBackported = true;

                // Overwrite the current triple slash with the text that comes from api docs
                SyntaxToken textLiteral = SyntaxFactory.XmlTextLiteral(
                    leading: SyntaxFactory.TriviaList(),
                    text: apiDocsText,
                    value: apiDocsText,
                    trailing: SyntaxFactory.TriviaList());

                contentTokens = SyntaxFactory.TokenList(textLiteral);
            }
            else
            {
                // Not yet documented in api docs, so try to see if it was documented in triple slash
                XmlNodeSyntax? xmlNode = originalXmls.FirstOrDefault(xmlNode => DoesNodeHasTag(xmlNode, tagName));

                if (xmlNode != null)
                {
                    XmlElementSyntax xmlElement = (XmlElementSyntax)xmlNode;
                    XmlTextSyntax xmlText = (XmlTextSyntax)xmlElement.Content.Single();
                    contentTokens = xmlText.TextTokens;
                }
                else
                {
                    // We don't want to add an empty xml item. We want don't want to add one in this case, it needs
                    // to be missing on purpose so the developer sees the build error and adds it manually.
                    node = null;
                    return false;
                }
            }

            node = CreateXmlNode(tagName, contentTokens, attributeValue);
            return true;
        }

        private static XmlTextSyntax GetTripleSlashNode()
        {
            SyntaxToken token = SyntaxFactory.XmlTextLiteral(
                        leading: SyntaxFactory.TriviaList(SyntaxFactory.DocumentationCommentExterior(TripleSlash)),
                        text: Space,
                        value: Space,
                        trailing: SyntaxFactory.TriviaList());

            return SyntaxFactory.XmlText().WithTextTokens(SyntaxFactory.TokenList(token));
        }

        private static XmlTextSyntax GetIndentationNode(SyntaxTrivia? indentationTrivia)
        {
            List<SyntaxTrivia> triviaList = new();

            if (indentationTrivia != null)
            {
                triviaList.Add(indentationTrivia.Value);
            }

            SyntaxToken token = SyntaxFactory.XmlTextLiteral(
                        leading: SyntaxFactory.TriviaList(triviaList),
                        text: string.Empty,
                        value: string.Empty,
                        trailing: SyntaxFactory.TriviaList());

            return SyntaxFactory.XmlText().WithTextTokens(SyntaxFactory.TokenList(token));

        }

        private static XmlTextSyntax GetNewLineNode()
        {
            List<SyntaxToken> tokens = new()
            {
                SyntaxFactory.XmlTextNewLine(
                                    leading: SyntaxFactory.TriviaList(),
                                    text: NewLine,
                                    value: NewLine,
                                    trailing: SyntaxFactory.TriviaList())
            };

            return SyntaxFactory.XmlText().WithTextTokens(SyntaxFactory.TokenList(tokens));
        }

        private static XmlElementSyntax CreateXmlNode(string tagName, SyntaxTokenList contentTokens, string? attributeValue = null)
        {
            SyntaxList<XmlNodeSyntax> content = SyntaxFactory.SingletonList<XmlNodeSyntax>(SyntaxFactory.XmlText().WithTextTokens(contentTokens));

            XmlElementSyntax result;

            switch (tagName)
            {
                case SummaryTag:
                    result = SyntaxFactory.XmlSummaryElement(content);
                    break;

                case ReturnsTag:
                    result = SyntaxFactory.XmlReturnsElement(content);
                    break;

                case ParamTag:
                    Debug.Assert(!string.IsNullOrWhiteSpace(attributeValue));
                    result = SyntaxFactory.XmlParamElement(attributeValue, content);
                    break;

                case ValueTag:
                    result = SyntaxFactory.XmlValueElement(content);
                    break;

                case ExceptionTag:
                    Debug.Assert(!string.IsNullOrWhiteSpace(attributeValue));
                    // Workaround because I can't figure out how to make a CrefSyntax object
                    result = GetXmlAttributedElement(content, ExceptionTag, CrefAttributeName, attributeValue);
                    break;

                case TypeParamTag:
                    Debug.Assert(!string.IsNullOrWhiteSpace(attributeValue));
                    // Workaround because I couldn't find a SyntaxFactor for TypeParam like we have for Param
                    result = GetXmlAttributedElement(content, TypeParamTag, NameAttributeName, attributeValue);
                    break;

                case RemarksTag:
                    result = SyntaxFactory.XmlRemarksElement(content);
                    break;

                default:
                    throw new NotSupportedException();
            }

            return result;
        }

        private static XmlElementSyntax GetXmlAttributedElement(SyntaxList<XmlNodeSyntax> content, string tagName, string attributeName, string attributeValue)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(tagName));
            Debug.Assert(!string.IsNullOrWhiteSpace(attributeName));
            Debug.Assert(!string.IsNullOrWhiteSpace(attributeValue));

            XmlElementStartTagSyntax startTag = SyntaxFactory.XmlElementStartTag(SyntaxFactory.XmlName(SyntaxFactory.Identifier(tagName)));

            SyntaxToken xmlAttributeName = SyntaxFactory.Identifier(
                leading: SyntaxFactory.TriviaList(SyntaxFactory.Space),
                text: attributeName,
                trailing: SyntaxFactory.TriviaList());

            XmlNameAttributeSyntax xmlAttribute = SyntaxFactory.XmlNameAttribute(
                                                                name: SyntaxFactory.XmlName(xmlAttributeName),
                                                                startQuoteToken: SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
                                                                identifier: SyntaxFactory.IdentifierName(attributeValue),
                                                                endQuoteToken: SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken));

            SyntaxList<XmlAttributeSyntax> startTagAttributes = SyntaxFactory.SingletonList<XmlAttributeSyntax>(xmlAttribute);

            startTag = startTag.WithAttributes(startTagAttributes);

            XmlElementEndTagSyntax endTag = SyntaxFactory.XmlElementEndTag(SyntaxFactory.XmlName(SyntaxFactory.Identifier(tagName)));

            return SyntaxFactory.XmlElement(startTag, content, endTag);
        }

        private static bool DoesNodeHasTag(SyntaxNode xmlNode, string tagName)
        {
            if (tagName == ExceptionTag)
            {
                // Temporary workaround to avoid overwriting all existing triple slash exceptions
                return false;
            }
            return xmlNode.Kind() is SyntaxKind.XmlElement &&
            xmlNode is XmlElementSyntax xmlElement &&
            xmlElement.StartTag.Name.LocalName.ValueText == tagName;
        }
    }
}
