using Libraries.Docs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Libraries.RoslynTripleSlash
{
    public static class XmlDocComments
    {
        private static readonly string[] ReservedKeywords = new[] { "abstract", "async", "await", "false", "null", "sealed", "static", "true", "virtual" };

        private static readonly Dictionary<string, string> PrimitiveTypes = new()
        {
            { "System.Boolean", "bool" },
            { "System.Byte",    "byte" },
            { "System.Char",    "char" },
            { "System.Decimal", "decimal" },
            { "System.Double",  "double" },
            { "System.Int16",   "short" },
            { "System.Int32",   "int" },
            { "System.Int64",   "long" },
            { "System.Object",  "object" }, // Ambiguous: could be 'object' or 'dynamic' https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types
            { "System.SByte",   "sbyte" },
            { "System.Single",  "float" },
            { "System.String",  "string" },
            { "System.UInt16",  "ushort" },
            { "System.UInt32",  "uint" },
            { "System.UInt64",  "ulong" },
            { "System.Void",    "void" }
        };

        // Note that we need to support generics that use the ` literal as well as any url encoded character
        private static readonly string ValidRegexChars = @"[A-Za-z0-9\-\._~:\/#\[\]\{\}@!\$&'\(\)\*\+,;]|`\d+|%\w{2}";
        private static readonly string ValidExtraChars = @"\?=";

        private static readonly string RegexDocIdPattern = @"(?<prefix>[A-Za-z]{1}:)?(?<docId>(" + ValidRegexChars + @")+)?(?<extraVars>\?(" + ValidRegexChars + @")+=(" + ValidRegexChars + @")+)?";
        private static readonly string RegexXmlCrefPattern = "cref=\"" + RegexDocIdPattern + "\"";
        private static readonly string RegexMarkdownXrefPattern = @"(?<xref><xref:" + RegexDocIdPattern + ">)";

        private static readonly string RegexMarkdownBoldPattern = @"\*\*(?<content>[A-Za-z0-9\-\._~:\/#\[\]@!\$&'\(\)\+,;%` ]+)\*\*";
        private static readonly string RegexXmlBoldReplacement = @"<b>${content}</b>";

        private static readonly string RegexMarkdownLinkPattern = @"\[(?<linkValue>.+)\]\((?<linkURL>(http|www)(" + ValidRegexChars + "|" + ValidExtraChars + @")+)\)";
        private static readonly string RegexHtmlLinkReplacement = "<a href=\"${linkURL}\">${linkValue}</a>";

        private static readonly string RegexMarkdownCodeStartPattern = @"```(?<language>(cs|csharp|cpp|vb|visualbasic))(?<spaces>\s+)";
        private static readonly string RegexXmlCodeStartReplacement = "<code class=\"lang-${language}\">${spaces}";

        private static readonly string RegexMarkdownCodeEndPattern = @"```(?<spaces>\s+)";
        private static readonly string RegexXmlCodeEndReplacement = "</code>${spaces}";
        private static readonly string[] MarkdownUnconvertableStrings = new[] { "](~/includes", "[!INCLUDE" };

        private static readonly string[] MarkdownCodeIncludes = new[] { "[!code-cpp", "[!code-csharp", "[!code-vb", };

        private static readonly string[] MarkdownExamples = new[] { "## Examples", "## Example" };

        private static readonly string[] MarkdownHeaders = new[] { "[!NOTE]", "[!IMPORTANT]", "[!TIP]" };

        public static SyntaxList<XmlNodeSyntax> GetXmlCommentLines(string[] commentLines)
        {
            var xmlTokens = commentLines
                .Select(l => XmlTextLiteral(l, l))
                .Zip(Enumerable.Repeat(XmlTextNewLine(Environment.NewLine), commentLines.Length - 1))
                .SelectMany((pair) => new[] { pair.First, pair.Second });

            var xmlText = XmlText(TokenList(xmlTokens));

            return SingletonList<XmlNodeSyntax>(xmlText);
        }

        public static XmlNodeSyntax? GetSummary(DocsSummary summary)
        {
            var text = summary.ParsedText;

            if (!text.IsDocsEmpty())
            {
                return XmlElement("summary", SingletonList<XmlNodeSyntax>(XmlText(XmlTextLiteral(text, text))));
            }

            return null;
        }

        public static SyntaxTriviaList GetSummary(DocsAPI api, SyntaxTriviaList leadingWhitespace)
        {
            if (!api.Summary.IsDocsEmpty())
            {
                XmlTextSyntax content = GetTextAsCommentedTokens(api.Summary, leadingWhitespace);
                XmlElementSyntax element = XmlSummaryElement(content);
                return GetXmlTrivia(element, leadingWhitespace);
            }

            return new();
        }

        public static XmlNodeSyntax? GetRemarks(DocsRemarks remarks)
        {
            var text = remarks.ParsedText;

            if (!text.IsDocsEmpty())
            {
                return XmlElement("remarks", SingletonList<XmlNodeSyntax>(XmlText(XmlTextLiteral(text, text))));
            }

            return null;
        }

        public static SyntaxTriviaList GetRemarks(DocsAPI api, SyntaxTriviaList leadingWhitespace)
        {
            if (!api.Remarks.IsDocsEmpty())
            {
                return GetFormattedRemarks(api, leadingWhitespace);
            }

            return new();
        }

        public static XmlNodeSyntax? GetExample(DocsExample example)
        {
            if (!example.ParsedText.IsDocsEmpty())
            {
                var content = XmlText(example.ParsedText);
                return XmlExampleElement(content);
            }

            return null;
        }

        public static IEnumerable<XmlNodeSyntax> GetExamples(IEnumerable<DocsExample> examples)
        {
            return examples
                .Where(e => !e.ParsedText?.IsDocsEmpty() ?? false)
                .Select(GetExample)!;
        }

        public static XmlNodeSyntax? GetValue(string value)
        {
            if (!value.IsDocsEmpty())
            {
                return XmlValueElement(XmlText(value));
            }

            return null;
        }

        public static SyntaxTriviaList GetValue(DocsMember api, SyntaxTriviaList leadingWhitespace)
        {
            if (!api.Value.IsDocsEmpty())
            {
                XmlTextSyntax contents = GetTextAsCommentedTokens(api.Value, leadingWhitespace);
                XmlElementSyntax element = XmlValueElement(contents);
                return GetXmlTrivia(element, leadingWhitespace);
            }

            return new();
        }

        public static XmlNodeSyntax? GetParameter(DocsParam parameter)
        {
            if (!parameter.ParsedText.IsDocsEmpty())
            {
                var content = XmlText(parameter.ParsedText);
                return XmlParamElement(parameter.Name, content);
            }

            return null;
        }

        public static SyntaxTriviaList GetParameter(string name, string text, SyntaxTriviaList leadingWhitespace)
        {
            if (!text.IsDocsEmpty())
            {
                XmlTextSyntax contents = GetTextAsCommentedTokens(text, leadingWhitespace);
                XmlElementSyntax element = XmlParamElement(name, contents);
                return GetXmlTrivia(element, leadingWhitespace);
            }

            return new();
        }

        public static IEnumerable<XmlNodeSyntax> GetParameters(IEnumerable<DocsParam> parameters)
        {
            return parameters
                .Where(param => !param.ParsedText.IsDocsEmpty())
                .Select(GetParameter)!;
        }

        public static SyntaxTriviaList GetParameters(DocsAPI api, SyntaxTriviaList leadingWhitespace)
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

        public static XmlNodeSyntax? GetTypeParameter(DocsTypeParam param)
        {
            if (!param.ParsedText.IsDocsEmpty())
            {
                var attribute = new SyntaxList<XmlAttributeSyntax>(XmlTextAttribute("name", param.Name));
                var content = XmlText(param.ParsedText);

                return GetXmlNode("typeparam", attribute, content);
            }

            return null;
        }

        public static SyntaxTriviaList GetTypeParam(string name, string text, SyntaxTriviaList leadingWhitespace)
        {
            if (!text.IsDocsEmpty())
            {
                var attribute = new SyntaxList<XmlAttributeSyntax>(XmlTextAttribute("name", name));
                XmlTextSyntax contents = GetTextAsCommentedTokens(text, leadingWhitespace);
                return GetXmlTrivia("typeparam", attribute, contents, leadingWhitespace);
            }

            return new();
        }

        public static IEnumerable<XmlNodeSyntax> GetTypeParameters(IEnumerable<DocsTypeParam> typeParams)
        {
            return typeParams
                .Where(typeParam => !typeParam.ParsedText.IsDocsEmpty())
                .Select(GetTypeParameter)!;
        }

        public static SyntaxTriviaList GetTypeParameters(DocsAPI api, SyntaxTriviaList leadingWhitespace)
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

        public static XmlNodeSyntax? GetReturns(string returns)
        {
            if (!returns.IsDocsEmpty())
            {
                return XmlReturnsElement(XmlText(returns));
            }

            return null;
        }

        public static SyntaxTriviaList GetReturns(DocsMember api, SyntaxTriviaList leadingWhitespace)
        {
            // Also applies for when <returns> is empty because the method return type is void
            if (!api.Returns.IsDocsEmpty())
            {
                XmlTextSyntax contents = GetTextAsCommentedTokens(api.Returns, leadingWhitespace);
                XmlElementSyntax element = XmlReturnsElement(contents);
                return GetXmlTrivia(element, leadingWhitespace);
            }

            return new();
        }

        public static XmlNodeSyntax GetException(DocsException exception)
        {
            var withoutPrefix = RemoveCrefPrefix(exception.Cref);
            var crefType = TypeCref(ParseTypeName(withoutPrefix));

            return XmlExceptionElement(crefType, XmlText(XmlTextLiteral(exception.Value, exception.Value)));
        }

        public static SyntaxTriviaList GetException(string cref, string text, SyntaxTriviaList leadingWhitespace)
        {
            if (!text.IsDocsEmpty())
            {
                cref = RemoveCrefPrefix(cref);
                TypeCrefSyntax crefSyntax = TypeCref(ParseTypeName(cref));
                XmlTextSyntax contents = GetTextAsCommentedTokens(text, leadingWhitespace);
                XmlElementSyntax element = XmlExceptionElement(crefSyntax, contents);
                return GetXmlTrivia(element, leadingWhitespace);
            }

            return new();
        }

        public static IEnumerable<XmlNodeSyntax> GetExceptions(List<DocsException> docsExceptions) =>
            docsExceptions.Select(GetException);

        public static SyntaxTriviaList GetExceptions(List<DocsException> docsExceptions, SyntaxTriviaList leadingWhitespace)
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

        public static XmlNodeSyntax GetSeeAlso(string cref)
        {
            var withoutPrefix = RemoveCrefPrefix(cref);
            var crefType = TypeCref(ParseTypeName(withoutPrefix));

            return XmlSeeAlsoElement(crefType);
        }

        public static SyntaxTriviaList GetSeeAlso(string cref, SyntaxTriviaList leadingWhitespace)
        {
            cref = RemoveCrefPrefix(cref);
            TypeCrefSyntax crefSyntax = TypeCref(ParseTypeName(cref));
            XmlEmptyElementSyntax element = XmlSeeAlsoElement(crefSyntax);
            return GetXmlTrivia(element, leadingWhitespace);
        }

        public static IEnumerable<XmlNodeSyntax> GetSeeAlsos(List<string> docsSeeAlsoCrefs) =>
            docsSeeAlsoCrefs.Select(GetSeeAlso);

        public static SyntaxTriviaList GetSeeAlsos(List<string> docsSeeAlsoCrefs, SyntaxTriviaList leadingWhitespace)
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

        public static XmlNodeSyntax GetAltMember(string cref)
        {
            var withoutPrefix = RemoveCrefPrefix(cref);
            var crefType = MapDocIdGenericsToCrefGenerics(withoutPrefix);

            return XmlEmptyElement(XmlName("altmember"), new(new[] { XmlTextAttribute("cref", crefType) }));
        }

        public static SyntaxTriviaList GetAltMember(string cref, SyntaxTriviaList leadingWhitespace)
        {
            cref = RemoveCrefPrefix(cref);
            cref = MapDocIdGenericsToCrefGenerics(cref);
            XmlAttributeSyntax attribute = XmlTextAttribute("cref", cref);
            XmlEmptyElementSyntax emptyElement = XmlEmptyElement(XmlName(Identifier("altmember")), new SyntaxList<XmlAttributeSyntax>(attribute));
            return GetXmlTrivia(emptyElement, leadingWhitespace);
        }

        public static IEnumerable<XmlNodeSyntax> GetAltMembers(List<string> docsAltMembers)
            => docsAltMembers.Select(GetAltMember);

        public static SyntaxTriviaList GetAltMembers(List<string> docsAltMembers, SyntaxTriviaList leadingWhitespace)
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

        public static XmlNodeSyntax GetRelated(DocsRelated related)
        {
            var attributes = new[]
            {
                XmlTextAttribute("type", related.ArticleType),
                XmlTextAttribute("href", related.Href)
            };

            var start = XmlElementStartTag(XmlName("related"), new(attributes));
            var end = XmlElementEndTag(XmlName("related"));

            return XmlElement(start, new(XmlText(XmlTextLiteral(related.Value))), end);
        }

        public static SyntaxTriviaList GetRelated(string articleType, string href, string value, SyntaxTriviaList leadingWhitespace)
        {
            SyntaxList<XmlAttributeSyntax> attributes = new();

            attributes = attributes.Add(XmlTextAttribute("type", articleType));
            attributes = attributes.Add(XmlTextAttribute("href", href));

            XmlTextSyntax contents = GetTextAsCommentedTokens(value, leadingWhitespace);
            return GetXmlTrivia("related", attributes, contents, leadingWhitespace);
        }

        public static IEnumerable<XmlNodeSyntax> GetRelateds(List<DocsRelated> docsRelateds) =>
            docsRelateds.Select(GetRelated);

        public static SyntaxTriviaList GetRelateds(List<DocsRelated> docsRelateds, SyntaxTriviaList leadingWhitespace)
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

        private static XmlTextSyntax GetTextAsCommentedTokens(string text, SyntaxTriviaList leadingWhitespace, bool wrapWithNewLines = false)
        {
            text = CleanCrefs(text);

            // collapse newlines to a single one
            string whitespace = Regex.Replace(leadingWhitespace.ToFullString(), @"(\r?\n)+", "");
            SyntaxToken whitespaceToken = XmlTextNewLine(Environment.NewLine + whitespace);

            SyntaxTrivia leadingTrivia = SyntaxTrivia(SyntaxKind.DocumentationCommentExteriorTrivia, string.Empty);
            SyntaxTriviaList leading = SyntaxTriviaList.Create(leadingTrivia);

            string[] lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var tokens = new List<SyntaxToken>();

            if (wrapWithNewLines)
            {
                tokens.Add(whitespaceToken);
            }

            for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
            {
                string line = lines[lineNumber];

                SyntaxToken token = XmlTextLiteral(leading, line, line, default);
                tokens.Add(token);

                if (lines.Length > 1 && lineNumber < lines.Length - 1)
                {
                    tokens.Add(whitespaceToken);
                }
            }

            if (wrapWithNewLines)
            {
                tokens.Add(whitespaceToken);
            }

            XmlTextSyntax xmlText = XmlText(tokens.ToArray());
            return xmlText;
        }

        private static SyntaxTriviaList GetXmlTrivia(XmlNodeSyntax node, SyntaxTriviaList leadingWhitespace)
        {
            DocumentationCommentTriviaSyntax docComment = DocumentationComment(node);
            SyntaxTrivia docCommentTrivia = Trivia(docComment);

            return leadingWhitespace
                .Add(docCommentTrivia)
                .Add(CarriageReturnLineFeed);
        }

        private static XmlNodeSyntax GetXmlNode(string name, SyntaxList<XmlAttributeSyntax> attributes, XmlTextSyntax content)
        {
            var start = XmlElementStartTag(
                Token(SyntaxKind.LessThanToken),
                XmlName(Identifier(name)),
                attributes,
                Token(SyntaxKind.GreaterThanToken));

            var end = XmlElementEndTag(
                Token(SyntaxKind.LessThanSlashToken),
                XmlName(Identifier(name)),
                Token(SyntaxKind.GreaterThanToken));

            return XmlElement(start, new(content), end);
        }

        // Generates a custom SyntaxTrivia object containing a triple slashed xml element with optional attributes.
        // Looks like below (excluding square brackets):
        // [    /// <element attribute1="value1" attribute2="value2">text</element>]
        private static SyntaxTriviaList GetXmlTrivia(string name, SyntaxList<XmlAttributeSyntax> attributes, XmlTextSyntax contents, SyntaxTriviaList leadingWhitespace)
        {
            XmlElementStartTagSyntax start = XmlElementStartTag(
                Token(SyntaxKind.LessThanToken),
                XmlName(Identifier(name)),
                attributes,
                Token(SyntaxKind.GreaterThanToken));

            XmlElementEndTagSyntax end = XmlElementEndTag(
                Token(SyntaxKind.LessThanSlashToken),
                XmlName(Identifier(name)),
                Token(SyntaxKind.GreaterThanToken));

            XmlElementSyntax element = XmlElement(start, new SyntaxList<XmlNodeSyntax>(contents), end);
            return GetXmlTrivia(element, leadingWhitespace);
        }

        private static string WrapInRemarks(string acum)
        {
            string wrapped = Environment.NewLine + "<format type=\"text/markdown\"><![CDATA[" + Environment.NewLine;
            wrapped += acum;
            wrapped += Environment.NewLine + "]]></format>" + Environment.NewLine;
            return wrapped;
        }

        private static string WrapCodeIncludes(string[] splitted, ref int n)
        {
            string acum = string.Empty;
            while (n < splitted.Length && splitted[n].ContainsStrings(MarkdownCodeIncludes))
            {
                acum += Environment.NewLine + splitted[n];
                if ((n + 1) < splitted.Length && splitted[n + 1].ContainsStrings(MarkdownCodeIncludes))
                {
                    n++;
                }
                else
                {
                    break;
                }
            }
            return WrapInRemarks(acum);
        }

        private static SyntaxTriviaList GetFormattedRemarks(IDocsAPI api, SyntaxTriviaList leadingWhitespace)
        {

            string remarks = RemoveUnnecessaryMarkdown(api.Remarks);
            string example = string.Empty;

            XmlNodeSyntax contents;
            if (remarks.ContainsStrings(MarkdownUnconvertableStrings))
            {
                contents = GetTextAsFormatCData(remarks, leadingWhitespace);
            }
            else
            {
                string[] splitted = remarks.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                string updatedRemarks = string.Empty;
                for (int n = 0; n < splitted.Length; n++)
                {
                    string acum;
                    string line = splitted[n];
                    if (line.ContainsStrings(MarkdownHeaders))
                    {
                        acum = line;
                        n++;
                        while (n < splitted.Length && splitted[n].StartsWith(">"))
                        {
                            acum += Environment.NewLine + splitted[n];
                            if ((n + 1) < splitted.Length && splitted[n + 1].StartsWith(">"))
                            {
                                n++;
                            }
                            else
                            {
                                break;
                            }
                        }
                        updatedRemarks += WrapInRemarks(acum);
                    }
                    else if (line.ContainsStrings(MarkdownCodeIncludes))
                    {
                        updatedRemarks += WrapCodeIncludes(splitted, ref n);
                    }
                    // When an example is found, everything after the header is considered part of that section
                    else if (line.Contains("## Example"))
                    {
                        n++;
                        while (n < splitted.Length)
                        {
                            line = splitted[n];
                            if (line.ContainsStrings(MarkdownCodeIncludes))
                            {
                                example += WrapCodeIncludes(splitted, ref n);
                            }
                            else
                            {
                                example += Environment.NewLine + line;
                            }
                            n++;
                        }
                    }
                    else
                    {
                        updatedRemarks += ReplaceMarkdownWithXmlElements(Environment.NewLine + line, api.Params, api.TypeParams);
                    }
                }

                contents = GetTextAsCommentedTokens(updatedRemarks, leadingWhitespace);
            }

            XmlElementSyntax remarksXml = XmlRemarksElement(contents);
            SyntaxTriviaList result = GetXmlTrivia(remarksXml, leadingWhitespace);

            if (!string.IsNullOrWhiteSpace(example))
            {
                SyntaxTriviaList exampleTriviaList = GetFormattedExamples(api, example, leadingWhitespace);
                result = result.AddRange(exampleTriviaList);
            }

            return result;
        }

        private static SyntaxTriviaList GetFormattedExamples(IDocsAPI api, string example, SyntaxTriviaList leadingWhitespace)
        {
            example = ReplaceMarkdownWithXmlElements(example, api.Params, api.TypeParams);
            XmlNodeSyntax exampleContents = GetTextAsCommentedTokens(example, leadingWhitespace);
            XmlElementSyntax exampleXml = XmlExampleElement(exampleContents);
            SyntaxTriviaList exampleTriviaList = GetXmlTrivia(exampleXml, leadingWhitespace);
            return exampleTriviaList;
        }

        private static XmlNodeSyntax GetTextAsFormatCData(string text, SyntaxTriviaList leadingWhitespace)
        {
            XmlTextSyntax remarks = GetTextAsCommentedTokens(text, leadingWhitespace, wrapWithNewLines: true);

            XmlNameSyntax formatName = XmlName("format");
            XmlAttributeSyntax formatAttribute = XmlTextAttribute("type", "text/markdown");
            var formatAttributes = new SyntaxList<XmlAttributeSyntax>(formatAttribute);

            var formatStart = XmlElementStartTag(formatName, formatAttributes);
            var formatEnd = XmlElementEndTag(formatName);

            XmlCDataSectionSyntax cdata = XmlCDataSection(remarks.TextTokens);
            var cdataList = new SyntaxList<XmlNodeSyntax>(cdata);

            XmlElementSyntax contents = XmlElement(formatStart, cdataList, formatEnd);

            return contents;
        }

        private static string RemoveUnnecessaryMarkdown(string text)
        {
            text = Regex.Replace(text, @"<!\[CDATA\[(\r?\n)*[\t ]*", "");
            text = Regex.Replace(text, @"\]\]>", "");
            text = Regex.Replace(text, @"##[ ]?Remarks(\r?\n)*[\t ]*", "");
            return text;
        }

        private static string ReplaceMarkdownWithXmlElements(string text, List<DocsParam> docsParams, List<DocsTypeParam> docsTypeParams)
        {
            text = CleanXrefs(text);

            // commonly used url entities
            text = Regex.Replace(text, @"%23", "#");
            text = Regex.Replace(text, @"%28", "(");
            text = Regex.Replace(text, @"%29", ")");
            text = Regex.Replace(text, @"%2C", ",");

            // hyperlinks
            text = Regex.Replace(text, RegexMarkdownLinkPattern, RegexHtmlLinkReplacement);

            // bold
            text = Regex.Replace(text, RegexMarkdownBoldPattern, RegexXmlBoldReplacement);

            // code snippet
            text = Regex.Replace(text, RegexMarkdownCodeStartPattern, RegexXmlCodeStartReplacement);
            text = Regex.Replace(text, RegexMarkdownCodeEndPattern, RegexXmlCodeEndReplacement);

            // langwords|parameters|typeparams and other type references within markdown backticks
            MatchCollection collection = Regex.Matches(text, @"(?<backtickContent>`(?<backtickedApi>[a-zA-Z0-9_]+(?<genericType>\<(?<typeParam>[a-zA-Z0-9_,]+)\>){0,1})`)");
            foreach (Match match in collection)
            {
                string backtickContent = match.Groups["backtickContent"].Value;
                string backtickedApi = match.Groups["backtickedApi"].Value;
                Group genericType = match.Groups["genericType"];
                Group typeParam = match.Groups["typeParam"];

                if (genericType.Success && typeParam.Success)
                {
                    backtickedApi = backtickedApi.Replace(genericType.Value, $"{{{typeParam.Value}}}");
                }

                if (ReservedKeywords.Any(x => x == backtickedApi))
                {
                    text = Regex.Replace(text, $"{backtickContent}", $"<see langword=\"{backtickedApi}\" />");
                }
                else if (docsParams.Any(x => x.Name == backtickedApi))
                {
                    text = Regex.Replace(text, $"{backtickContent}", $"<paramref name=\"{backtickedApi}\" />");
                }
                else if (docsTypeParams.Any(x => x.Name == backtickedApi))
                {
                    text = Regex.Replace(text, $"{backtickContent}", $"<typeparamref name=\"{backtickedApi}\" />");
                }
                else
                {
                    text = Regex.Replace(text, $"{backtickContent}", $"<see cref=\"{backtickedApi}\" />");
                }
            }

            return text;
        }

        // Removes the one letter prefix and the following colon, if found, from a cref.
        private static string RemoveCrefPrefix(string cref)
        {
            if (cref.Length > 2 && cref[1] == ':')
            {
                return cref[2..];
            }
            return cref;
        }

        private static string ReplacePrimitives(string text)
        {
            foreach ((string key, string value) in PrimitiveTypes)
            {
                text = Regex.Replace(text, key, value);
            }
            return text;
        }

        private static string ReplaceDocId(Match m)
        {
            string docId = m.Groups["docId"].Value;
            string? prefix = m.Groups["prefix"].Value == "O:" ? "O:" : null;
            docId = ReplacePrimitives(docId);
            docId = System.Net.WebUtility.UrlDecode(docId);

            // Strip '*' character from the tail end of DocId names
            if (docId.EndsWith('*'))
            {
                prefix = "O:";
                docId = docId[..^1];
            }

            return prefix + MapDocIdGenericsToCrefGenerics(docId);
        }

        private static string MapDocIdGenericsToCrefGenerics(string docId)
        {
            // Map DocId generic parameters to Xml Doc generic parameters
            // need to support both single and double backtick syntax
            const string GenericParameterPattern = @"`{1,2}([\d+])";
            int genericParameterArity = 0;
            return Regex.Replace(docId, GenericParameterPattern, MapDocIdGenericParameterToXmlDocGenericParameter);

            string MapDocIdGenericParameterToXmlDocGenericParameter(Match match)
            {
                int index = int.Parse(match.Groups[1].Value);

                if (genericParameterArity == 0)
                {
                    // this is the first match that declares the generic parameter arity of the method
                    // e.g. GenericMethod``3 ---> GenericMethod{T1,T2,T3}(...);
                    Debug.Assert(index > 0);
                    genericParameterArity = index;
                    return WrapInCurlyBrackets(string.Join(",", Enumerable.Range(0, index).Select(CreateGenericParameterName)));
                }

                // Subsequent matches are references to generic parameters in the method signature,
                // e.g. GenericMethod{T1,T2,T3}(..., List{``1} parameter, ...); ---> List{T2} parameter
                return CreateGenericParameterName(index);

                // NB this naming scheme does not map to the exact generic parameter names,
                // however this is still accepted by intellisense and backporters can rename
                // manually with the help of tooling.
                string CreateGenericParameterName(int index)
                    => genericParameterArity == 1 ? "T" : $"T{index + 1}";

                static string WrapInCurlyBrackets(string input) => $"{{{input}}}";
            }
        }

        private static string CrefEvaluator(Match m)
        {
            string docId = ReplaceDocId(m);
            return "cref=\"" + docId + "\"";
        }

        private static string CleanCrefs(string text)
        {
            text = Regex.Replace(text, RegexXmlCrefPattern, CrefEvaluator);
            return text;
        }

        private static string XrefEvaluator(Match m)
        {
            string docId = ReplaceDocId(m);
            return "<see cref=\"" + docId + "\" />";
        }

        private static string CleanXrefs(string text)
        {
            text = Regex.Replace(text, RegexMarkdownXrefPattern, XrefEvaluator);
            return text;
        }
    }
}
