// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ApiDocsSync.Libraries
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

        private static readonly Dictionary<string, string> _replaceableNormalElementRegexPatterns = new Dictionary<string, string>
        {
            // Replace primitives: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types
            { @"\<(see|seealso){1} cref\=""bool""[ ]*\/\>",    "<see cref=\"T:System.Boolean\" />" },
            { @"\<(see|seealso){1} cref\=""byte""[ ]*\/\>",    "<see cref=\"T:System.Byte\" />" },
            { @"\<(see|seealso){1} cref\=""sbyte""[ ]*\/\>",   "<see cref=\"T:System.SByte\" />" },
            { @"\<(see|seealso){1} cref\=""char""[ ]*\/\>",    "<see cref=\"T:System.Char\" />" },
            { @"\<(see|seealso){1} cref\=""decimal""[ ]*\/\>", "<see cref=\"T:System.Decimal\" />" },
            { @"\<(see|seealso){1} cref\=""double""[ ]*\/\>",  "<see cref=\"T:System.Double\" />" },
            { @"\<(see|seealso){1} cref\=""float""[ ]*\/\>",   "<see cref=\"T:System.Single\" />" },
            { @"\<(see|seealso){1} cref\=""int""[ ]*\/\>",     "<see cref=\"T:System.Int32\" />" },
            { @"\<(see|seealso){1} cref\=""uint""[ ]*\/\>",    "<see cref=\"T:System.UInt32\" />" },
            { @"\<(see|seealso){1} cref\=""nint""[ ]*\/\>",    "<see cref=\"T:System.IntPtr\" />" },
            { @"\<(see|seealso){1} cref\=""nuint""[ ]*\/\>",   "<see cref=\"T:System.UIntPtr\" />" },
            { @"\<(see|seealso){1} cref\=""long""[ ]*\/\>",    "<see cref=\"T:System.Int64\" />" },
            { @"\<(see|seealso){1} cref\=""ulong""[ ]*\/\>",   "<see cref=\"T:System.UInt64\" />" },
            { @"\<(see|seealso){1} cref\=""short""[ ]*\/\>",   "<see cref=\"T:System.Int16\" />" },
            { @"\<(see|seealso){1} cref\=""ushort""[ ]*\/\>",  "<see cref=\"T:System.UInt16\" />" },
            { @"\<(see|seealso){1} cref\=""object""[ ]*\/\>",  "<see cref=\"T:System.Object\" />" },
            { @"\<(see|seealso){1} cref\=""dynamic""[ ]*\/\>", "<see langword=\"dynamic\" />" },
            { @"\<(see|seealso){1} cref\=""string""[ ]*\/\>",  "<see cref=\"T:System.String\" />" },
        };

        private static readonly Dictionary<string, string> _replaceableMarkdownPatterns = new Dictionary<string, string> {
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
            // Replace primitives: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types
            { @"\<(see|seealso){1} cref\=""bool""[ ]*\/\>",    "`bool`" },
            { @"\<(see|seealso){1} cref\=""byte""[ ]*\/\>",    "`byte`" },
            { @"\<(see|seealso){1} cref\=""sbyte""[ ]*\/\>",   "`sbyte`" },
            { @"\<(see|seealso){1} cref\=""char""[ ]*\/\>",    "`char`" },
            { @"\<(see|seealso){1} cref\=""decimal""[ ]*\/\>", "`decimal`" },
            { @"\<(see|seealso){1} cref\=""double""[ ]*\/\>",  "`double`" },
            { @"\<(see|seealso){1} cref\=""float""[ ]*\/\>",   "`float`" },
            { @"\<(see|seealso){1} cref\=""int""[ ]*\/\>",     "`int`" },
            { @"\<(see|seealso){1} cref\=""uint""[ ]*\/\>",    "`uint`" },
            { @"\<(see|seealso){1} cref\=""nint""[ ]*\/\>",    "`nint`" },
            { @"\<(see|seealso){1} cref\=""nuint""[ ]*\/\>",   "`nuint`" },
            { @"\<(see|seealso){1} cref\=""long""[ ]*\/\>",    "`long`" },
            { @"\<(see|seealso){1} cref\=""ulong""[ ]*\/\>",   "`ulong`" },
            { @"\<(see|seealso){1} cref\=""short""[ ]*\/\>",   "`short`" },
            { @"\<(see|seealso){1} cref\=""ushort""[ ]*\/\>",  "`ushort`" },
            { @"\<(see|seealso){1} cref\=""object""[ ]*\/\>",  "`object`" },
            { @"\<(see|seealso){1} cref\=""dynamic""[ ]*\/\>", "`dynamic`" },
            { @"\<(see|seealso){1} cref\=""string""[ ]*\/\>",  "`string`" },
            // Full DocId
            { @"\<(see|seealso){1} cref\=""([a-zA-Z0-9]{1}\:)?(?'seeContents'[a-zA-Z0-9\._\-\{\}\<\>\(\)\,\#\@\&\*\+]+)""[ ]*\/\>",      @"<xref:${seeContents}>" },
            // Params, typeparams, langwords
            { @"\<(typeparamref|paramref){1} name\=""(?'refNameContents'[a-zA-Z0-9_\-]+)""[ ]*\/\>",  @"`${refNameContents}`" },
            { @"\<see langword\=""(?'seeLangwordContents'[a-zA-Z0-9_\-]+)""[ ]*\/\>",  @"`${seeLangwordContents}`" },
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

            return string.Join("", element.Nodes()).Trim();
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

            string updatedValue = SubstituteRegexPatterns(newValue, _replaceableMarkdownRegexPatterns);
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
            updatedValue = SubstituteRegexPatterns(updatedValue, _replaceableNormalElementRegexPatterns);

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
