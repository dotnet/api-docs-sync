#nullable enable
using Libraries.Docs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace Libraries.RoslynTripleSlash
{
    /*
    The following triple slash comments section:

        /// <summary>
        /// My summary.
        /// </summary>
        /// <param name="paramName">My param description.</param>
        /// <remarks>My remarks.</remarks>
        public ...
    
    translates to this syntax tree structure:

    PublicKeyword (SyntaxToken) -> The public keyword including its trivia.
        Lead: EndOfLineTrivia -> The newline char before the 4 whitespace chars before the triple slash comments.
        Lead: WhitespaceTrivia -> The 4 whitespace chars before the triple slash comments.
        Lead: SingleLineDocumentationCommentTrivia (SyntaxTrivia)
            SingleLineDocumentationCommentTrivia (DocumentationCommentTriviaSyntax) -> The triple slash comments, excluding the first 3 slash chars.
                XmlText (XmlTextSyntax)
                    XmlTextLiteralToken (SyntaxToken) -> The space between the first triple slash and <summary>.
                        Lead: DocumentationCommentExteriorTrivia (SyntaxTrivia) -> The first 3 slash chars.

                XmlElement (XmlElementSyntax) -> From <summary> to </summary>. Excludes the first 3 slash chars, but includes the second and third trios.
                    XmlElementStartTag (XmlElementStartTagSyntax) -> <summary>
                        LessThanToken (SyntaxToken) -> <
                        XmlName (XmlNameSyntax) -> summary
                            IdentifierToken (SyntaxToken) -> summary
                        GreaterThanToken (SyntaxToken) -> >
                    XmlText (XmlTextSyntax) -> Everything after <summary> and before </summary>
                        XmlTextLiteralNewLineToken (SyntaxToken) -> endline after <summary>
                        XmlTextLiteralToken (SyntaxToken) -> [ My summary.]
                            Lead: DocumentationCommentExteriorTrivia (SyntaxTrivia) -> endline after summary text
                        XmlTextLiteralNewToken (SyntaxToken) -> Space between 3 slashes and </summary>
                            Lead: DocumentationCommentExteriorTrivia (SyntaxTrivia) -> whitespace + 3 slashes before the </summary>
                    XmlElementEndTag (XmlElementEndTagSyntax) -> </summary>
                        LessThanSlashToken (SyntaxToken) -> </
                        XmlName (XmlNameSyntax) -> summary
                            IdentifierToken (SyntaxToken) -> summary
                        GreaterThanToken (SyntaxToken) -> >
                XmlText -> endline + whitespace + 3 slahes before <param
                    XmlTextLiteralNewLineToken (XmlTextSyntax) -> endline after </summary>
                    XmlTextLiteralToken (XmlTextLiteralToken) -> space after 3 slashes and before <param
                        Lead: DocumentationCommentExteriorTrivia (SyntaxTrivia) -> whitespace + 3 slashes before the space and <param

                XmlElement -> <param name="...">...</param>
                    XmlElementStartTag -> <param name="...">
                        LessThanToken -> <
                        XmlName -> param
                            IdentifierToken -> param
                        XmlNameAttribute (XmlNameAttributeSyntax) -> name="paramName"
                            XmlName -> name
                                IdentifierToken -> name
                                    Lead: WhitespaceTrivia -> space between param and name
                            EqualsToken -> =
                            DoubleQuoteToken -> opening "
                            IdentifierName -> paramName
                                IdentifierToken -> paramName
                            DoubleQuoteToken -> closing "
                        GreaterThanToken -> >
                    XmlText -> My param description.
                        XmlTextLiteralToken -> My param description.
                    XmlElementEndTag -> </param>
                        LessThanSlashToken -> </
                        XmlName -> param
                            IdentifierToken -> param
                        GreaterThanToken -> >
                XmlText -> newline + 4 whitespace chars + /// before <remarks>

                XmlElement -> <remarks>My remarks.</remarks>
                XmlText -> new line char after </remarks>
                    XmlTextLiteralNewLineToken -> new line char after </remarks>
                EndOfDocumentationCommentToken (SyntaxToken) -> invisible

        Lead: WhitespaceTrivia -> The 4 whitespace chars before the public keyword.
        Trail: WhitespaceTrivia -> The single whitespace char after the public keyword.
    */
    internal class TripleSlashSyntaxRewriter : CSharpSyntaxRewriter
    {
        private static readonly string[] ReservedKeywords = new[] { "abstract", "async", "await", "false", "null", "sealed", "static", "true", "virtual" };
        private DocsCommentsContainer DocsComments { get; }
        private SemanticModel Model { get; }

        public TripleSlashSyntaxRewriter(DocsCommentsContainer docsComments, SemanticModel model) : base(visitIntoStructuredTrivia: true)
        {
            DocsComments = docsComments;
            Model = model;
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

        public override SyntaxNode? VisitOperatorDeclaration(OperatorDeclarationSyntax node) =>
            VisitBaseMethodDeclaration(node);

        public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (!TryGetMember(node, out DocsMember? member))
            {
                return node;
            }

            SyntaxTriviaList leadingWhitespace = GetLeadingWhitespace(node);

            SyntaxTriviaList summary = GetSummary(member, leadingWhitespace);
            SyntaxTriviaList value = GetValue(member, leadingWhitespace);
            SyntaxTriviaList remarks = GetRemarks(member, leadingWhitespace);
            SyntaxTriviaList exceptions = GetExceptions(member.Exceptions, leadingWhitespace);
            SyntaxTriviaList seealsos = GetSeeAlsos(member.SeeAlsoCrefs, leadingWhitespace);
            SyntaxTriviaList altmembers = GetAltMembers(member.AltMembers, leadingWhitespace);
            SyntaxTriviaList relateds = GetRelateds(member.Relateds, leadingWhitespace);

            return GetNodeWithTrivia(leadingWhitespace, node, summary, value, remarks, exceptions, seealsos, altmembers, relateds);
        }

        public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            SyntaxNode? baseNode = base.VisitRecordDeclaration(node);

            ISymbol? symbol = Model.GetDeclaredSymbol(node);
            if (symbol == null)
            {
                Log.Warning($"Symbol is null.");
                return baseNode;
            }

            return VisitType(baseNode, symbol);
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

            SyntaxTriviaList leadingWhitespace = GetLeadingWhitespace(node);

            if (!TryGetType(symbol, out DocsType? type))
            {
                return node;
            }

            
            SyntaxTriviaList summary = GetSummary(type, leadingWhitespace);
            SyntaxTriviaList remarks = GetRemarks(type, leadingWhitespace);
            SyntaxTriviaList parameters = GetParameters(type, leadingWhitespace);
            SyntaxTriviaList typeParameters = GetTypeParameters(type, leadingWhitespace);
            SyntaxTriviaList seealsos = GetSeeAlsos(type.SeeAlsoCrefs, leadingWhitespace);
            SyntaxTriviaList altmembers = GetAltMembers(type.AltMembers, leadingWhitespace);
            SyntaxTriviaList relateds = GetRelateds(type.Relateds, leadingWhitespace);


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

            SyntaxTriviaList summary = GetSummary(member, leadingWhitespace);
            SyntaxTriviaList parameters = GetParameters(member, leadingWhitespace);
            SyntaxTriviaList typeParameters = GetTypeParameters(member, leadingWhitespace);
            SyntaxTriviaList returns = GetReturns(member, leadingWhitespace);
            SyntaxTriviaList remarks = GetRemarks(member, leadingWhitespace);
            SyntaxTriviaList exceptions = GetExceptions(member.Exceptions, leadingWhitespace);
            SyntaxTriviaList seealsos = GetSeeAlsos(member.SeeAlsoCrefs, leadingWhitespace);
            SyntaxTriviaList altmembers = GetAltMembers(member.AltMembers, leadingWhitespace);
            SyntaxTriviaList relateds = GetRelateds(member.Relateds, leadingWhitespace);

            return GetNodeWithTrivia(leadingWhitespace, node, summary, parameters, typeParameters, returns, remarks, exceptions, seealsos, altmembers, relateds);
        }

        private SyntaxNode? VisitMemberDeclaration(MemberDeclarationSyntax node)
        {
            if (!TryGetMember(node, out DocsMember? member))
            {
                return node;
            }

            SyntaxTriviaList leadingWhitespace = GetLeadingWhitespace(node);

            SyntaxTriviaList summary = GetSummary(member, leadingWhitespace);
            SyntaxTriviaList remarks = GetRemarks(member, leadingWhitespace);
            SyntaxTriviaList exceptions = GetExceptions(member.Exceptions, leadingWhitespace);
            SyntaxTriviaList seealsos = GetSeeAlsos(member.SeeAlsoCrefs, leadingWhitespace);
            SyntaxTriviaList altmembers = GetAltMembers(member.AltMembers, leadingWhitespace);
            SyntaxTriviaList relateds = GetRelateds(member.Relateds, leadingWhitespace);

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

                SyntaxTriviaList summary = GetSummary(member, leadingWhitespace);
                SyntaxTriviaList remarks = GetRemarks(member, leadingWhitespace);
                SyntaxTriviaList seealsos = GetSeeAlsos(member.SeeAlsoCrefs, leadingWhitespace);
                SyntaxTriviaList altmembers = GetAltMembers(member.AltMembers, leadingWhitespace);
                SyntaxTriviaList relateds = GetRelateds(member.Relateds, leadingWhitespace);

                return GetNodeWithTrivia(leadingWhitespace, node, summary, remarks, seealsos, altmembers, relateds);
            }

            return node;
        }

        private static SyntaxNode GetNodeWithTrivia(SyntaxTriviaList leadingWhitespace, SyntaxNode node, params SyntaxTriviaList[] trivias)
        {
            SyntaxTriviaList finalTrivia = new();
            foreach (SyntaxTriviaList t in trivias)
            {
                finalTrivia = finalTrivia.AddRange(t);
            }
            if (finalTrivia.Count > 0)
            {
                finalTrivia = finalTrivia.AddRange(leadingWhitespace);

                var leadingTrivia = node.GetLeadingTrivia();
                if (leadingTrivia.Any())
                {
                    if (leadingTrivia[0].IsKind(SyntaxKind.EndOfLineTrivia))
                    {
                        // Ensure the endline that separates nodes is respected
                        finalTrivia = new SyntaxTriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed)
                            .AddRange(finalTrivia);
                    }
                }

                return node.WithLeadingTrivia(finalTrivia);
            }

            // If there was no new trivia, return untouched
            return node;
        }

        // Finds the last set of whitespace characters that are to the left of the public|protected keyword of the node.
        private static SyntaxTriviaList GetLeadingWhitespace(SyntaxNode node)
        {
            if (node is MemberDeclarationSyntax memberDeclaration)
            {
                if (memberDeclaration.Modifiers.FirstOrDefault(x => x.IsKind(SyntaxKind.PublicKeyword) || x.IsKind(SyntaxKind.ProtectedKeyword)) is SyntaxToken publicModifier)
                {
                    if (publicModifier.LeadingTrivia.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia)) is SyntaxTrivia last)
                    {
                        return new(last);
                    }
                }
            }
            return new();
        }

        private static SyntaxTriviaList GetSummary(DocsAPI api, SyntaxTriviaList leadingWhitespace)
        {
            if (!api.Summary.IsDocsEmpty())
            {
                XmlTextSyntax contents = GetTextAsCommentedTokens(api.Summary, leadingWhitespace);
                XmlElementSyntax element = SyntaxFactory.XmlSummaryElement(contents);
                return GetXmlTrivia(element, leadingWhitespace);
            }

            return new();
        }

        private static SyntaxTriviaList GetRemarks(DocsAPI api, SyntaxTriviaList leadingWhitespace)
        {
            if (!api.Remarks.IsDocsEmpty())
            {
                string text = GetRemarksWithXmlElements(api);
                XmlTextSyntax contents = GetTextAsCommentedTokens(text, leadingWhitespace);
                XmlElementSyntax xmlRemarks = SyntaxFactory.XmlRemarksElement(contents);
                return GetXmlTrivia(xmlRemarks, leadingWhitespace);
            }

            return new();
        }

        /// <summary>
        /// <see langword="virtual"static"sealed"await"async"abstract"
        /// </summary>
        /// <param name="api"></param>
        /// <returns></returns>
        private static string GetRemarksWithXmlElements(IDocsAPI api)
        {
            string remarks = api.Remarks;

            if (!api.Remarks.IsDocsEmpty())
            {
                remarks = Regex.Replace(remarks, @"<!\[CDATA\[(\r?\n)*[\t ]*", "");
                remarks = Regex.Replace(remarks, @"\]\]>", "");
                remarks = Regex.Replace(remarks, @"##[ ]?Remarks(\r?\n)*[\t ]*", "");
                remarks = Regex.Replace(remarks, @"(?<xref><xref\:(?<DocId>[a-zA-Z0-9_\.]+)(?<extraVars>\?[a-zA-Z0-9_]+=[a-zA-Z0-9_])?>)", "<see cref=\"${DocId}\" />");

                    MatchCollection collection = Regex.Matches(api.Remarks, @"(?<backtickedParam>`(?<paramName>[a-zA-Z0-9_]+)`)");

                foreach (Match match in collection)
                {
                    string backtickedParam = match.Groups["backtickedParam"].Value;
                    string paramName = match.Groups["paramName"].Value;
                    if(ReservedKeywords.Any(x => x == paramName))
                    {
                        remarks = Regex.Replace(remarks, $"{backtickedParam}", $"<see langword=\"{paramName}\" />");
                    }
                    else if (api.Params.Any(x => x.Name == paramName))
                    {
                        remarks = Regex.Replace(remarks, $"{backtickedParam}", $"<paramref name=\"{paramName}\" />");
                    }
                    else if (api.TypeParams.Any(x => x.Name == paramName))
                    {
                        remarks = Regex.Replace(remarks, $"{backtickedParam}", $"<typeparamref name=\"{paramName}\" />");
                    }
                }
            }
            return remarks;
        }

        private static SyntaxTriviaList GetValue(DocsMember api, SyntaxTriviaList leadingWhitespace)
        {
            if (!api.Value.IsDocsEmpty())
            {
                XmlTextSyntax contents = GetTextAsCommentedTokens(api.Value, leadingWhitespace);
                XmlElementSyntax element = SyntaxFactory.XmlValueElement(contents);
                return GetXmlTrivia(element, leadingWhitespace);
            }

            return new();
        }

        private static SyntaxTriviaList GetParameter(string name, string text, SyntaxTriviaList leadingWhitespace)
        {
            if (!text.IsDocsEmpty())
            {
                XmlTextSyntax contents = GetTextAsCommentedTokens(text, leadingWhitespace);
                XmlElementSyntax element = SyntaxFactory.XmlParamElement(name, contents);
                return GetXmlTrivia(element, leadingWhitespace);
            }

            return new();
        }

        private static SyntaxTriviaList GetParameters(DocsAPI api, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList parameters = new();
            foreach (SyntaxTriviaList parameterTrivia in api.Params
                    .Where(param => !param.Value.IsDocsEmpty())
                    .Select(param => GetParameter(param.Name, param.Value, leadingWhitespace)))
            {
                parameters = parameters.AddRange(parameterTrivia);
            }
            return parameters;
        }

        private static SyntaxTriviaList GetTypeParam(string name, string text, SyntaxTriviaList leadingWhitespace)
        {
            if (!text.IsDocsEmpty())
            {
                var attribute = new SyntaxList<XmlAttributeSyntax>(SyntaxFactory.XmlTextAttribute("name", name));
                XmlTextSyntax contents = GetTextAsCommentedTokens(text, leadingWhitespace);
                return GetXmlTrivia("typeparam", attribute, contents, leadingWhitespace);
            }

            return new();
        }

        private static SyntaxTriviaList GetTypeParameters(DocsAPI api, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList typeParameters = new();
            foreach (SyntaxTriviaList typeParameterTrivia in api.TypeParams
                        .Where(typeParam => !typeParam.Value.IsDocsEmpty())
                        .Select(typeParam => GetTypeParam(typeParam.Name, typeParam.Value, leadingWhitespace)))
            {
                typeParameters = typeParameters.AddRange(typeParameterTrivia);
            }
            return typeParameters;
        }

        private static SyntaxTriviaList GetReturns(DocsMember api, SyntaxTriviaList leadingWhitespace)
        {
            // Also applies for when <returns> is empty because the method return type is void
            if (!api.Returns.IsDocsEmpty())
            {
                XmlTextSyntax contents = GetTextAsCommentedTokens(api.Returns, leadingWhitespace);
                XmlElementSyntax element = SyntaxFactory.XmlReturnsElement(contents);
                return GetXmlTrivia(element, leadingWhitespace);
            }

            return new();
        }

        private static SyntaxTriviaList GetException(string cref, string text, SyntaxTriviaList leadingWhitespace)
        {
            if (!text.IsDocsEmpty())
            {
                TypeCrefSyntax crefSyntax = SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName(cref.WithoutDocIdPrefixes()));
                //XmlTextSyntax contents = SyntaxFactory.XmlText(GetTextAsTokens(text.WithoutPrefix(), leadingWhitespace));
                XmlTextSyntax contents = GetTextAsCommentedTokens(text, leadingWhitespace);
                XmlElementSyntax element = SyntaxFactory.XmlExceptionElement(crefSyntax, contents);
                return GetXmlTrivia(element, leadingWhitespace);
            }

            return new();
        }

        private static SyntaxTriviaList GetExceptions(List<DocsException> docsExceptions, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList exceptions = new();
            if (docsExceptions.Any())
            {
                foreach (SyntaxTriviaList exceptionsTrivia in docsExceptions.Select(
                    exception => GetException(exception.Cref, exception.Value, leadingWhitespace)))
                {
                    exceptions = exceptions.AddRange(exceptionsTrivia);
                }
            }
            return exceptions;
        }

        private static SyntaxTriviaList GetSeeAlso(string cref, SyntaxTriviaList leadingWhitespace)
        {
            TypeCrefSyntax crefSyntax = SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName(cref.WithoutDocIdPrefixes()));
            XmlEmptyElementSyntax element = SyntaxFactory.XmlSeeAlsoElement(crefSyntax);
            return GetXmlTrivia(element, leadingWhitespace);
        }

        private static SyntaxTriviaList GetSeeAlsos(List<string> docsSeeAlsoCrefs, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList seealsos = new();
            if (docsSeeAlsoCrefs.Any())
            {
                foreach (SyntaxTriviaList seealsoTrivia in docsSeeAlsoCrefs.Select(
                    s => GetSeeAlso(s, leadingWhitespace)))
                {
                    seealsos = seealsos.AddRange(seealsoTrivia);
                }
            }
            return seealsos;
        }

        private static SyntaxTriviaList GetAltMember(string cref, SyntaxTriviaList leadingWhitespace)
        {
            XmlAttributeSyntax attribute = SyntaxFactory.XmlTextAttribute("cref", cref.WithoutDocIdPrefixes());
            XmlEmptyElementSyntax emptyElement = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName(SyntaxFactory.Identifier("altmember")), new SyntaxList<XmlAttributeSyntax>(attribute));
            return GetXmlTrivia(emptyElement, leadingWhitespace);
        }

        private static SyntaxTriviaList GetAltMembers(List<string> docsAltMembers, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList altMembers = new();
            if (docsAltMembers.Any())
            {
                foreach (SyntaxTriviaList altMemberTrivia in docsAltMembers.Select(
                    s => GetAltMember(s, leadingWhitespace)))
                {
                    altMembers = altMembers.AddRange(altMemberTrivia);
                }
            }
            return altMembers;
        }

        private static SyntaxTriviaList GetRelated(string articleType, string href, string value, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxList<XmlAttributeSyntax> attributes = new();

            attributes = attributes.Add(SyntaxFactory.XmlTextAttribute("type", articleType));
            attributes = attributes.Add(SyntaxFactory.XmlTextAttribute("href", href));

            XmlTextSyntax contents = GetTextAsCommentedTokens(value, leadingWhitespace);
            return GetXmlTrivia("related", attributes, contents, leadingWhitespace);
        }

        private static SyntaxTriviaList GetRelateds(List<DocsRelated> docsRelateds, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxTriviaList relateds = new();
            if (docsRelateds.Any())
            {
                foreach (SyntaxTriviaList relatedsTrivia in docsRelateds.Select(
                    s => GetRelated(s.ArticleType, s.Href, s.Value, leadingWhitespace)))
                {
                    relateds = relateds.AddRange(relatedsTrivia);
                }
            }
            return relateds;
        }

        /*
        XmlText
            XmlTextLiteralNewLineToken (XmlTextSyntax) -> endline
            XmlTextLiteralToken (XmlTextLiteralToken) -> [ text]
                Lead: DocumentationCommentExteriorTrivia (SyntaxTrivia) -> [    /// ]
         */
        private static XmlTextSyntax GetTextAsCommentedTokens(string text, SyntaxTriviaList leadingWhitespace)
        {
            text = text.WithoutDocIdPrefixes();

            // collapse newlines to a single one
            string whitespace = Regex.Replace(leadingWhitespace.ToFullString(), @"(\r?\n)+", "");
            SyntaxToken whitespaceToken = SyntaxFactory.XmlTextNewLine(Environment.NewLine + whitespace);

            SyntaxTrivia leadingTrivia = SyntaxFactory.SyntaxTrivia(SyntaxKind.DocumentationCommentExteriorTrivia, string.Empty);
            SyntaxTriviaList leading = SyntaxTriviaList.Create(leadingTrivia);
            
            var tokens = new List<SyntaxToken>();

            string[] lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
            {
                string line = lines[lineNumber];

                SyntaxToken token = SyntaxFactory.XmlTextLiteral(leading, line, line, default);
                tokens.Add(token);

                if (lines.Length > 1 && lineNumber < lines.Length - 1)
                {
                    tokens.Add(whitespaceToken);
                }
            }

            XmlTextSyntax xmlText = SyntaxFactory.XmlText(tokens.ToArray());
            return xmlText;
        }

        private static SyntaxTriviaList GetXmlTrivia(XmlNodeSyntax node, SyntaxTriviaList leadingWhitespace)
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
        private static SyntaxTriviaList GetXmlTrivia(string name, SyntaxList<XmlAttributeSyntax> attributes, XmlTextSyntax contents, SyntaxTriviaList leadingWhitespace)
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

            XmlElementSyntax element = SyntaxFactory.XmlElement(start, new SyntaxList<XmlNodeSyntax>(contents), end);
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
