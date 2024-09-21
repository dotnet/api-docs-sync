// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using ApiDocsSync.PortToTripleSlash.Docs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.Net.Mime.MediaTypeNames;

namespace ApiDocsSync.PortToTripleSlash.Roslyn;

internal class DocumentationUpdater
{
    private const string TripleSlash = "///";
    private const string Space = " ";
    private const string NewLine = "\n";
    private static readonly char[] _NewLineSeparators = ['\n', '\r'];
    private const StringSplitOptions _NewLineSplitOptions = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

    private readonly Configuration _config;
    private readonly IDocsAPI _api;
    private readonly SyntaxTrivia _indentationTrivia;

    public DocumentationUpdater(Configuration config, IDocsAPI api, SyntaxTrivia? indentationTrivia)
    {
        _config = config;
        _api = api;
        _indentationTrivia = indentationTrivia.HasValue ? indentationTrivia.Value : SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, string.Empty);
    }

    public DocumentationCommentTriviaSyntax GetUpdatedDocs(SyntaxList<XmlNodeSyntax> originalDocumentation)
    {
        List<XmlNodeSyntax> docsNodes = [];

        // Preserve the order in which each API element is looked for below

        if (!_api.Summary.IsDocsEmpty())
        {
            docsNodes.Add(GetSummaryNodeFromDocs());
        }
        else if (TryGet("summary") is XmlNodeSyntax existingSummary)
        {
            docsNodes.Add(GetExistingElementWithRequiredTrivia(existingSummary));
        }

        if (!_api.Value.IsDocsEmpty())
        {
            docsNodes.Add(GetValueNodeFromDocs());
        }
        else if (TryGet("value") is XmlNodeSyntax existingValue)
        {
            docsNodes.Add(GetExistingElementWithRequiredTrivia(existingValue));
        }

        foreach (DocsTypeParam typeParam in _api.TypeParams)
        {
            if (!typeParam.Value.IsDocsEmpty())
            {
                docsNodes.Add(GetTypeParamNode(typeParam));
            }
            else if (TryGet("typeparam", "name", typeParam.Value) is XmlNodeSyntax existingTypeParam)
            {
                docsNodes.Add(GetExistingElementWithRequiredTrivia(existingTypeParam));

            }
        }

        foreach (DocsParam param in _api.Params)
        {
            if (!param.Value.IsDocsEmpty())
            {
                docsNodes.Add(GetParamNode(param));
            }
            else if (TryGet("param", "name", param.Value) is XmlNodeSyntax existingParam)
            {
                docsNodes.Add(GetExistingElementWithRequiredTrivia(existingParam));

            }
        }

        if (!_api.Returns.IsDocsEmpty())
        {
            docsNodes.Add(GetReturnsNodeFromDocs());
        }
        else if (TryGet("returns") is XmlNodeSyntax existingReturns)
        {
            docsNodes.Add(GetExistingElementWithRequiredTrivia(existingReturns));
        }

        foreach (DocsException exception in _api.Exceptions)
        {
            if (!exception.Value.IsDocsEmpty())
            {
                docsNodes.Add(GetExceptionNode(exception));
            }
            else if (TryGet("exception", "cref", exception.Value) is XmlNodeSyntax existingException)
            {
                docsNodes.Add(GetExistingElementWithRequiredTrivia(existingException));
            }
        }

        // Only port them if that's the desired action, otherwise, preserve the existing ones
        if (!_config.SkipRemarks)
        {
            if (!_api.Remarks.IsDocsEmpty())
            {
                docsNodes.Add(GetRemarksNodeFromDocs());
            }
            else if (TryGet("remarks") is XmlNodeSyntax existingRemarks)
            {
                docsNodes.Add(GetExistingElementWithRequiredTrivia(existingRemarks));
            }
        }
        else if (TryGet("remarks") is XmlNodeSyntax existingRemarks)
        {
            docsNodes.Add(GetExistingElementWithRequiredTrivia(existingRemarks));
        }

        return SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            SyntaxFactory.List(docsNodes));

        XmlNodeSyntax? TryGet(string tagName, string? attributeName = null, string? attributeValue = null)
        {
            return originalDocumentation.FirstOrDefault(xmlNode => DoesNodeHaveTag(xmlNode, tagName, attributeName, attributeValue));
        }
    }

    public DocumentationCommentTriviaSyntax GetNewDocs()
    {
        List<XmlNodeSyntax> nodes = new();

        // Preserve the order
        if (!_api.Summary.IsDocsEmpty())
        {
            nodes.Add(GetSummaryNodeFromDocs());
        }
        if (!_api.Value.IsDocsEmpty())
        {
            nodes.Add(GetValueNodeFromDocs());
        }
        if (_api.TypeParams.Any())
        {
            nodes.AddRange(GetTypeParamNodesFromDocs());
        }
        if (_api.Params.Any())
        {
            nodes.AddRange(GetParamNodesFromDocs());
        }
        if (!_api.Returns.IsDocsEmpty())
        {
            nodes.Add(GetReturnsNodeFromDocs());
        }
        if (_api.Exceptions.Any())
        {
            nodes.AddRange(GetExceptionNodesFromDocs());
        }
        if (!_config.SkipRemarks && !_api.Remarks.IsDocsEmpty())
        {
            nodes.Add(GetRemarksNodeFromDocs());
        }

        return SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            SyntaxFactory.List(nodes));
    }

    private XmlNodeSyntax GetSummaryNodeFromDocs()
    {
        List<XmlNodeSyntax> internalTextNodes = [];

        bool startingTrivia = true;
        foreach (string line in _api.Summary.Split(_NewLineSeparators, _NewLineSplitOptions))
        {
            internalTextNodes.Add(GetFullTripleSlashSingleLineXmlTextSyntaxNode(line, startingTrivia));
            startingTrivia = false;
        }

        return GetXmlAttributedElementNode(internalTextNodes, "summary", keepTagsInSameLine: false);
    }

    private XmlNodeSyntax GetValueNodeFromDocs()
    {
        List<XmlNodeSyntax> internalTextNodes = GetNonSummaryFullTripleSlashSingleLineXmlTextSyntaxNodes(_api.Value);
        return GetXmlAttributedElementNode(internalTextNodes, "value");
    }

    private XmlNodeSyntax[] GetTypeParamNodesFromDocs()
    {
        List<XmlNodeSyntax> typeParamNodes = new();
        foreach (DocsTypeParam typeParam in _api.TypeParams)
        {
            typeParamNodes.Add(GetTypeParamNode(typeParam));
        }

        return typeParamNodes.ToArray();
    }

    private XmlNodeSyntax GetTypeParamNode(DocsTypeParam typeParam)
    {
        List<XmlNodeSyntax> internalTextNodes = GetNonSummaryFullTripleSlashSingleLineXmlTextSyntaxNodes(typeParam.Value);
        return GetXmlAttributedElementNode(internalTextNodes, "typeparam", "name", typeParam.Name);
    }

    private XmlNodeSyntax[] GetParamNodesFromDocs()
    {
        List<XmlNodeSyntax> paramNodes = new();
        foreach (DocsParam param in _api.Params)
        {
            paramNodes.Add(GetParamNode(param));
        }

        return paramNodes.ToArray();
    }

    private XmlNodeSyntax GetParamNode(DocsParam param)
    {
        List<XmlNodeSyntax> internalTextNodes = GetNonSummaryFullTripleSlashSingleLineXmlTextSyntaxNodes(param.Value);
        return GetXmlAttributedElementNode(internalTextNodes, "param", "name", param.Name);
    }

    private XmlNodeSyntax GetReturnsNodeFromDocs()
    {
        List<XmlNodeSyntax> internalTextNodes = GetNonSummaryFullTripleSlashSingleLineXmlTextSyntaxNodes(_api.Returns);
        return GetXmlAttributedElementNode(internalTextNodes, "returns");
    }

    private XmlNodeSyntax GetRemarksNodeFromDocs()
    {
        List<XmlNodeSyntax> internalTextNodes = GetNonSummaryFullTripleSlashSingleLineXmlTextSyntaxNodes(_api.Remarks);
        return GetXmlAttributedElementNode(internalTextNodes, "remarks");
    }

    private XmlNodeSyntax[] GetExceptionNodesFromDocs()
    {
        List<XmlNodeSyntax> exceptionNodes = new();
        foreach (DocsException exception in _api.Exceptions)
        {
            exceptionNodes.Add(GetExceptionNode(exception));
        }

        return exceptionNodes.ToArray();
    }

    private XmlNodeSyntax GetExceptionNode(DocsException exception)
    {
        List<XmlNodeSyntax> internalTextNodes = GetNonSummaryFullTripleSlashSingleLineXmlTextSyntaxNodes(exception.Value);
        return GetXmlAttributedElementNode(internalTextNodes, "exception", "cref", exception.Cref[2..]);
    }

    private XmlNodeSyntax GetXmlAttributedElementNode(IEnumerable<XmlNodeSyntax> content, string tagName, string? attributeName = null, string? attributeValue = null, bool keepTagsInSameLine = true)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(tagName));

        GetLeadingTrivia(out SyntaxTriviaList leadingTrivia);
        GetTrailingTrivia(out SyntaxTriviaList trailingTrivia);

        XmlElementStartTagSyntax startTag = SyntaxFactory
            .XmlElementStartTag(SyntaxFactory.XmlName(SyntaxFactory.Identifier(tagName)))
            .WithLeadingTrivia(leadingTrivia);

        if (!keepTagsInSameLine)
        {
            startTag = startTag.WithTrailingTrivia(trailingTrivia);
        }

        if (!string.IsNullOrWhiteSpace(attributeName))
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(attributeValue));

            SyntaxToken xmlAttributeName = SyntaxFactory.Identifier(
                leading: SyntaxFactory.TriviaList(SyntaxFactory.Space),
                text: attributeName,
                trailing: SyntaxFactory.TriviaList());

            XmlNameAttributeSyntax xmlAttribute = SyntaxFactory.XmlNameAttribute(
                    name: SyntaxFactory.XmlName(xmlAttributeName),
                    startQuoteToken: SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
                    identifier: SyntaxFactory.IdentifierName(attributeValue),
                    endQuoteToken: SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken));

            startTag = startTag.WithAttributes(SyntaxFactory.List<XmlAttributeSyntax>([xmlAttribute]));
        }

        XmlElementEndTagSyntax endTag = SyntaxFactory
            .XmlElementEndTag(SyntaxFactory.XmlName(SyntaxFactory.Identifier(tagName)))
            .WithTrailingTrivia(trailingTrivia);

        if (!keepTagsInSameLine)
        {
            endTag = endTag.WithLeadingTrivia(leadingTrivia);
        }

        return SyntaxFactory.XmlElement(startTag, SyntaxFactory.List(content), endTag);
    }

    private XmlNodeSyntax GetExistingElementWithRequiredTrivia(XmlNodeSyntax existingNode)
    {
        GetLeadingTrivia(out SyntaxTriviaList leadingTrivia);
        GetTrailingTrivia(out SyntaxTriviaList trailingTrivia);
        return existingNode.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia);
    }

    // Returns a single line of optional indentaiton, optional triple slashes, the optional line of text that may follow it, and the optional newline.
    // Examples:
    // - For the summary tag, leadingTrivia must always be true and trailingTrivia must always be true:
    //   [indentation][tripleslash][textline][newline]
    //     Example: ->->->/// text\n
    // - For all other tags, leadingTrivia must only be false in the first item and trailingTrivia must be false in the last item:
    //   First item: <tag>[textline][newline]
    //     Example: <tag>text\n
    //   Last item: [indentation][tripleslash][textline]</tag>
    //     Example: ->->->/// text</tag>
    private XmlTextSyntax GetFullTripleSlashSingleLineXmlTextSyntaxNode(string text, bool leadingTrivia = false, bool trailingTrivia = true)
    {
        GetIndentationSyntaxToken(out SyntaxToken indentationSyntaxToken);
        GetTripleSlashSyntaxToken(out SyntaxToken tripleSlashSyntaxToken);
        GetNewLineSyntaxToken(out SyntaxToken newLineSyntaxToken);

        List<SyntaxToken> list = [];

        if (leadingTrivia)
        {
            list.Add(indentationSyntaxToken);
            list.Add(tripleSlashSyntaxToken);
        }

        list.Add(SyntaxFactory.XmlTextNewLine(
                leading: SyntaxFactory.TriviaList(),
                text: text,
                value: text,
                trailing: SyntaxFactory.TriviaList()));

        if (trailingTrivia)
        {
            list.Add(newLineSyntaxToken);
        }

        return SyntaxFactory.XmlText(SyntaxFactory.TokenList(list));
    }

    private List<XmlNodeSyntax> GetNonSummaryFullTripleSlashSingleLineXmlTextSyntaxNodes(string text)
    {
        List<XmlNodeSyntax> nodes = [];
        string[] splitted = text.Split(_NewLineSeparators, _NewLineSplitOptions);
        for(int i = 0; i < splitted.Length; i++)
        {
            string line = splitted[i];
            nodes.Add(GetFullTripleSlashSingleLineXmlTextSyntaxNode(line, leadingTrivia: i > 0, trailingTrivia: i < (splitted.Length - 1)));
        }
        return nodes;
    }

    // Returns a syntax node containing the "/// "  text literal syntax token.
    private XmlTextSyntax GetTripleSlashTextSyntaxNode()
    {
        GetTripleSlashSyntaxToken(out SyntaxToken tripleSlashSyntaxToken);
        return SyntaxFactory.XmlText().WithTextTokens(SyntaxFactory.TokenList(tripleSlashSyntaxToken));
    }

    // Returns a syntax node containing the "\n" text literal syntax token.
    private XmlTextSyntax GetNewLineTextSyntaxNode()
    {
        GetNewLineSyntaxToken(out SyntaxToken newLineSyntaxToken);
        return SyntaxFactory.XmlText().WithTextTokens(SyntaxFactory.TokenList(newLineSyntaxToken));
    }

    // Returns a syntax node containing the specified indentation text literal syntax token.
    private XmlTextSyntax GetIndentationTextSyntaxNode()
    {
        GetIndentationSyntaxToken(out SyntaxToken indentationSyntaxToken);
        return SyntaxFactory.XmlText().WithTextTokens(SyntaxFactory.TokenList(indentationSyntaxToken));
    }

    // Returns a syntax token containing the "/// " text literal.
    private void GetTripleSlashSyntaxToken(out SyntaxToken tripleSlashSyntaxToken) =>
        tripleSlashSyntaxToken = SyntaxFactory.XmlTextLiteral(
            leading: SyntaxFactory.TriviaList(SyntaxFactory.DocumentationCommentExterior(TripleSlash)),
            text: Space,
            value: Space,
            trailing: SyntaxFactory.TriviaList());

    // Returns a syntax token containing the "\n" text literal.
    private void GetNewLineSyntaxToken(out SyntaxToken newLineSyntaxToken) =>
        newLineSyntaxToken = SyntaxFactory.XmlTextNewLine(
            leading: SyntaxFactory.TriviaList(),
            text: NewLine,
            value: NewLine,
            trailing: SyntaxFactory.TriviaList());

    // Returns a syntax token with the "" text literal preceded by the specified indentation trivia.
    private void GetIndentationSyntaxToken(out SyntaxToken indentationSyntaxToken) =>
        indentationSyntaxToken = SyntaxFactory.XmlTextLiteral(
            leading: SyntaxFactory.TriviaList(_indentationTrivia),
            text: string.Empty,
            value: string.Empty,
            trailing: SyntaxFactory.TriviaList());

    private void GetLeadingTrivia(out SyntaxTriviaList leadingTrivia)
    {
        leadingTrivia = SyntaxFactory.TriviaList(
            SyntaxFactory.Trivia(
                SyntaxFactory.DocumentationCommentTrivia(
                    SyntaxKind.SingleLineDocumentationCommentTrivia,
                    SyntaxFactory.List<XmlNodeSyntax>([GetIndentationTextSyntaxNode(), GetTripleSlashTextSyntaxNode()]))));
    }

    private void GetTrailingTrivia(out SyntaxTriviaList trailingTrivia)
    {
        trailingTrivia = SyntaxFactory.TriviaList(
            SyntaxFactory.Trivia(
                SyntaxFactory.DocumentationCommentTrivia(
                    SyntaxKind.SingleLineDocumentationCommentTrivia,
                    SyntaxFactory.SingletonList<XmlNodeSyntax>(GetNewLineTextSyntaxNode()))));
    }

    private static bool DoesNodeHaveTag(SyntaxNode xmlNode, string tagName, string? attributeName = null, string? attributeValue = null)
    {
        if (xmlNode.Kind() is SyntaxKind.XmlElement && xmlNode is XmlElementSyntax xmlElement)
        {
            bool hasNodeWithTag = xmlElement.StartTag.Name.LocalName.ValueText == tagName;

            // No attribute passed, we just want to check tag name
            if (string.IsNullOrWhiteSpace(attributeName))
            {
                return hasNodeWithTag;
            }

            // To check attribute, attributeValue must also be passed
            return !string.IsNullOrWhiteSpace(attributeValue) &&
                xmlElement.StartTag.Attributes.FirstOrDefault(a => a.Name.LocalName.ValueText == attributeName) is XmlTextAttributeSyntax xmlAttribute &&
                xmlAttribute.TextTokens.ToString() == attributeValue;
        }

        return false;
    }
}
