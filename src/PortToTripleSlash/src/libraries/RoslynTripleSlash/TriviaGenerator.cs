// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ApiDocsSync.PortToTripleSlash.Docs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ApiDocsSync.PortToTripleSlash.Roslyn;

internal class TriviaGenerator
{
    private static readonly string UnixNewLine = "\n";

    private static readonly string[] ReservedKeywords = new[] { "abstract", "async", "await", "false", "null", "sealed", "static", "true", "virtual" };

    private static readonly string[] MarkdownUnconvertableStrings = new[] { "](~/includes", "[!INCLUDE" };

    private static readonly string[] MarkdownCodeIncludes = new[] { "[!code-cpp", "[!code-csharp", "[!code-vb", };

    private static readonly string[] MarkdownExamples = new[] { "## Examples", "## Example" };

    private static readonly string[] MarkdownHeaders = new[] { "[!NOTE]", "[!IMPORTANT]", "[!TIP]" };

    // Note that we need to support generics that use the ` literal as well as the escaped %60
    private static readonly string ValidRegexChars = @"[A-Za-z0-9\-\._~:\/#\[\]\{\}@!\$&'\(\)\*\+,;]|(%60|`)\d+";
    private static readonly string ValidExtraChars = @"\?=";

    private static readonly string RegexDocIdPattern = @"(?<prefix>[A-Za-z]{1}:)?(?<docId>(" + ValidRegexChars + @")+)(?<overload>%2[aA])?(?<extraVars>\?(" + ValidRegexChars + @")+=(" + ValidRegexChars + @")+)?";
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

    private static readonly Dictionary<string, string> PrimitiveTypes = new()
        {
            { "System.Boolean", "bool" },
            { "System.Byte", "byte" },
            { "System.Char", "char" },
            { "System.Decimal", "decimal" },
            { "System.Double", "double" },
            { "System.Int16", "short" },
            { "System.Int32", "int" },
            { "System.Int64", "long" },
            { "System.Object", "object" }, // Ambiguous: could be 'object' or 'dynamic' https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types
            { "System.SByte", "sbyte" },
            { "System.Single", "float" },
            { "System.String", "string" },
            { "System.UInt16", "ushort" },
            { "System.UInt32", "uint" },
            { "System.UInt64", "ulong" },
            { "System.Void", "void" }
        };

    private readonly Configuration _config;
    private readonly SyntaxNode _node;
    private readonly DocsMember? _member;
    private readonly DocsType? _type;
    private readonly APIKind _kind;

    private TriviaGenerator(Configuration config, SyntaxNode node, APIKind kind)
    {
        _config = config;
        _node = node;
        _kind = kind;
    }

    public TriviaGenerator(Configuration config, SyntaxNode node, DocsMember member) : this(config, node, APIKind.Member) => _member = member;

    public TriviaGenerator(Configuration config, SyntaxNode node, DocsType type) : this(config, node, APIKind.Type) => _type = type;

    public SyntaxNode Generate()
    {
        SyntaxTriviaList leadingWhitespace = GetLeadingWhitespace();
        SyntaxTriviaList leadingTrivia = _node.GetLeadingTrivia();
        DocsAPI? api = _kind == APIKind.Member ? _member : _type;
        ArgumentNullException.ThrowIfNull(api);

        SyntaxTriviaList summary = GetSummary(leadingTrivia, api, leadingWhitespace);
        SyntaxTriviaList remarks = GetRemarks(leadingTrivia, api, leadingWhitespace);
        SyntaxTriviaList seealsos = GetSeeAlsos(leadingTrivia, api.SeeAlsoCrefs, leadingWhitespace);
        SyntaxTriviaList altmembers = GetAltMembers(leadingTrivia, api.AltMembers, leadingWhitespace);
        SyntaxTriviaList relateds = GetRelateds(leadingTrivia, api.Relateds, leadingWhitespace);

        List<SyntaxTriviaList> trivias;
        if (_kind == APIKind.Member)
        {
            ArgumentNullException.ThrowIfNull(_member);

            switch (_member.MemberType)
            {
                case "Property":
                    {
                        SyntaxTriviaList value = GetValue(leadingTrivia, _member, leadingWhitespace);
                        SyntaxTriviaList exceptions = GetExceptions(leadingTrivia, _member.Exceptions, leadingWhitespace);

                        trivias = new() { summary, value, exceptions, remarks, seealsos, altmembers, relateds };
                    }
                    break;

                case "Method":
                    {
                        SyntaxTriviaList typeParameters = GetTypeParameters(leadingTrivia, _member, leadingWhitespace);
                        SyntaxTriviaList parameters = GetParameters(leadingTrivia, _member, leadingWhitespace);
                        SyntaxTriviaList returns = GetReturns(leadingTrivia, _member, leadingWhitespace);
                        SyntaxTriviaList exceptions = GetExceptions(leadingTrivia, _member.Exceptions, leadingWhitespace);

                        trivias = new() { summary, typeParameters, parameters, returns, exceptions, remarks, seealsos, altmembers, relateds };
                    }
                    break;

                case "Field":
                    {
                        trivias = new() { summary, remarks, seealsos, altmembers, relateds };
                    }
                    break;

                default: // All other members
                    {
                        SyntaxTriviaList exceptions = GetExceptions(leadingTrivia, _member.Exceptions, leadingWhitespace);

                        trivias = new() { summary, exceptions, remarks, seealsos, altmembers, relateds };
                    }
                    break;
            }
        }
        else
        {
            ArgumentNullException.ThrowIfNull(_type);

            SyntaxTriviaList typeParameters = GetTypeParameters(leadingTrivia, _type, leadingWhitespace);
            SyntaxTriviaList parameters = GetParameters(leadingTrivia, _type, leadingWhitespace);

            trivias = new() { summary, typeParameters, parameters, remarks, seealsos, altmembers, relateds };
        }

        return GetNodeWithTrivia(leadingWhitespace, trivias.ToArray());
    }

