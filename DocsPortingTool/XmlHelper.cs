using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace DocsPortingTool
{
    public class XmlHelper
    {
        private static readonly Dictionary<string, string> _replaceableNormalElementPatterns = new Dictionary<string, string> {
            { "<c>null</c>",                "<see langword=\"null\" />"},
            { "<c>true</c>",                "<see langword=\"true\" />"},
            { "<c>false</c>",               "<see langword=\"false\" />"},
            { " null ", " <see langword=\"null\" /> " },
            { " true ", " <see langword=\"true\" /> " },
            { " false ", " <see langword=\"false\" /> " },
            { " null,", " <see langword=\"null\" />," },
            { " true,", " <see langword=\"true\" />," },
            { " false,", " <see langword=\"false\" />," },
            { " null.", " <see langword=\"null\" />." },
            { " true.", " <see langword=\"true\" />." },
            { " false.", " <see langword=\"false\" />." },
            { "null ", "<see langword=\"null\" /> " },
            { "true ", "<see langword=\"true\" /> " },
            { "false ", "<see langword=\"false\" /> " },
            { "Null ", "<see langword=\"null\" /> " },
            { "True ", "<see langword=\"true\" /> " },
            { "False ", "<see langword=\"false\" /> " },
            { "<c>",     "" },
            { "</c>",    "" },
            { "<para>",  "" },
            { "</para>", "" },
            { "></see>", " />" }
        };

        private static readonly Dictionary<string, string> _replaceableMarkdownPatterns = new Dictionary<string, string> {
            { "<see langword=\"null\"/>",   "`null`" },
            { "<see langword=\"null\" />",  "`null`" },
            { "<see langword=\"true\"/>",   "`true`" },
            { "<see langword=\"true\" />",  "`true`" },
            { "<see langword=\"false\"/>",  "`false`" },
            { "<see langword=\"false\" />", "`false`" },
            { "<c>null</c>",                "`null`"},
            { "<c>true</c>",                "`true`"},
            { "<c>false</c>",               "`false`"},
            { " null ",            " `null` " },
            { "'null'",            "`null`" },
            { " null.",            " `null`." },
            { " null,",            " `null`," },
            { " false ",           " `false` " },
            { "'false'",           "`false`" },
            { " false.",           " `false`." },
            { " false,",           " `false`," },
            { " true ",            " `true` " },
            { "'true'",            "`true`" },
            { " true.",            " `true`." },
            { " true,",            " `true`," },
            { "null ", "`null` " },
            { "true ", "`true` " },
            { "false ", "`false` " },
            { "Null ", "`null` " },
            { "True ", "`true` " },
            { "False ", "`false` " },
            { "<note type=\"inheritinfo\">", ""},
            { "</note>",           "" },
            { "<see cref=\"T:",    "<xref:" },
            { "<see cref=\"F:",    "<xref:" },
            { "<see cref=\"M:",    "<xref:" },
            { "<see cref=\"P:",    "<xref:" },
            { "<see cref=\"",      "<xref:" },
            { "<para>",            "" },
            { "</para>",           "" },
            { "\" />",             ">" },
            { "<![CDATA[",         "" },
            { "]]>",               "" }
        };

        private static readonly Dictionary<string, string> _replaceableMarkdownRegexPatterns = new Dictionary<string, string> {
            { @"\<paramref name\=""(?'paramrefContents'[a-zA-Z0-9_\-]+)""[ ]*\/\>",  @"`${paramrefContents}`" },
            { @"\<seealso cref\=""(?'seealsoContents'.+)""[ ]*\/\>",      @"seealsoContents" },
        };

        public static string GetAttributeValue(XElement parent, string name)
        {
            if (parent == null)
            {
                Log.Error("A null parent was passed when attempting to get attribute '{0}'", name);
                throw new ArgumentNullException(nameof(parent));
            }
            else
            {
                XAttribute attr = parent.Attribute(name);
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
            XElement child = parent.Element(childName);

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
                Log.Error("A null element was passed when attempting to retrieve the nodes in plain text.");
                throw new ArgumentNullException(nameof(element));
            }
            return string.Join("", element.Nodes()).Trim();
        }

        public static void SaveFormattedAsMarkdown(XElement element, string newValue)
        {
            if (element == null)
            {
                Log.Error("A null element was passed when attempting to save formatted as markdown");
                throw new ArgumentNullException(nameof(element));
            }

            // Empty value because SaveChildElement will add a child to the parent, not replace it
            element.Value = string.Empty;

            XElement xeFormat = new XElement("format");

            string updatedValue = RemoveUndesiredEndlines(newValue);
            updatedValue = SubstituteRemarksRegexPatterns(updatedValue);
            updatedValue = ReplaceMarkdownPatterns(updatedValue);

            string remarksTitle = string.Empty;
            if (!updatedValue.Contains("## Remarks"))
            {
                remarksTitle = "## Remarks\r\n\r\n";
            }

            xeFormat.ReplaceAll(new XCData("\r\n\r\n" + remarksTitle + updatedValue + "\r\n\r\n          "));

            // Attribute at the end, otherwise it would be replaced by ReplaceAll
            xeFormat.SetAttributeValue("type", "text/markdown");

            element.Add(xeFormat);
        }

        public static void AddChildFormattedAsMarkdown(XElement parent, XElement child, string childValue)
        {
            if (parent == null)
            {
                Log.Error("A null parent was passed when attempting to add child formatted as markdown");
                throw new ArgumentNullException(nameof(parent));
            }

            if (child == null)
            {
                Log.Error("A null child was passed when attempting to add child formatted as markdown");
                throw new ArgumentNullException(nameof(child));
            }

            SaveFormattedAsMarkdown(child, childValue);
            parent.Add(child);
        }

        public static void SaveFormattedAsXml(XElement element, string newValue)
        {
            if (element == null)
            {
                Log.Error("A null element was passed when attempting to save formatted as xml");
                throw new ArgumentNullException(nameof(element));
            }

            element.Value = string.Empty;

            var attributes = element.Attributes();

            string updatedValue = RemoveUndesiredEndlines(newValue);
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

        public static void AppendFormattedAsXml(XElement element, string valueToAppend)
        {
            if (element == null)
            {
                Log.Error("A null element was passed when attempting to append formatted as xml");
                throw new ArgumentNullException(nameof(element));
            }

            SaveFormattedAsXml(element, GetNodesInPlainText(element) + valueToAppend);
        }

        public static void AddChildFormattedAsXml(XElement parent, XElement child, string childValue)
        {
            if (parent == null)
            {
                Log.Error("A null parent was passed when attempting to add child formatted as xml");
                throw new ArgumentNullException(nameof(parent));
            }

            if (child == null)
            {
                Log.Error("A null child was passed when attempting to add child formatted as xml");
                throw new ArgumentNullException(nameof(child));
            }

            SaveFormattedAsXml(child, childValue);
            parent.Add(child);
        }

        private static string RemoveUndesiredEndlines(string value)
        {
            Regex regex = new Regex(@"((?'undesiredEndlinePrefix'[^\.\:])[\r\n]+[ \t]*)");
            string newValue = value;
            if (regex.IsMatch(value))
            {
                newValue = regex.Replace(value, @"${undesiredEndlinePrefix} ");
            }
            return newValue.Trim();
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