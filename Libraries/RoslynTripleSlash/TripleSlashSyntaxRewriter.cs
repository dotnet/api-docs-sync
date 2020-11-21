#nullable enable
using Libraries.Docs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Libraries.RoslynTripleSlash
{
    internal class TripleSlashSyntaxRewriter : CSharpSyntaxRewriter
    {
        private const string BoilerplateText = "Comments located in main file.";

        private DocsCommentsContainer DocsComments { get; }
        private SemanticModel Model { get; }
        private bool UseBoilerplate { get; }

        public TripleSlashSyntaxRewriter(DocsCommentsContainer docsComments, SemanticModel model, Location location, SyntaxTree tree, bool useBoilerplate) : base(visitIntoStructuredTrivia: true)
        {
            DocsComments = docsComments;
            Model = model;
            UseBoilerplate = useBoilerplate;
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            SyntaxNode? baseNode = base.VisitClassDeclaration(node);

            ISymbol? symbol = Model.GetDeclaredSymbol(node);
            if (symbol == null)
            {
                Log.Warning($"Symbol is null.");
                return baseNode;
            }

            return VisitType(baseNode, symbol);
        }

        public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node) =>
            VisitBaseMethodDeclaration(node);

        public override SyntaxNode? VisitEnumDeclaration(EnumDeclarationSyntax node) =>
            VisitMemberDeclaration(node);

        public override SyntaxNode? VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node) =>
            VisitMemberDeclaration(node);

        public override SyntaxNode? VisitEventDeclaration(EventDeclarationSyntax node) =>
            VisitMemberDeclaration(node);

        public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node) =>
            VisitMemberDeclaration(node);

        public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            SyntaxNode? baseNode = base.VisitInterfaceDeclaration(node);

            ISymbol? symbol = Model.GetDeclaredSymbol(node);
            if (symbol == null)
            {
                Log.Warning($"Symbol is null.");
                return baseNode;
            }

            return VisitType(baseNode, symbol);
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node) =>
            VisitBaseMethodDeclaration(node);

        public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (!TryGetMember(node, out DocsMember? member))
            {
                return node;
            }

            string summaryText = BoilerplateText;
            string valueText = BoilerplateText;

            if (!UseBoilerplate)
            {
                summaryText = member.Summary;
                valueText = member.Value;
            }

            SyntaxTriviaList leadingWhitespace = GetLeadingWhitespace(node);

            SyntaxTriviaList summary = GetSummary(summaryText, leadingWhitespace);
            SyntaxTriviaList value = GetValue(valueText, leadingWhitespace);
            SyntaxTriviaList remarks = GetRemarks(member.Remarks, leadingWhitespace);
            SyntaxTriviaList exceptions = GetExceptions(member, leadingWhitespace);
            SyntaxTriviaList seealsos = GetSeeAlsos(member, leadingWhitespace);

            return GetNodeWithTrivia(node, summary, value, remarks, exceptions, seealsos);
        }

        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
        {
            SyntaxNode? baseNode = base.VisitStructDeclaration(node);

            ISymbol? symbol = Model.GetDeclaredSymbol(node);
            if (symbol == null)
            {
                Log.Warning($"Symbol is null.");
                return baseNode;
            }

            return VisitType(baseNode, symbol);
        }

        private SyntaxNode? VisitType(SyntaxNode? node, ISymbol? symbol)
        {
            if (node == null || symbol == null)
            {
                return node;
            }

            string? docId = symbol.GetDocumentationCommentId();
            if (string.IsNullOrWhiteSpace(docId))
            {
                Log.Warning($"DocId is null or empty.");
                return node;
            }

            string summaryText = BoilerplateText;
            string remarksText = string.Empty;

            if (!UseBoilerplate)
            {
                if (!TryGetType(symbol, out DocsType? type))
                {
                    return node;
                }

                summaryText = type.Summary;
                remarksText = type.Remarks;
            }

            SyntaxTriviaList leadingWhitespace = GetLeadingWhitespace(node);

            SyntaxTriviaList summary = GetSummary(summaryText, leadingWhitespace);
            SyntaxTriviaList remarks = GetRemarks(remarksText, leadingWhitespace);

            return GetNodeWithTrivia(node, summary, remarks);
        }

        private SyntaxNode GetNodeWithTrivia(SyntaxNode node, params SyntaxTriviaList[] trivias)
        {
            SyntaxTriviaList finalTrivia = new(SyntaxFactory.CarriageReturnLineFeed); // Space to separate from previous definition
            foreach (SyntaxTriviaList t in trivias)
            {
                finalTrivia = finalTrivia.AddRange(t);
            }
            finalTrivia = finalTrivia.AddRange(GetLeadingWhitespace(node)); // spaces before type declaration

            return node.WithLeadingTrivia(finalTrivia);
        }

        private SyntaxNode? VisitBaseMethodDeclaration(BaseMethodDeclarationSyntax node)
        {
            if (!TryGetMember(node, out DocsMember? member))
            {
                return node;
            }

            SyntaxTriviaList leadingWhitespace = GetLeadingWhitespace(node);

            SyntaxTriviaList summary = GetSummary(UseBoilerplate ? BoilerplateText : member.Summary, leadingWhitespace);

            SyntaxTriviaList parameters = new();
            foreach (SyntaxTriviaList parameterTrivia in member.Params.Select(
                param => GetParam(param.Name, UseBoilerplate ? BoilerplateText : param.Value, leadingWhitespace)))
            {
                parameters = parameters.AddRange(parameterTrivia);
            }

            SyntaxTriviaList typeParameters = new();
            foreach (SyntaxTriviaList typeParameterTrivia in member.TypeParams.Select(
                param => GetTypeParam(param.Name, UseBoilerplate ? BoilerplateText : param.Value, leadingWhitespace)))
            {
                typeParameters = typeParameters.AddRange(typeParameterTrivia);
            }

            SyntaxTriviaList returns = GetReturns(UseBoilerplate ? BoilerplateText : member.Returns, leadingWhitespace);
            SyntaxTriviaList remarks = GetRemarks(member.Remarks, leadingWhitespace);
            SyntaxTriviaList exceptions = GetExceptions(member, leadingWhitespace);
            SyntaxTriviaList seealsos = GetSeeAlsos(member, leadingWhitespace);

            return GetNodeWithTrivia(node, summary, parameters, typeParameters, returns, remarks, exceptions, seealsos);
        }

        private SyntaxNode? VisitMemberDeclaration(MemberDeclarationSyntax node)
        {
            if (!TryGetMember(node, out DocsMember? member))
            {
                return node;
            }

            SyntaxTriviaList leadingWhitespace = GetLeadingWhitespace(node);

            SyntaxTriviaList summary = GetSummary(UseBoilerplate ? BoilerplateText : member.Summary, leadingWhitespace);
            SyntaxTriviaList remarks = GetRemarks(member.Remarks, leadingWhitespace);

            SyntaxTriviaList exceptions = new();
            // No need to add exceptions in secondary files
            if (!UseBoilerplate && member.Exceptions.Any())
            {
                foreach (SyntaxTriviaList exceptionsTrivia in member.Exceptions.Select(
                    exception => GetException(exception.Cref, exception.Value, leadingWhitespace)))
                {
                    exceptions = exceptions.AddRange(exceptionsTrivia);
                }
            }

            return GetNodeWithTrivia(node, summary, remarks, exceptions);
        }

        private SyntaxTriviaList GetLeadingWhitespace(SyntaxNode node) =>
            node.GetLeadingTrivia().Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).ToSyntaxTriviaList();

        private SyntaxTriviaList GetSummary(string text, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxList<XmlNodeSyntax> contents = GetContentsInRows(text);
            XmlElementSyntax element = SyntaxFactory.XmlSummaryElement(contents);
            return GetXmlTrivia(element, leadingWhitespace);
        }

        private SyntaxTriviaList GetRemarks(string text, SyntaxTriviaList leadingWhitespace)
        {
            if (!UseBoilerplate && !text.IsDocsEmpty())
            {
                string trimmedRemarks = text.RemoveSubstrings("<![CDATA[", "]]>").Trim();
                SyntaxTokenList cdata = GetTextAsTokens(trimmedRemarks, leadingWhitespace.Add(SyntaxFactory.CarriageReturnLineFeed));
                XmlNodeSyntax xmlRemarksContent = SyntaxFactory.XmlCDataSection(SyntaxFactory.Token(SyntaxKind.XmlCDataStartToken), cdata, SyntaxFactory.Token(SyntaxKind.XmlCDataEndToken));
                XmlElementSyntax xmlRemarks = SyntaxFactory.XmlRemarksElement(xmlRemarksContent);

                return GetXmlTrivia(xmlRemarks, leadingWhitespace);
            }

            return new();
        }

        private SyntaxTriviaList GetValue(string text, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxList<XmlNodeSyntax> contents = GetContentsInRows(text);
            XmlElementSyntax element = SyntaxFactory.XmlValueElement(contents);
            return GetXmlTrivia(element, leadingWhitespace);
        }

        private SyntaxTriviaList GetParam(string name, string text, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxList<XmlNodeSyntax> contents = GetContentsInRows(text);
            XmlElementSyntax element = SyntaxFactory.XmlParamElement(name, contents);
            return GetXmlTrivia(element, leadingWhitespace);
        }

        private SyntaxTriviaList GetTypeParam(string name, string text, SyntaxTriviaList leadingWhitespace)
        {
            var attribute = new SyntaxList<XmlAttributeSyntax>(SyntaxFactory.XmlTextAttribute(name, text));
            SyntaxList<XmlNodeSyntax> contents = GetContentsInRows(text);
            return GetXmlTrivia("typeparam", attribute, contents, leadingWhitespace);
        }

        private SyntaxTriviaList GetReturns(string text, SyntaxTriviaList leadingWhitespace)
        {
            // For when returns is empty because the method returns void
            if (string.IsNullOrWhiteSpace(text))
            {
                return new();
            }

            SyntaxList<XmlNodeSyntax> contents = GetContentsInRows(text);
            XmlElementSyntax element = SyntaxFactory.XmlReturnsElement(contents);
            return GetXmlTrivia(element, leadingWhitespace);
        }

        private SyntaxTriviaList GetExceptions(DocsMember member, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList exceptions = new();
            // No need to add exceptions in secondary files
            if (!UseBoilerplate && member.Exceptions.Any())
            {
                foreach (SyntaxTriviaList exceptionsTrivia in member.Exceptions.Select(
                    exception => GetException(exception.Cref, exception.Value, leadingWhitespace)))
                {
                    exceptions = exceptions.AddRange(exceptionsTrivia);
                }
            }
            return exceptions;
        }

        private SyntaxTriviaList GetException(string cref, string text, SyntaxTriviaList leadingWhitespace)
        {
            TypeCrefSyntax crefSyntax = SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName(cref));
            XmlTextSyntax contents = SyntaxFactory.XmlText(GetTextAsTokens(text, leadingWhitespace));
            XmlElementSyntax element = SyntaxFactory.XmlExceptionElement(crefSyntax, contents);
            return GetXmlTrivia(element, leadingWhitespace);
        }

        private SyntaxTriviaList GetSeeAlsos(DocsMember member, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList seealsos = new();
            // No need to add exceptions in secondary files
            if (!UseBoilerplate && member.SeeAlsos.Any())
            {
                foreach (SyntaxTriviaList seealsoTrivia in member.SeeAlsos.Select(
                    s => GetSeeAlso(s.Cref, leadingWhitespace)))
                {
                    seealsos = seealsos.AddRange(seealsoTrivia);
                }
            }
            return seealsos;
        }

        private SyntaxTriviaList GetSeeAlso(string cref, SyntaxTriviaList leadingWhitespace)
        {
            TypeCrefSyntax crefSyntax = SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName(cref));
            XmlEmptyElementSyntax element = SyntaxFactory.XmlSeeAlsoElement(crefSyntax);
            return GetXmlTrivia(element, leadingWhitespace);
        }

        private SyntaxTokenList GetTextAsTokens(string text, SyntaxTriviaList leadingWhitespace)
        {
            string whitespace = leadingWhitespace.ToFullString().Replace(Environment.NewLine, "");
            SyntaxToken newLineAndWhitespace = SyntaxFactory.XmlTextNewLine(Environment.NewLine + whitespace);

            SyntaxTrivia leadingTrivia = SyntaxFactory.SyntaxTrivia(SyntaxKind.DocumentationCommentExteriorTrivia, string.Empty);
            SyntaxTriviaList leading = SyntaxTriviaList.Create(leadingTrivia);

            var tokens = new List<SyntaxToken>();
            tokens.Add(newLineAndWhitespace);
            foreach (string line in text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                SyntaxToken token = SyntaxFactory.XmlTextLiteral(leading, line, line, default);
                tokens.Add(token);
                tokens.Add(newLineAndWhitespace);
            }
            return SyntaxFactory.TokenList(tokens);
        }

        private SyntaxList<XmlNodeSyntax> GetContentsInRows(string text)
        {
            var nodes = new SyntaxList<XmlNodeSyntax>();
            foreach (string line in text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var tokenList = SyntaxFactory.ParseTokens(line).ToArray(); // Prevents unexpected change from "<" to "&lt;"
                XmlTextSyntax xmlText = SyntaxFactory.XmlText(tokenList);
                return nodes.Add(xmlText);
            }
            return nodes;
        }

        private SyntaxTriviaList GetXmlTrivia(XmlNodeSyntax node, SyntaxTriviaList leadingWhitespace)
        {
            DocumentationCommentTriviaSyntax docComment = SyntaxFactory.DocumentationComment(node);
            SyntaxTrivia docCommentTrivia = SyntaxFactory.Trivia(docComment);

            return leadingWhitespace
                .Add(docCommentTrivia)
                .Add(SyntaxFactory.CarriageReturnLineFeed);
        }

        // Generates a custom SyntaxTrivia object containing a triple slashed xml element with optional attributes.
        // Looks like below (excluding square brackets):
        // [    /// <element attribute1="value1" attribute2="value2">text</element>]
        private SyntaxTriviaList GetXmlTrivia(string name, SyntaxList<XmlAttributeSyntax> attributes, SyntaxList<XmlNodeSyntax> contents, SyntaxTriviaList leadingWhitespace)
        {
            XmlElementStartTagSyntax start = SyntaxFactory.XmlElementStartTag(
                SyntaxFactory.Token(SyntaxKind.LessThanToken),
                SyntaxFactory.XmlName(SyntaxFactory.Identifier(name)),
                attributes,
                SyntaxFactory.Token(SyntaxKind.GreaterThanToken));

            XmlElementEndTagSyntax end = SyntaxFactory.XmlElementEndTag(
                SyntaxFactory.Token(SyntaxKind.LessThanSlashToken),
                SyntaxFactory.XmlName(SyntaxFactory.Identifier(name)),
                SyntaxFactory.Token(SyntaxKind.GreaterThanToken));

            XmlElementSyntax element = SyntaxFactory.XmlElement(start, contents, end);

            return GetXmlTrivia(element, leadingWhitespace);
        }

        private bool TryGetMember(SyntaxNode node, [NotNullWhen(returnValue: true)] out DocsMember? member)
        {
            member = null;
            if (Model.GetDeclaredSymbol(node) is ISymbol symbol)
            {
                string? docId = symbol.GetDocumentationCommentId();
                if (!string.IsNullOrWhiteSpace(docId))
                {
                    member = DocsComments.Members.FirstOrDefault(m => m.DocId == docId);
                }
            }
            return member != null;
        }

        private bool TryGetType(ISymbol symbol, [NotNullWhen(returnValue: true)] out DocsType? type)
        {
            type = null;

            string? docId = symbol.GetDocumentationCommentId();
            if (!string.IsNullOrWhiteSpace(docId))
            {
                type = DocsComments.Types.FirstOrDefault(t => t.DocId == docId);
            }

            return type != null;
        }
    }
}
