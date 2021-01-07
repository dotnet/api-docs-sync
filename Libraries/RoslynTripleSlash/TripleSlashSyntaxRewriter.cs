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

        #region Visitor overrides

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

        public override SyntaxNode? VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            SyntaxNode? baseNode = base.VisitDelegateDeclaration(node);

            ISymbol? symbol = Model.GetDeclaredSymbol(node);
            if (symbol == null)
            {
                Log.Warning($"Symbol is null.");
                return baseNode;
            }

            return VisitType(baseNode, symbol);
        }

        public override SyntaxNode? VisitEnumDeclaration(EnumDeclarationSyntax node) =>
            VisitMemberDeclaration(node);

        public override SyntaxNode? VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node) =>
            VisitMemberDeclaration(node);

        public override SyntaxNode? VisitEventFieldDeclaration(EventFieldDeclarationSyntax node) =>
            VisitVariableDeclaration(node);

        public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node) =>
            VisitVariableDeclaration(node);

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
            SyntaxTriviaList altmembers = GetAltMembers(member, leadingWhitespace);
            SyntaxTriviaList relateds = GetRelateds(member, leadingWhitespace);

            return GetNodeWithTrivia(leadingWhitespace, node, summary, value, remarks, exceptions, seealsos, altmembers, relateds);
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

        #endregion

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

            SyntaxTriviaList parameters = new();
            SyntaxTriviaList typeParameters = new();
            SyntaxTriviaList seealsos = new();
            SyntaxTriviaList altmembers = new();
            SyntaxTriviaList relateds = new();

            SyntaxTriviaList leadingWhitespace = GetLeadingWhitespace(node);

            if (!UseBoilerplate)
            {
                if (!TryGetType(symbol, out DocsType? type))
                {
                    return node;
                }

                summaryText = type.Summary;
                remarksText = type.Remarks;

                parameters = GetParameters(type, leadingWhitespace);
                typeParameters = GetTypeParameters(type, leadingWhitespace);
                seealsos = GetSeeAlsos(type, leadingWhitespace);
                altmembers = GetAltMembers(type, leadingWhitespace);
                relateds = GetRelateds(type, leadingWhitespace);
            }

            SyntaxTriviaList summary = GetSummary(summaryText, leadingWhitespace);
            SyntaxTriviaList remarks = GetRemarks(remarksText, leadingWhitespace);

            return GetNodeWithTrivia(leadingWhitespace, node, summary, parameters, typeParameters, remarks, seealsos, altmembers, relateds);
        }

        private SyntaxNode? VisitBaseMethodDeclaration(BaseMethodDeclarationSyntax node)
        {
            // The Docs files only contain docs for public elements,
            // so if no comments are found, we return the node unmodified
            if (!TryGetMember(node, out DocsMember? member))
            {
                return node;
            }

            SyntaxTriviaList leadingWhitespace = GetLeadingWhitespace(node);

            SyntaxTriviaList summary = GetSummary(UseBoilerplate ? BoilerplateText : member.Summary, leadingWhitespace);
            SyntaxTriviaList parameters = GetParameters(member, leadingWhitespace);
            SyntaxTriviaList typeParameters = GetTypeParameters(member, leadingWhitespace);
            SyntaxTriviaList returns = GetReturns(UseBoilerplate ? BoilerplateText : member.Returns, leadingWhitespace);
            SyntaxTriviaList remarks = GetRemarks(member.Remarks, leadingWhitespace);
            SyntaxTriviaList exceptions = GetExceptions(member, leadingWhitespace);
            SyntaxTriviaList seealsos = GetSeeAlsos(member, leadingWhitespace);
            SyntaxTriviaList altmembers = GetAltMembers(member, leadingWhitespace);
            SyntaxTriviaList relateds = GetRelateds(member, leadingWhitespace);

            return GetNodeWithTrivia(leadingWhitespace, node, summary, parameters, typeParameters, returns, remarks, exceptions, seealsos, altmembers, relateds);
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
            SyntaxTriviaList exceptions = GetExceptions(member, leadingWhitespace);
            SyntaxTriviaList seealsos = GetSeeAlsos(member, leadingWhitespace);
            SyntaxTriviaList altmembers = GetAltMembers(member, leadingWhitespace);
            SyntaxTriviaList relateds = GetRelateds(member, leadingWhitespace);

            return GetNodeWithTrivia(leadingWhitespace, node, summary, remarks, exceptions, seealsos, altmembers, relateds);
        }

        private SyntaxNode? VisitVariableDeclaration(BaseFieldDeclarationSyntax node)
        {
            // The comments need to be extracted from the underlying variable declarator inside the declaration
            VariableDeclarationSyntax declaration = node.Declaration;

            // Only port docs if there is only one variable in the declaration
            if (declaration.Variables.Count == 1)
            {
                if (!TryGetMember(declaration.Variables.First(), out DocsMember? member))
                {
                    return node;
                }

                SyntaxTriviaList leadingWhitespace = GetLeadingWhitespace(node);

                SyntaxTriviaList summary = GetSummary(UseBoilerplate ? BoilerplateText : member.Summary, leadingWhitespace);
                SyntaxTriviaList remarks = GetRemarks(member.Remarks, leadingWhitespace);
                SyntaxTriviaList seealsos = GetSeeAlsos(member, leadingWhitespace);
                SyntaxTriviaList altmembers = GetAltMembers(member, leadingWhitespace);
                SyntaxTriviaList relateds = GetRelateds(member, leadingWhitespace);

                return GetNodeWithTrivia(leadingWhitespace, node, summary, remarks, seealsos, altmembers, relateds);
            }

            return node;
        }

        private SyntaxNode GetNodeWithTrivia(SyntaxTriviaList leadingWhitespace, SyntaxNode node, params SyntaxTriviaList[] trivias)
        {
            SyntaxTriviaList finalTrivia = new();
            var leadingTrivia = node.GetLeadingTrivia();
            if (leadingTrivia.Any())
            {
                if (leadingTrivia[0].IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    // Ensure the endline that separates nodes is respected
                    finalTrivia = new(SyntaxFactory.ElasticCarriageReturnLineFeed);
                }
            }

            foreach (SyntaxTriviaList t in trivias)
            {
                finalTrivia = finalTrivia.AddRange(t);
            }
            finalTrivia = finalTrivia.AddRange(leadingWhitespace);

            return node.WithLeadingTrivia(finalTrivia);
        }

        private SyntaxTriviaList GetLeadingWhitespace(SyntaxNode node) =>
            node.GetLeadingTrivia().Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).ToSyntaxTriviaList();

        private SyntaxTriviaList GetSummary(string text, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxList<XmlNodeSyntax> contents = GetContentsInRows(text.WithoutPrefix());
            XmlElementSyntax element = SyntaxFactory.XmlSummaryElement(contents);
            return GetXmlTrivia(element, leadingWhitespace);
        }

        private SyntaxTriviaList GetRemarks(string text, SyntaxTriviaList leadingWhitespace)
        {
            if (!UseBoilerplate && !text.IsDocsEmpty())
            {
                string trimmedRemarks = text.RemoveSubstrings("<![CDATA[", "]]>").Trim(); // The SyntaxFactory needs to be the one to add these
                SyntaxTokenList cdata = GetTextAsTokens(trimmedRemarks, leadingWhitespace.Add(SyntaxFactory.CarriageReturnLineFeed), addInitialNewLine: true);
                XmlNodeSyntax xmlRemarksContent = SyntaxFactory.XmlCDataSection(SyntaxFactory.Token(SyntaxKind.XmlCDataStartToken), cdata, SyntaxFactory.Token(SyntaxKind.XmlCDataEndToken));
                XmlElementSyntax xmlRemarks = SyntaxFactory.XmlRemarksElement(xmlRemarksContent);

                return GetXmlTrivia(xmlRemarks, leadingWhitespace);
            }

            return new();
        }

        private SyntaxTriviaList GetValue(string text, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxList<XmlNodeSyntax> contents = GetContentsInRows(text.WithoutPrefix());
            XmlElementSyntax element = SyntaxFactory.XmlValueElement(contents);
            return GetXmlTrivia(element, leadingWhitespace);
        }

        private SyntaxTriviaList GetParameter(string name, string text, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxList<XmlNodeSyntax> contents = GetContentsInRows(text.WithoutPrefix());
            XmlElementSyntax element = SyntaxFactory.XmlParamElement(name, contents);
            return GetXmlTrivia(element, leadingWhitespace);
        }

        private SyntaxTriviaList GetParameters(DocsAPI api, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList parameters = new();
            foreach (SyntaxTriviaList parameterTrivia in api.Params.Select(
                param => GetParameter(param.Name, UseBoilerplate ? BoilerplateText : param.Value, leadingWhitespace)))
            {
                parameters = parameters.AddRange(parameterTrivia);
            }
            return parameters;
        }

        private SyntaxTriviaList GetTypeParam(string name, string text, SyntaxTriviaList leadingWhitespace)
        {
            var attribute = new SyntaxList<XmlAttributeSyntax>(SyntaxFactory.XmlTextAttribute("name", name));
            SyntaxList<XmlNodeSyntax> contents = GetContentsInRows(text);
            return GetXmlTrivia("typeparam", attribute, contents, leadingWhitespace);
        }

        private SyntaxTriviaList GetTypeParameters(DocsAPI api, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList typeParameters = new();
            foreach (SyntaxTriviaList typeParameterTrivia in api.TypeParams.Select(
                typeParam => GetTypeParam(typeParam.Name, UseBoilerplate ? BoilerplateText : typeParam.Value, leadingWhitespace)))
            {
                typeParameters = typeParameters.AddRange(typeParameterTrivia);
            }
            return typeParameters;
        }

        private SyntaxTriviaList GetReturns(string text, SyntaxTriviaList leadingWhitespace)
        {
            // For when returns is empty because the method returns void
            if (string.IsNullOrWhiteSpace(text))
            {
                return new();
            }

            SyntaxList<XmlNodeSyntax> contents = GetContentsInRows(text.WithoutPrefix());
            XmlElementSyntax element = SyntaxFactory.XmlReturnsElement(contents);
            return GetXmlTrivia(element, leadingWhitespace);
        }

        private SyntaxTriviaList GetException(string cref, string text, SyntaxTriviaList leadingWhitespace)
        {
            TypeCrefSyntax crefSyntax = SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName(cref.WithoutPrefix()));
            XmlTextSyntax contents = SyntaxFactory.XmlText(GetTextAsTokens(text.WithoutPrefix(), leadingWhitespace, addInitialNewLine: false));
            XmlElementSyntax element = SyntaxFactory.XmlExceptionElement(crefSyntax, contents);
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

        private SyntaxTriviaList GetSeeAlso(string cref, SyntaxTriviaList leadingWhitespace)
        {
            TypeCrefSyntax crefSyntax = SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName(cref.WithoutPrefix()));
            XmlEmptyElementSyntax element = SyntaxFactory.XmlSeeAlsoElement(crefSyntax);
            return GetXmlTrivia(element, leadingWhitespace);
        }

        private SyntaxTriviaList GetSeeAlsos(DocsAPI api, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList seealsos = new();
            if (!UseBoilerplate && api.SeeAlsoCrefs.Any())
            {
                foreach (SyntaxTriviaList seealsoTrivia in api.SeeAlsoCrefs.Select(
                    s => GetSeeAlso(s, leadingWhitespace)))
                {
                    seealsos = seealsos.AddRange(seealsoTrivia);
                }
            }
            return seealsos;
        }

        private SyntaxTriviaList GetAltMember(string cref, SyntaxTriviaList leadingWhitespace)
        {
            XmlAttributeSyntax attribute = SyntaxFactory.XmlTextAttribute("cref", cref.WithoutPrefix());
            XmlEmptyElementSyntax emptyElement = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName(SyntaxFactory.Identifier("altmember")), new SyntaxList<XmlAttributeSyntax>(attribute));
            return GetXmlTrivia(emptyElement, leadingWhitespace);
        }

        private SyntaxTriviaList GetAltMembers(DocsAPI api, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList altMembers = new();
            if (!UseBoilerplate && api.AltMembers.Any())
            {
                foreach (SyntaxTriviaList altMemberTrivia in api.AltMembers.Select(
                    s => GetAltMember(s, leadingWhitespace)))
                {
                    altMembers = altMembers.AddRange(altMemberTrivia);
                }
            }
            return altMembers;
        }

        private SyntaxTriviaList GetRelated(string articleType, string href, string value, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxList<XmlAttributeSyntax> attributes = new();

            attributes = attributes.Add(SyntaxFactory.XmlTextAttribute("type", articleType));
            attributes = attributes.Add(SyntaxFactory.XmlTextAttribute("href", href));

            SyntaxList<XmlNodeSyntax> contents = GetContentsInRows(value);
            return GetXmlTrivia("related", attributes, contents, leadingWhitespace);
        }

        private SyntaxTriviaList GetRelateds(DocsAPI api, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList relateds = new();
            if (!UseBoilerplate && api.Relateds.Any())
            {
                foreach (SyntaxTriviaList relatedsTrivia in api.Relateds.Select(
                    s => GetRelated(s.ArticleType, s.Href, s.Value, leadingWhitespace)))
                {
                    relateds = relateds.AddRange(relatedsTrivia);
                }
            }
            return relateds;
        }

        private SyntaxTokenList GetTextAsTokens(string text, SyntaxTriviaList leadingWhitespace, bool addInitialNewLine)
        {
            string whitespace = leadingWhitespace.ToFullString().Replace(Environment.NewLine, "");
            SyntaxToken newLineAndWhitespace = SyntaxFactory.XmlTextNewLine(Environment.NewLine + whitespace);

            SyntaxTrivia leadingTrivia = SyntaxFactory.SyntaxTrivia(SyntaxKind.DocumentationCommentExteriorTrivia, string.Empty);
            SyntaxTriviaList leading = SyntaxTriviaList.Create(leadingTrivia);

            var tokens = new List<SyntaxToken>();

            string[] splittedLines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Only add the initial new line and whitespace if the contents have more than one line. Otherwise, we want the contents to be inlined inside the tags.
            if (splittedLines.Length > 1 && addInitialNewLine)
            {
                // For example, the remarks section needs a new line before the initial "## Remarks" title
                tokens.Add(newLineAndWhitespace);
                tokens.Add(newLineAndWhitespace);
            }

            int lineNumber = 1;
            foreach (string line in splittedLines)
            {
                SyntaxToken token = SyntaxFactory.XmlTextLiteral(leading, line, line, default);
                tokens.Add(token);

                // Only add extra new lines if we expect more than one line of text in the contents. Otherwise, inline it inside the tags.
                if (splittedLines.Length > 1)
                {
                    tokens.Add(newLineAndWhitespace);
                    tokens.Add(newLineAndWhitespace);
                }

                lineNumber++;
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
