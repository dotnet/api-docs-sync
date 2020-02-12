using DocsPortingTool.Docs;
using Shared;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace DocsPortingTool
{
    public class XmlHelper
    {
        #region Private members

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
            { "</para>", "" }
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

        #endregion

        #region Read actions

        #region Public methods

        public static string GetAttributeValue(XElement parent, string name, bool errorCheck=false)
        {
            if (parent == null)
            {
                if (errorCheck)
                {
                    Log.Error("A null parent was passed when attempting to get attribute '{0}'", name);
                }
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

        public static XElement GetChildElement(XElement parent, string name, bool errorCheck=false)
        {
            XElement child = null;
            if (parent == null)
            {
                if (errorCheck)
                {
                    Log.Error("A null parent was passed when attempting to get element '{0}'", name);
                }
            }
            else
            {
                child = parent.Element(name);
                if (child == null)
                {
                    if (errorCheck)
                    {
                        Log.Error("Root '{0}' does not have a child named '{1}'", name, parent.Name);
                    }
                }
            }
            return child;
        }

        public static string GetChildElementValue(XElement parent, string name, bool errorCheck=false)
        {
            XElement child = GetChildElement(parent, name, errorCheck);
            if (child != null)
            {
                return GetRealValue(child);
            }
            return null;
        }

        public static string GetRealValue(XElement element, bool errorCheck=false)
        {
            string value = string.Empty;

            if (element == null)
            {
                if (errorCheck)
                {
                    Log.Error("A null parent was passed when attempting to retrieve the real value.");
                }
            }
            else
            {
                value = string.Join("", element.Nodes()).Trim().Replace("></see>", " />");
            }

            return value;
        }

        #endregion

        #endregion

        #region Write actions

        #region Public methods

        public static void SaveXml(XDocument xDoc, string filePath)
        {
            if (Configuration.Save)
            {
                // These settings prevent the addition of the <xml> element on the first line and will preserve indentation+endlines
                XmlWriterSettings xws = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true, Encoding = Encoding.GetEncoding("ISO-8859-1") };
                using (XmlWriter xw = XmlWriter.Create(filePath, xws))
                {
                    xDoc.Save(xw);
                    Log.Success(" [Saved]");
                }
            }
        }

        public static void FormatAsMarkdown(IDocsAPI api, XElement xeElement, string value)
        {
            // Empty value because SaveChildElement will add a child to the parent, not replace it
            xeElement.Value = string.Empty;

            XElement xeFormat = new XElement("format");

            string updatedValue = RemoveUndesiredEndlines(value);
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

            xeElement.Add(xeFormat);

            api.Changed = true;
        }

        public static void FormatAsNormalElement(XElement xeElement)
        {
            var attributes = xeElement.Attributes();
            string innerText = string.Join("", xeElement.Nodes());

            string updatedValue = RemoveUndesiredEndlines(innerText);
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

            xeElement.ReplaceNodes(parsedElement.Nodes());

            // Ensure attributes are preserved after replacing nodes
            xeElement.ReplaceAttributes(attributes);
        }

        #endregion

        #endregion

        #region Private methods

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

        #endregion
    }
}