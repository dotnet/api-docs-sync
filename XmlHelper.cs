using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace DocsPortingTool
{
    class XmlHelper
    {
        #region Private members

        private static readonly Dictionary<string, string> replaceablePatterns = new Dictionary<string, string> {
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

        public static void SetElementValueOld(string filePath, XElement element, string value)
        {
            if (element == null)
            {
                Log.Error("A null element was passed when attempting to set its value to '{0}'. File: {1}", value, filePath);
            }
            else
            {
                element.Value = value;
            }
        }

        public static void SetElementValue(string filePath, XElement element, string value)
        {
            if (element == null)
            {
                Log.Error("A null element was passed when attempting to set its value to '{0}'. File: {1}", value, filePath);
            }
            else
            {
                EnsureParsedValue(element, value);
            }
        }

        public static void SetChildElementValue(string filePath, XElement parent, string name, string value, bool errorCheck=false)
        {
            if (parent == null)
            {
                if (errorCheck)
                {
                    Log.Error("A null XElement parent was passed when attempting to save a child element value: {0}->{1}", name, value);
                }
            }
            else
            {
                XElement child = GetChildElement(parent, name, errorCheck);
                SetElementValue(filePath, child, value);
            }
        }

        public static XElement SaveChildElement(string filePath, XDocument doc, XElement parent, XElement child, bool errorCheck=false)
        {
            if (doc == null)
            {
                if (errorCheck)
                {
                    Log.Error("A null XDocument was passed when attempting to save a new child element");
                }
            }
            else if (parent == null)
            {
                if (errorCheck)
                {
                    Log.Error("A null XElement parent was passed when attempting to save a new child element");
                }
            }
            else if (child == null)
            {
                if (errorCheck)
                {
                    Log.Error("A null XElement child was passed when attempting to save a new child element");
                }
            }
            else
            {
                //EnsureParsedValue(child);
                parent.Add(child);
                SaveXml(filePath, doc);
            }
            return child;
        }

        public static XElement SaveChildElement(string filePath, XDocument doc, XElement parent, string name, string value, bool errorCheck=false)
        {
            XElement child = new XElement(name, value);
            return SaveChildElement(filePath, doc, parent, child, errorCheck);
        }

        public static void SaveXml(string filePath, XDocument doc)
        {
            if (CLArgumentVerifier.Save)
            {
                // These settings prevent the addition of the <xml> element on the first line and will preserve indentation+endlines
                XmlWriterSettings xws = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true };
                using (XmlWriter xw = XmlWriter.Create(filePath, xws))
                {
                    Log.Info(xw.Settings.OutputMethod.ToString());
                    doc.Save(xw);
                    Log.Success("        [Saved]");
                }
            }
        }

        public static string UpdatedRemark(string originalRemark)
        {
            string updatedRemark = originalRemark;

            foreach (KeyValuePair<string, string> kvp in replaceablePatterns)
            {
                if (updatedRemark.Contains(kvp.Key))
                {
                    updatedRemark = updatedRemark.Replace(kvp.Key, kvp.Value);
                }
            }

            return updatedRemark;
        }

        public static void SaveRemark(string filePath, XDocument xDoc, XElement xeRemarks, string value)
        {
            // Empty the contents, because SaveChildElement will add a child to the parent, not replace it
            xeRemarks.Value = string.Empty;

            XElement formatElement = new XElement("format");

            string updatedRemark = UpdatedRemark(value);
            formatElement.ReplaceAll(new XCData(updatedRemark));
            formatElement.SetAttributeValue("type", "text/markdown");

            SaveChildElement(filePath, xDoc, xeRemarks, formatElement, true);
        }

        private static void EnsureParsedValue(XElement element, string value)
        {
            // Workaround: <x> will ensure XElement does not complain about having an invalid xml object inside. Those tags will be removed in the next line.
            XElement parsedElement = XElement.Parse("<x>" + value + "</x>");
            element.ReplaceNodes(parsedElement.Nodes());
        }

        private static void EnsureParsedValue(XElement element)
        {
            EnsureParsedValue(element, element.Value);
        }

        #endregion

        #endregion
    }
}