using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Libraries.Docs
{
    public abstract class DocsTextElement
    {
        private readonly XElement Element;
        private IEnumerable<string>? _parsedNodes;
        private string[]? _parsedLines;
        private string? _parsedText;

        public string RawText { get; private init; }

        public IEnumerable<XNode> RawNodes { get; private init; }

        public IEnumerable<string> ParsedNodes
        {
            get
            {
                EnsureParsed();
                return _parsedNodes;
            }
        }

        public string ParsedText
        {
            get
            {
                EnsureParsed();
                return _parsedText;
            }
        }

        public string[] ParsedTextLines
        {
            get
            {
                EnsureParsed();
                return _parsedLines;
            }
        }

        [MemberNotNull(nameof(_parsedNodes), nameof(_parsedText), nameof(_parsedLines))]
        protected void EnsureParsed()
        {
            if (_parsedNodes is null || _parsedText is null || _parsedLines is null)
            {
                // Clone the element for non-mutating parsing
                var cloned = XElement.Parse(Element.ToString()).Nodes();

                // Parse each node and filter out nulls, building a block of text
                _parsedNodes = cloned.Select(ParseNode).OfType<string>().ToArray();
                var allNodeContent = string.Join("", _parsedNodes);

                // Normalize line endings, trim lines, remove empty lines, and join back into 1 string
                allNodeContent = Regex.Replace(allNodeContent, "\r?\n", Environment.NewLine);
                _parsedLines = allNodeContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                _parsedText = string.Join(Environment.NewLine, _parsedLines);
            }
        }

        public DocsTextElement(XElement element)
        {
            Element = element;
            RawNodes = element.Nodes();
            RawText = string.Join("", RawNodes);
        }

        protected virtual string? ParseNode(XNode node) =>
            RewriteDocReferences(node).ToString();

        protected static XNode RewriteDocReferences(XNode node)
        {
            if (node is XElement element && element.Name == "see")
            {
                var cref = element.Attribute("cref");

                if (cref is not null)
                {
                    var apiReference = new DocsApiReference(cref.Value);
                    cref.SetValue(apiReference.Api);
                }
            }

            return node;
        }
    }
}