    private SyntaxNode GetNodeWithTrivia(SyntaxTriviaList leadingWhitespace, params SyntaxTriviaList[] trivias)
    {
        SyntaxTriviaList leadingDoubleSlashComments = GetLeadingDoubleSlashComments(leadingWhitespace);

        SyntaxTriviaList finalTrivia = new();
        foreach (SyntaxTriviaList t in trivias)
        {
            finalTrivia = finalTrivia.AddRange(t);
        }
        finalTrivia = finalTrivia.AddRange(leadingDoubleSlashComments);

        if (finalTrivia.Count > 0)
        {
            finalTrivia = finalTrivia.AddRange(leadingWhitespace);

            var leadingTrivia = _node.GetLeadingTrivia();
            if (leadingTrivia.Any())
            {
                if (leadingTrivia[0].IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    // Ensure the endline that separates nodes is respected
                    finalTrivia = new SyntaxTriviaList(SyntaxFactory.ElasticLineFeed)
                        .AddRange(finalTrivia);
                }
            }

            return _node.WithLeadingTrivia(finalTrivia);
        }

        // If there was no new trivia, return untouched
        return _node;
    }

    // Finds the last set of whitespace characters that are to the left of the public|protected keyword of the node.
    private SyntaxTriviaList GetLeadingWhitespace()
    {
        SyntaxTriviaList triviaList = GetLeadingTrivia();

        if (triviaList.Any() &&
            triviaList.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia)) is SyntaxTrivia last)
        {
            return new(last);
        }

        return new();
    }

    private SyntaxTriviaList GetLeadingDoubleSlashComments(SyntaxTriviaList leadingWhitespace)
    {
        SyntaxTriviaList triviaList = GetLeadingTrivia();

        SyntaxTriviaList doubleSlashComments = new();

        foreach (SyntaxTrivia trivia in triviaList)
        {
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                doubleSlashComments = doubleSlashComments
                                        .AddRange(leadingWhitespace)
                                        .Add(trivia)
                                        .Add(SyntaxFactory.LineFeed);
            }
        }

        return doubleSlashComments;
    }

    private SyntaxTriviaList GetLeadingTrivia()
    {
        if (_node is MemberDeclarationSyntax memberDeclaration)
        {
            if ((memberDeclaration.Modifiers.FirstOrDefault(x => x.IsKind(SyntaxKind.PublicKeyword) || x.IsKind(SyntaxKind.ProtectedKeyword)) is SyntaxToken modifier) &&
                    !modifier.IsKind(SyntaxKind.None))
            {
                return modifier.LeadingTrivia;
            }

            return _node.GetLeadingTrivia();
        }

        return new();
    }

    // Collects all tags with of the same name from a SyntaxTriviaList.
    private SyntaxTriviaList FindTag(string tag, SyntaxTriviaList leadingWhitespace, SyntaxTriviaList from)
    {
        List<XmlNodeSyntax> list = new();
        foreach (var trivia in from)
        {
            if (trivia.GetStructure() is DocumentationCommentTriviaSyntax structure)
            {
                foreach (XmlNodeSyntax node in structure.Content)
                {
                    if (node is XmlEmptyElementSyntax emptyElement && emptyElement.Name.ToString() == tag)
                    {
                        list.Add(node);
                    }
                    else if (node is XmlElementSyntax element && element.StartTag.Name.ToString() == tag)
                    {
                        list.Add(node);
                    }
                }
            }
        }

        return list.Any() ? GetXmlTrivia(leadingWhitespace, list.ToArray()) : new();
    }

    private SyntaxTriviaList GetSummary(SyntaxTriviaList old, DocsAPI api, SyntaxTriviaList leadingWhitespace)
    {
        if (!api.Summary.IsDocsEmpty())
        {
            XmlTextSyntax contents = GetTextAsCommentedTokens(api.Summary, leadingWhitespace);
            XmlElementSyntax element = SyntaxFactory.XmlSummaryElement(contents);
            return GetXmlTrivia(leadingWhitespace, element);
        }

        return FindTag("summary", leadingWhitespace, old);
    }

    private SyntaxTriviaList GetRemarks(SyntaxTriviaList old, DocsAPI api, SyntaxTriviaList leadingWhitespace)
    {
        if (_config.SkipRemarks)
        {
            return SyntaxTriviaList.Empty;
        }

        if (!api.Remarks.IsDocsEmpty())
        {
            return GetFormattedRemarks(api, leadingWhitespace);
        }

        return FindTag("remarks", leadingWhitespace, old);
    }

    private SyntaxTriviaList GetValue(SyntaxTriviaList old, DocsMember api, SyntaxTriviaList leadingWhitespace)
    {
        if (!api.Value.IsDocsEmpty())
        {
            XmlTextSyntax contents = GetTextAsCommentedTokens(api.Value, leadingWhitespace);
            XmlElementSyntax element = SyntaxFactory.XmlValueElement(contents);
            return GetXmlTrivia(leadingWhitespace, element);
        }

        return FindTag("value", leadingWhitespace, old);
    }

    private SyntaxTriviaList GetParameter(string name, string text, SyntaxTriviaList leadingWhitespace)
    {
        if (!text.IsDocsEmpty())
        {
            XmlTextSyntax contents = GetTextAsCommentedTokens(text, leadingWhitespace);
            XmlElementSyntax element = SyntaxFactory.XmlParamElement(name, contents);
            return GetXmlTrivia(leadingWhitespace, element);
        }

        return new();
    }

    private SyntaxTriviaList GetParameters(SyntaxTriviaList old, DocsAPI api, SyntaxTriviaList leadingWhitespace)
    {
        if (!api.Params.HasItems())
        {
            return FindTag("param", leadingWhitespace, old);
        }
        SyntaxTriviaList parameters = new();
        foreach (SyntaxTriviaList parameterTrivia in api.Params
                .Where(param => !param.Value.IsDocsEmpty())
                .Select(param => GetParameter(param.Name, param.Value, leadingWhitespace)))
        {
            parameters = parameters.AddRange(parameterTrivia);
        }
        return parameters;
    }

    private SyntaxTriviaList GetTypeParam(string name, string text, SyntaxTriviaList leadingWhitespace)
    {
        if (!text.IsDocsEmpty())
        {
            var attribute = new SyntaxList<XmlAttributeSyntax>(SyntaxFactory.XmlTextAttribute("name", name));
            XmlTextSyntax contents = GetTextAsCommentedTokens(text, leadingWhitespace);
            return GetXmlTrivia("typeparam", attribute, contents, leadingWhitespace);
        }

        return new();
    }

    private SyntaxTriviaList GetTypeParameters(SyntaxTriviaList old, DocsAPI api, SyntaxTriviaList leadingWhitespace)
    {
        if (!api.TypeParams.HasItems())
        {
            return FindTag("typeparams", leadingWhitespace, old);
        }
        SyntaxTriviaList typeParameters = new();
        foreach (SyntaxTriviaList typeParameterTrivia in api.TypeParams
                    .Where(typeParam => !typeParam.Value.IsDocsEmpty())
                    .Select(typeParam => GetTypeParam(typeParam.Name, typeParam.Value, leadingWhitespace)))
        {
            typeParameters = typeParameters.AddRange(typeParameterTrivia);
        }
        return typeParameters;
    }

    private SyntaxTriviaList GetReturns(SyntaxTriviaList old, DocsMember api, SyntaxTriviaList leadingWhitespace)
    {
        // Also applies for when <returns> is empty because the method return type is void
        if (!api.Returns.IsDocsEmpty())
        {
            XmlTextSyntax contents = GetTextAsCommentedTokens(api.Returns, leadingWhitespace);
            XmlElementSyntax element = SyntaxFactory.XmlReturnsElement(contents);
            return GetXmlTrivia(leadingWhitespace, element);
        }

        return FindTag("returns", leadingWhitespace, old);
    }

    private SyntaxTriviaList GetException(string cref, string text, SyntaxTriviaList leadingWhitespace)
    {
        if (!text.IsDocsEmpty())
        {
            cref = RemoveCrefPrefix(cref);
            TypeCrefSyntax crefSyntax = SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName(cref));
            XmlTextSyntax contents = GetTextAsCommentedTokens(text, leadingWhitespace);
            XmlElementSyntax element = SyntaxFactory.XmlExceptionElement(crefSyntax, contents);
            return GetXmlTrivia(leadingWhitespace, element);
        }

        return new();
    }

    private SyntaxTriviaList GetExceptions(SyntaxTriviaList old, List<DocsException> docsExceptions, SyntaxTriviaList leadingWhitespace)
    {
        if (!docsExceptions.Any())
        {
            return FindTag("exception", leadingWhitespace, old);
        }
        SyntaxTriviaList exceptions = new();
        foreach (SyntaxTriviaList exceptionsTrivia in docsExceptions.Select(
            exception => GetException(exception.Cref, exception.Value, leadingWhitespace)))
        {
            exceptions = exceptions.AddRange(exceptionsTrivia);
        }
        return exceptions;
    }

    private SyntaxTriviaList GetSeeAlso(string cref, SyntaxTriviaList leadingWhitespace)
    {
        cref = RemoveCrefPrefix(cref);
        TypeCrefSyntax crefSyntax = SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName(cref));
        XmlEmptyElementSyntax element = SyntaxFactory.XmlSeeAlsoElement(crefSyntax);
        return GetXmlTrivia(leadingWhitespace, element);
    }

    private SyntaxTriviaList GetSeeAlsos(SyntaxTriviaList old, List<string> docsSeeAlsoCrefs, SyntaxTriviaList leadingWhitespace)
    {
        if (!docsSeeAlsoCrefs.Any())
        {
            return FindTag("seealso", leadingWhitespace, old);
        }
        SyntaxTriviaList seealsos = new();
        foreach (SyntaxTriviaList seealsoTrivia in docsSeeAlsoCrefs.Select(
            s => GetSeeAlso(s, leadingWhitespace)))
        {
            seealsos = seealsos.AddRange(seealsoTrivia);
        }
        return seealsos;
    }

    private SyntaxTriviaList GetAltMember(string cref, SyntaxTriviaList leadingWhitespace)
    {
        cref = RemoveCrefPrefix(cref);
        XmlAttributeSyntax attribute = SyntaxFactory.XmlTextAttribute("cref", cref);
        XmlEmptyElementSyntax emptyElement = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName(SyntaxFactory.Identifier("altmember")), new SyntaxList<XmlAttributeSyntax>(attribute));
        return GetXmlTrivia(leadingWhitespace, emptyElement);
    }

    private SyntaxTriviaList GetAltMembers(SyntaxTriviaList old, List<string> docsAltMembers, SyntaxTriviaList leadingWhitespace)
    {
        if (!docsAltMembers.Any())
        {
            return FindTag("altmember", leadingWhitespace, old);
        }
        SyntaxTriviaList altMembers = new();
        foreach (SyntaxTriviaList altMemberTrivia in docsAltMembers.Select(
            s => GetAltMember(s, leadingWhitespace)))
        {
            altMembers = altMembers.AddRange(altMemberTrivia);
        }
        return altMembers;
    }

    private SyntaxTriviaList GetRelated(string articleType, string href, string value, SyntaxTriviaList leadingWhitespace)
    {
        SyntaxList<XmlAttributeSyntax> attributes = new();

        attributes = attributes.Add(SyntaxFactory.XmlTextAttribute("type", articleType));
        attributes = attributes.Add(SyntaxFactory.XmlTextAttribute("href", href));

        XmlTextSyntax contents = GetTextAsCommentedTokens(value, leadingWhitespace);
        return GetXmlTrivia("related", attributes, contents, leadingWhitespace);
    }

    private SyntaxTriviaList GetRelateds(SyntaxTriviaList old, List<DocsRelated> docsRelateds, SyntaxTriviaList leadingWhitespace)
    {
        if (!docsRelateds.Any())
        {
            return FindTag("related", leadingWhitespace, old);
        }
        SyntaxTriviaList relateds = new();
        foreach (SyntaxTriviaList relatedsTrivia in docsRelateds.Select(
            s => GetRelated(s.ArticleType, s.Href, s.Value, leadingWhitespace)))
        {
            relateds = relateds.AddRange(relatedsTrivia);
        }
        return relateds;
    }

    private XmlTextSyntax GetTextAsCommentedTokens(string text, SyntaxTriviaList leadingWhitespace, bool wrapWithNewLines = false)
    {
        text = CleanCrefs(text);

        // collapse newlines to a single one
        string whitespace = Regex.Replace(leadingWhitespace.ToFullString(), @"(\r?\n)+", "");
        SyntaxToken whitespaceToken = SyntaxFactory.XmlTextNewLine(UnixNewLine + whitespace);

        SyntaxTrivia leadingTrivia = SyntaxFactory.SyntaxTrivia(SyntaxKind.DocumentationCommentExteriorTrivia, string.Empty);
        SyntaxTriviaList leading = SyntaxTriviaList.Create(leadingTrivia);

        string[] lines = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var tokens = new List<SyntaxToken>();

        if (wrapWithNewLines)
        {
            tokens.Add(whitespaceToken);
        }

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

        if (wrapWithNewLines)
        {
            tokens.Add(whitespaceToken);
        }

        XmlTextSyntax xmlText = SyntaxFactory.XmlText(tokens.ToArray());
        return xmlText;
    }

    private SyntaxTriviaList GetXmlTrivia(SyntaxTriviaList leadingWhitespace, params XmlNodeSyntax[] nodes)
    {
        DocumentationCommentTriviaSyntax docComment = SyntaxFactory.DocumentationComment(nodes);
        SyntaxTrivia docCommentTrivia = SyntaxFactory.Trivia(docComment);

        return leadingWhitespace
            .Add(docCommentTrivia)
            .Add(SyntaxFactory.LineFeed);
    }

    // Generates a custom SyntaxTrivia object containing a triple slashed xml element with optional attributes.
    // Looks like below (excluding square brackets):
    // [    /// <element attribute1="value1" attribute2="value2">text</element>]
    private SyntaxTriviaList GetXmlTrivia(string name, SyntaxList<XmlAttributeSyntax> attributes, XmlTextSyntax contents, SyntaxTriviaList leadingWhitespace)
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
        return GetXmlTrivia(leadingWhitespace, element);
    }

    private string WrapInRemarks(string acum)
    {
        string wrapped = UnixNewLine + "<format type=\"text/markdown\"><![CDATA[" + UnixNewLine;
        wrapped += acum;
        wrapped += UnixNewLine + "]]></format>" + UnixNewLine;
        return wrapped;
    }

    private string WrapCodeIncludes(string[] splitted, ref int n)
    {
        string acum = string.Empty;
        while (n < splitted.Length && splitted[n].ContainsStrings(MarkdownCodeIncludes))
        {
            acum += UnixNewLine + splitted[n];
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

    private SyntaxTriviaList GetFormattedRemarks(IDocsAPI api, SyntaxTriviaList leadingWhitespace)
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
            string[] splitted = remarks.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
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
                        acum += UnixNewLine + splitted[n];
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
                            example += UnixNewLine + line;
                        }
                        n++;
                    }
                }
                else
                {
                    updatedRemarks += ReplaceMarkdownWithXmlElements(UnixNewLine + line, api.Params, api.TypeParams);
                }
            }

            contents = GetTextAsCommentedTokens(updatedRemarks, leadingWhitespace);
        }

        XmlElementSyntax remarksXml = SyntaxFactory.XmlRemarksElement(contents);
        SyntaxTriviaList result = GetXmlTrivia(leadingWhitespace, remarksXml);

        if (!string.IsNullOrWhiteSpace(example))
        {
            SyntaxTriviaList exampleTriviaList = GetFormattedExamples(api, example, leadingWhitespace);
            result = result.AddRange(exampleTriviaList);
        }

        return result;
    }

    private SyntaxTriviaList GetFormattedExamples(IDocsAPI api, string example, SyntaxTriviaList leadingWhitespace)
    {
        example = ReplaceMarkdownWithXmlElements(example, api.Params, api.TypeParams);
        XmlNodeSyntax exampleContents = GetTextAsCommentedTokens(example, leadingWhitespace);
        XmlElementSyntax exampleXml = SyntaxFactory.XmlExampleElement(exampleContents);
        SyntaxTriviaList exampleTriviaList = GetXmlTrivia(leadingWhitespace, exampleXml);
        return exampleTriviaList;
    }

    private XmlNodeSyntax GetTextAsFormatCData(string text, SyntaxTriviaList leadingWhitespace)
    {
        XmlTextSyntax remarks = GetTextAsCommentedTokens(text, leadingWhitespace, wrapWithNewLines: true);

        XmlNameSyntax formatName = SyntaxFactory.XmlName("format");
        XmlAttributeSyntax formatAttribute = SyntaxFactory.XmlTextAttribute("type", "text/markdown");
        var formatAttributes = new SyntaxList<XmlAttributeSyntax>(formatAttribute);

        var formatStart = SyntaxFactory.XmlElementStartTag(formatName, formatAttributes);
        var formatEnd = SyntaxFactory.XmlElementEndTag(formatName);

        XmlCDataSectionSyntax cdata = SyntaxFactory.XmlCDataSection(remarks.TextTokens);
        var cdataList = new SyntaxList<XmlNodeSyntax>(cdata);

        XmlElementSyntax contents = SyntaxFactory.XmlElement(formatStart, cdataList, formatEnd);

        return contents;
    }

    private string RemoveUnnecessaryMarkdown(string text)
    {
        text = Regex.Replace(text, @"<!\[CDATA\[(\r?\n)*[\t ]*", "");
        text = Regex.Replace(text, @"\]\]>", "");
        text = Regex.Replace(text, @"##[ ]?Remarks(\r?\n)*[\t ]*", "");
        return text;
    }

    private string ReplaceMarkdownWithXmlElements(string text, List<DocsParam> docsParams, List<DocsTypeParam> docsTypeParams)
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

        // langwords|parameters|typeparams
        MatchCollection collection = Regex.Matches(text, @"(?<backtickedParam>`(?<paramName>[a-zA-Z0-9_]+)`)");
        foreach (Match match in collection)
        {
            string backtickedParam = match.Groups["backtickedParam"].Value;
            string paramName = match.Groups["paramName"].Value;
            if (ReservedKeywords.Any(x => x == paramName))
            {
                text = Regex.Replace(text, $"{backtickedParam}", $"<see langword=\"{paramName}\" />");
            }
            else if (docsParams.Any(x => x.Name == paramName))
            {
                text = Regex.Replace(text, $"{backtickedParam}", $"<paramref name=\"{paramName}\" />");
            }
            else if (docsTypeParams.Any(x => x.Name == paramName))
            {
                text = Regex.Replace(text, $"{backtickedParam}", $"<typeparamref name=\"{paramName}\" />");
            }
        }

        return text;
    }

    // Removes the one letter prefix and the following colon, if found, from a cref.
    private string RemoveCrefPrefix(string cref)
    {
        if (cref.Length > 2 && cref[1] == ':')
        {
            return cref[2..];
        }
        return cref;
    }

    private string ReplacePrimitives(string text)
    {
        foreach ((string key, string value) in PrimitiveTypes)
        {
            text = Regex.Replace(text, key, value);
        }
        return text;
    }

    private string ReplaceDocId(Match m)
    {
        string docId = m.Groups["docId"].Value;
        string overload = string.IsNullOrWhiteSpace(m.Groups["overload"].Value) ? "" : "O:";
        docId = ReplacePrimitives(docId);
        docId = Regex.Replace(docId, @"%60", "`");
        docId = Regex.Replace(docId, @"`\d", "{T}");
        return overload + docId;
    }

    private string CrefEvaluator(Match m)
    {
        string docId = ReplaceDocId(m);
        return "cref=\"" + docId + "\"";
    }

    private string CleanCrefs(string text)
    {
        text = Regex.Replace(text, RegexXmlCrefPattern, CrefEvaluator);
        return text;
    }

    private string XrefEvaluator(Match m)
    {
        string docId = ReplaceDocId(m);
        return "<see cref=\"" + docId + "\" />";
    }

    private string CleanXrefs(string text)
    {
        text = Regex.Replace(text, RegexMarkdownXrefPattern, XrefEvaluator);
        return text;
    }
}
