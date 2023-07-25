// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ApiDocsSync.PortToTripleSlash
{
    internal class XmlHelper
    {
        private static readonly Dictionary<string, string> _replaceableNormalElementPatterns = new Dictionary<string, string> {
            { "<c>null</c>",    "<see langword=\"null\" />"},
            { "<c>true</c>",    "<see langword=\"true\" />"},
            { "<c>false</c>",   "<see langword=\"false\" />"},
            { " null ",         " <see langword=\"null\" /> " },
            { " true ",         " <see langword=\"true\" /> " },
            { " false ",        " <see langword=\"false\" /> " },
            { " null,",         " <see langword=\"null\" />," },
            { " true,",         " <see langword=\"true\" />," },
            { " false,",        " <see langword=\"false\" />," },
            { " null.",         " <see langword=\"null\" />." },
            { " true.",         " <see langword=\"true\" />." },
            { " false.",        " <see langword=\"false\" />." },
            { "null ",          "<see langword=\"null\" /> " },
            { "true ",          "<see langword=\"true\" /> " },
            { "false ",         "<see langword=\"false\" /> " },
            { "Null ",          "<see langword=\"null\" /> " },
            { "True ",          "<see langword=\"true\" /> " },
            { "False ",         "<see langword=\"false\" /> " },
            { "></see>",        " />" }
        };

        private static readonly Dictionary<string, string> _replaceableMarkdownPatterns = new Dictionary<string, string> {
            { "<see langword=\"null\"/>",    "`null`" },
            { "<see langword=\"null\" />",   "`null`" },
            { "<see langword=\"true\"/>",    "`true`" },
            { "<see langword=\"true\" />",   "`true`" },
            { "<see langword=\"false\"/>",   "`false`" },
            { "<see langword=\"false\" />",  "`false`" },
            { "<see cref=\"T:",              "<xref:" },
            { "<see cref=\"F:",              "<xref:" },
            { "<see cref=\"M:",              "<xref:" },
            { "<see cref=\"P:",              "<xref:" },
            { "<see cref=\"",                "<xref:" },
            { " null ",                      " `null` " },
            { "'null'",                      "`null`" },
            { " null.",                      " `null`." },
            { " null,",                      " `null`," },
            { " false ",                     " `false` " },
            { "'false'",                     "`false`" },
            { " false.",                     " `false`." },
            { " false,",                     " `false`," },
            { " true ",                      " `true` " },
            { "'true'",                      "`true`" },
            { " true.",                      " `true`." },
            { " true,",                      " `true`," },
            { "null ",                       "`null` " },
            { "true ",                       "`true` " },
            { "false ",                      "`false` " },
            { "Null ",                       "`null` " },
            { "True ",                       "`true` " },
            { "False ",                      "`false` " },
            { "<c>",                         "`"},
            { "</c>",                        "`"},
            { "<para>",                      "" },
            { "</para>",                     "\r\n\r\n" },
            { "\" />",                       ">" },
            { "<![CDATA[",                   "" },
            { "]]>",                         "" },
            { "<note type=\"inheritinfo\">", ""},
            { "</note>",                     "" }
        };

        private static readonly Dictionary<string, string> _replaceableExceptionPatterns = new Dictionary<string, string>{

            { "<para>",  "\r\n" },
            { "</para>", "" }
        };

        private static readonly Dictionary<string, string> _replaceableMarkdownRegexPatterns = new Dictionary<string, string> {
            { @"\<paramref name\=""(?'paramrefContents'[a-zA-Z0-9_\-]+)""[ ]*\/\>",  @"`${paramrefContents}`" },
            { @"\<seealso cref\=""(?'seealsoContents'.+)""[ ]*\/\>",      @"seealsoContents" },
        };

        public static string GetAttributeValue(XElement parent, string name)
        {
            if (parent == null)
            {
                throw new Exception($"A null parent was passed when attempting to get attribute '{name}'");
            }
            else
            {
                XAttribute? attr = parent.Attribute(name);
                if (attr != null)
                {
                    return attr.Value.Trim();
                }
            }
            return string.Empty;
        }

        public static bool TryGetChildElement(XElement parent, string name, out XElement? child)
        {
            child = null;

            if (parent == null || string.IsNullOrWhiteSpace(name))
                return false;

            child = parent.Element(name);

            return child != null;
        }

        public static string GetChildElementValue(XElement parent, string childName)
        {
            XElement? child = parent.Element(childName);

            if (child != null)
            {
                return GetNodesInPlainText(child);
            }

            return string.Empty;
        }

        public static string GetNodesInPlainText(XElement element)
        {
            if (element == null)
            {
                throw new Exception("A null element was passed when attempting to retrieve the nodes in plain text.");
            }

            // string.Join("", element.Nodes()) is very slow.
            //
            // The following is twice as fast (although still slow)
            // but does not produce the same spacing. That may be OK.
            //
            //using var reader = element.CreateReader();
            //reader.MoveToContent();
            //return reader.ReadInnerXml().Trim();

            string actualValue = string.Join("", element.Nodes()).Trim();
            return actualValue.IsDocsEmpty() ? string.Empty : actualValue;
        }

        public static void SaveFormattedAsMarkdown(XElement element, string newValue, bool isMember)
        {
            if (element == null)
            {
                throw new Exception("A null element was passed when attempting to save formatted as markdown");
            }

            // Empty value because SaveChildElement will add a child to the parent, not replace it
            element.Value = string.Empty;

            XElement xeFormat = new XElement("format");

            string updatedValue = SubstituteRemarksRegexPatterns(newValue);
            updatedValue = ReplaceMarkdownPatterns(updatedValue).Trim();

            string remarksTitle = string.Empty;
            if (!updatedValue.Contains("## Remarks"))
            {
                remarksTitle = "## Remarks\r\n\r\n";
            }

            string spaces = isMember ? "          " : "      ";

            xeFormat.ReplaceAll(new XCData("\r\n\r\n" + remarksTitle + updatedValue + "\r\n\r\n" + spaces));

            // Attribute at the end, otherwise it would be replaced by ReplaceAll
            xeFormat.SetAttributeValue("type", "text/markdown");

            element.Add(xeFormat);
        }

        public static void AddChildFormattedAsMarkdown(XElement parent, XElement child, string childValue, bool isMember)
        {
            if (parent == null)
            {
                throw new Exception("A null parent was passed when attempting to add child formatted as markdown.");
            }

            if (child == null)
            {
                throw new Exception("A null child was passed when attempting to add child formatted as markdown.");
            }

            SaveFormattedAsMarkdown(child, childValue, isMember);
            parent.Add(child);
        }

        public static void SaveFormattedAsXml(XElement element, string newValue, bool removeUndesiredEndlines = true)
        {
            if (element == null)
            {
                throw new Exception("A null element was passed when attempting to save formatted as xml");
            }

            element.Value = string.Empty;

            var attributes = element.Attributes();

            string updatedValue = removeUndesiredEndlines ? RemoveUndesiredEndlines(newValue) : newValue;
            updatedValue = ReplaceNormalElementPatterns(updatedValue);

            // Workaround: <x> will ensure XElement does not complain about having an invalid xml object inside. Those tags will be removed by replacing the nodes.
            XElement parsedElement;
            try
            {
                parsedElement = XElement.Parse("<x>" + updatedValue + "</x>");
            }
            catch (XmlException)
            {
                parsedElement = XElement.Parse("<x>" + updatedValue.Replace("<", "&lt;").Replace(">", "&gt;") + "</x>");
            }

            element.ReplaceNodes(parsedElement.Nodes());

            // Ensure attributes are preserved after replacing nodes
            element.ReplaceAttributes(attributes);
        }

        public static void AppendFormattedAsXml(XElement element, string valueToAppend, bool removeUndesiredEndlines)
        {
            if (element == null)
            {
                throw new Exception("A null element was passed when attempting to append formatted as xml");
            }

            SaveFormattedAsXml(element, GetNodesInPlainText(element) + valueToAppend, removeUndesiredEndlines);
        }

        public static void AddChildFormattedAsXml(XElement parent, XElement child, string childValue)
        {
            if (parent == null)
            {
                throw new Exception("A null parent was passed when attempting to add child formatted as xml");
            }

            if (child == null)
            {
                throw new Exception("A null child was passed when attempting to add child formatted as xml");
            }

            SaveFormattedAsXml(child, childValue);
            parent.Add(child);
        }

        private static string RemoveUndesiredEndlines(string value)
        {
            value = Regex.Replace(value, @"((?'undesiredEndlinePrefix'[^\.\:])(\r\n)+[ \t]*)", @"${undesiredEndlinePrefix} ");

            return value.Trim();
        }

        private static string SubstituteRemarksRegexPatterns(string value)
        {
            return SubstituteRegexPatterns(value, _replaceableMarkdownRegexPatterns);
        }

        private static string ReplaceMarkdownPatterns(string value)
        {
            string updatedValue = value;
            foreach (KeyValuePair<string, string> kvp in _replaceableMarkdownPatterns)
            {
                if (updatedValue.Contains(kvp.Key))
                {
                    updatedValue = updatedValue.Replace(kvp.Key, kvp.Value);
                }
            }
            return updatedValue;
        }

        internal static string ReplaceExceptionPatterns(string value)
        {
            string updatedValue = value;
            foreach (KeyValuePair<string, string> kvp in _replaceableExceptionPatterns)
            {
                if (updatedValue.Contains(kvp.Key))
                {
                    updatedValue = updatedValue.Replace(kvp.Key, kvp.Value);
                }
            }

            updatedValue = Regex.Replace(updatedValue, @"[\r\n\t ]+\-[ ]?or[ ]?\-[\r\n\t ]+", "\r\n\r\n-or-\r\n\r\n");
            return updatedValue;
        }

        private static string ReplaceNormalElementPatterns(string value)
        {
            string updatedValue = value;
            foreach (KeyValuePair<string, string> kvp in _replaceableNormalElementPatterns)
            {
                if (updatedValue.Contains(kvp.Key))
                {
                    updatedValue = updatedValue.Replace(kvp.Key, kvp.Value);
                }
            }

            return updatedValue;
        }

        private static string SubstituteRegexPatterns(string value, Dictionary<string, string> replaceableRegexPatterns)
        {
            foreach (KeyValuePair<string, string> pattern in replaceableRegexPatterns)
            {
                Regex regex = new Regex(pattern.Key);
                if (regex.IsMatch(value))
                {
                    value = regex.Replace(value, pattern.Value);
                }
            }

            return value;
        }
    }
}
