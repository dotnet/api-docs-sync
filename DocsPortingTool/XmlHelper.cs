using Shared;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace DocsPortingTool
{
    class XmlHelper
    {
        #region Private members

        private static readonly Dictionary<string, string> replaceableNonRemarkPatterns = new Dictionary<string, string> {
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

        private static readonly Dictionary<string, string> replaceableRemarkPatterns = new Dictionary<string, string> {
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
            { "<paramref name=\"", "<xref:" },
            { "<seealso cref=\"",  "<xref:" },
            { "<para>",            "" },
            { "</para>",           "" },
            { "\" />",             ">" },
            { "<![CDATA[",         "" },
            { "]]>",               "" }
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

        public static void SaveXml(string filePath, XDocument xDoc)
        {
            if (CLArgumentVerifier.Save)
            {
                // These settings prevent the addition of the <xml> element on the first line and will preserve indentation+endlines
                XmlWriterSettings xws = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true };
                using (XmlWriter xw = XmlWriter.Create(filePath, xws))
                {
                    Log.Info(xw.Settings.OutputMethod.ToString());
                    xDoc.Save(xw);
                    Log.Success("        [Saved]");
                }
            }
        }

        public static XElement SaveChildAsRemark(string filePath, XDocument xDoc, XElement xeParent, XElement xeChild, bool errorCheck = false)
        {
            if (VerifySaveChildParams(xDoc, xeParent, xeChild, errorCheck))
            {
                xeParent.Add(xeChild);
                SaveAsRemark(filePath, xDoc, xeChild, xeChild.Value);
            }
            return xeChild;
        }

        public static XElement SaveChildAsNonRemark(string filePath, XDocument xDoc, XElement xParent, XElement xeChild, bool errorCheck=false)
        {
            if (VerifySaveChildParams(xDoc, xParent, xeChild, errorCheck))
            {
                xParent.Add(xeChild);
                SaveAsNonRemark(filePath, xDoc, xeChild, xeChild.Value);
            }
            return xeChild;
        }

        public static void SaveAsRemark(string filePath, XDocument xDoc, XElement xeRemarks, string value)
        {
            // Empty the contents, because SaveChildElement will add a child to the parent, not replace it
            xeRemarks.Value = string.Empty;

            XElement xeFormat = new XElement("format");

            string updatedValue = value;

            foreach (KeyValuePair<string, string> kvp in replaceableRemarkPatterns)
            {
                if (updatedValue.Contains(kvp.Key))
                {
                    updatedValue = updatedValue.Replace(kvp.Key, kvp.Value);
                }
            }
            xeFormat.ReplaceAll(new XCData("\r\n" + updatedValue + "\r\n          "));

            // Attribute at the end, otherwise it would be replaced by ReplaceAll
            xeFormat.SetAttributeValue("type", "text/markdown");

            xeRemarks.Add(xeFormat);

            SaveXml(filePath, xDoc);
        }

        public static void SaveAsNonRemark(string filePath, XDocument xDoc, XElement xeElement, string value)
        {
            if (xeElement == null)
            {
                Log.Error("A null element was passed when attempting to set its value to '{0}'. File: {1}", value, filePath);
            }
            else
            {
                string updatedValue = value;
                foreach (KeyValuePair<string, string> kvp in replaceableNonRemarkPatterns)
                {
                    if (updatedValue.Contains(kvp.Key))
                    {
                        updatedValue = updatedValue.Replace(kvp.Key, kvp.Value);
                    }
                }

                // Workaround: <x> will ensure XElement does not complain about having an invalid xml object inside. Those tags will be removed in the next line.
                XElement parsedElement = null;
                try
                {
                    parsedElement = XElement.Parse("<x>" + updatedValue + "</x>");
                }
                catch (System.Xml.XmlException)
                {
                    parsedElement = XElement.Parse("<x>" + updatedValue.Replace("<", "&lt;").Replace(">", "&gt;") + "</x>");
                }
                xeElement.ReplaceNodes(parsedElement.Nodes());

                SaveXml(filePath, xDoc);
            }
        }

        #endregion

        #endregion

        #region Private methods

        private static bool VerifySaveChildParams(XDocument doc, XElement parent, XElement child, bool errorCheck = false)
        {
            if (doc == null)
            {
                if (errorCheck)
                {
                    Log.Error("A null XDocument was passed when attempting to save a new child element");
                }
                return false;
            }
            else if (parent == null)
            {
                if (errorCheck)
                {
                    Log.Error("A null XElement parent was passed when attempting to save a new child element");
                }
                return false;
            }
            else if (child == null)
            {
                if (errorCheck)
                {
                    Log.Error("A null XElement child was passed when attempting to save a new child element");
                }
                return false;
            }

            return true;
        }

        #endregion
    }
}