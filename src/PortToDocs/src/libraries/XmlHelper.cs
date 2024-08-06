// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ApiDocsSync.PortToDocs
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

        private static readonly Dictionary<string, string> _replaceableSeeAlsos = new Dictionary<string, string>
        {
            { "seealso cref", "see cref" }
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
            { @"<code data-dev-comment-type=""(?<elementName>[a-zA-Z0-9_]+)"">(?<elementValue>[a-zA-Z0-9_]+)</code>", "<see ${elementName}=\"${elementValue}\" />" },
            { @"<xref data-throw-if-not-resolved=""[a-zA-Z0-9_]+"" uid=""(?<docId>[a-zA-Z0-9_,\<\>\.\@\#\$%^&`\(\)]+)""><\/xref>", "<see cref=\"T:${docId}\" />" },
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
            { "\" />",                       ">" },
            { "<![CDATA[",                   "" },
            { "]]>",                         "" },
            { "<note type=\"inheritinfo\">", ""},
            { "</note>",                     "" }
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
            { @"\<(see|seealso){1} cref\=""([a-zA-Z0-9]{1}\:)?(?'seeContents'[a-zA-Z0-9\._\-\{\}\<\>\(\)\,\#\@\&\*\+\`]+)""[ ]*\/\>",      @"<xref:${seeContents}>" },
            // Replace "`" character in xref docIDs
            { @"`(?<=<xref:[^>]+)", @"%60" },
            // Replace "#" character in xref docIDs
            { @"#(?<=<xref:[^>]+)", @"%23" },
            // Params, typeparams, langwords
            { @"\<(typeparamref|paramref){1} name\=""(?'refNameContents'[a-zA-Z0-9_\-]+)""[ ]*\/\>",  @"`${refNameContents}`" },
            { @"\<see langword\=""(?'seeLangwordContents'[a-zA-Z0-9_\-]+)""[ ]*\/\>",  @"`${seeLangwordContents}`" },
            { @"<code data-dev-comment-type=""[a-zA-Z0-9_]+"">(?<elementValue>[a-zA-Z0-9_]+)</code>", "`${elementValue}`" },
            { @"<xref data-throw-if-not-resolved=""[a-zA-Z0-9_]+"" uid=""(?<docId>[a-zA-Z0-9_,\<\>\.]+)""><\/xref>", "<xref:${docId}>" },
        };

        private static readonly string[] _splittingSeparators = new string[] { "\r", "\n", "\r\n" };
        private static readonly StringSplitOptions _splittingStringSplitOptions = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries;

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

        public static string GetFormattedAsXml(string value, bool removeUndesiredEndlines)
        {
            string updatedValue = ReplaceEndLinesWithParas(value);
            updatedValue = removeUndesiredEndlines ? RemoveUndesiredEndlines(updatedValue) : updatedValue;
            updatedValue = ReplaceNormalElementPatterns(updatedValue);
            updatedValue = SubstituteRegexPatterns(updatedValue, _replaceableNormalElementRegexPatterns);
            return updatedValue;
        }

        private static string ReplaceEndLinesWithParas(string updatedValue)
        {
            string[] splitted = updatedValue.Split(_splittingSeparators, _splittingStringSplitOptions);
            bool moreThanOne = splitted.Count() > 1;

            StringBuilder newValue = new();
            foreach (string s in splitted)
            {
                if (moreThanOne && !s.StartsWith("<para>"))
                {
                    newValue.Append("<para>");
                }
                newValue.Append(s);
                if (moreThanOne && !s.EndsWith("</para>"))
                {
                    newValue.Append("</para>");
                }
            }

            return newValue.ToString();
        }

        public static string GetFormattedAsMarkdown(string value, bool isMember)
        {
            XElement xeFormat = new XElement("format");

            string updatedValue = SubstituteRegexPatterns(value, _replaceableMarkdownRegexPatterns);
            updatedValue = ReplaceMarkdownPatterns(updatedValue).Trim();

            string remarksTitle = string.Empty;
            if (!updatedValue.Contains("## Remarks"))
            {
                remarksTitle = "## Remarks\n\n";
            }

            string spaces = isMember ? "          " : "      ";

            xeFormat.ReplaceAll(new XCData("\n\n" + remarksTitle + updatedValue + "\n\n" + spaces));

            // Attribute at the end, otherwise it would be replaced by ReplaceAll
            xeFormat.SetAttributeValue("type", "text/markdown");

            return xeFormat.ToString();
        }

        public static void SaveAsIs(XElement element, string newValue)
        {
            if (element == null)
            {
                throw new Exception("A null element was passed when attempting to save text into it.");
            }

            element.Value = string.Empty;

            var attributes = element.Attributes();

            // Workaround: <x> will ensure XElement does not complain about having an invalid xml object inside. Those tags will be removed by replacing the nodes.
            XElement parsedElement;
            try
            {
                parsedElement = XElement.Parse("<x>" + newValue + "</x>");
            }
            catch (XmlException)
            {
                parsedElement = XElement.Parse("<x>" + newValue.Replace("<", "&lt;").Replace(">", "&gt;") + "</x>");
            }

            element.ReplaceNodes(parsedElement.Nodes());

            // Ensure attributes are preserved after replacing nodes
            element.ReplaceAttributes(attributes);
        }

        public static void SaveFormattedAsXml(XElement element, string newValue, bool removeUndesiredEndlines = true)
        {
            if (element == null)
            {
                throw new Exception("A null element was passed when attempting to save formatted as xml");
            }

            element.Value = string.Empty;

            var attributes = element.Attributes();

            string updatedValue = GetFormattedAsXml(newValue, removeUndesiredEndlines);

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
            value = Regex.Replace(value, @"((?'undesiredEndlinePrefix'[^\.\:])[\r\n]+[ \t]*)", @"${undesiredEndlinePrefix} ");

            return value.Trim();
        }

        internal static string ReplaceExceptionPatterns(string value) =>
            Regex.Replace(value, @"[\r\n\t ]+\-[ ]*(or|OR)[ ]*\-[\r\n\t ]+", "\n\n-or-\n\n");

        internal static string ReplaceSeeAlsos(string value) => ReplacePatterns(value, _replaceableSeeAlsos);

        private static string ReplaceMarkdownPatterns(string value) => ReplacePatterns(value, _replaceableMarkdownPatterns);

        private static string ReplaceNormalElementPatterns(string value) => ReplacePatterns(value, _replaceableNormalElementPatterns);

        private static string ReplacePatterns(string value, Dictionary<string, string> patterns)
        {
            string updatedValue = value;
            foreach (KeyValuePair<string, string> kvp in patterns)
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
