using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public abstract class DocsAPI : IDocsAPI, IDisposable
    {
        public abstract string Identifier { get; }
        public XDocument XDoc { get; set; } = null;
        public bool Changed { get; set; } = false;
        public string FilePath { get; set; } = string.Empty;
        public abstract string DocId { get; }
        public abstract XElement Docs { get; }
        public abstract List<DocsParameter> Parameters { get; }
        public abstract List<DocsParam> Params { get; }
        public abstract string Summary { get; set; }
        public abstract string Remarks { get; set; }

        private string _docIdEscaped = null;
        public string DocIdEscaped
        {
            get
            {
                if (_docIdEscaped == null)
                {
                    _docIdEscaped = DocId.Replace("<", "{").Replace(">", "}").Replace("&lt;", "{").Replace("&gt;", "}");
                }
                return _docIdEscaped;
            }
        }

        public void Dispose()
        {
            Log.Warning(false, $"Saving file: {FilePath}");

            if (Configuration.Save)
            {
                // These settings prevent the addition of the <xml> element on the first line and will preserve indentation+endlines
                XmlWriterSettings xws = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true, Encoding = Encoding.GetEncoding("ISO-8859-1") };
                using (XmlWriter xw = XmlWriter.Create(FilePath, xws))
                {
                    XDoc.Save(xw);
                }

                // Workaround to delete the annoying endline added by XmlWriter.Save
                string fileData = File.ReadAllText(FilePath);
                if (!fileData.EndsWith(Environment.NewLine))
                {
                    File.WriteAllText(FilePath, fileData + Environment.NewLine);
                }

                Log.Success(" [Saved]");
            }
        }
        
        public DocsParam SaveParam(XElement xeTripleSlashParam)
        {
            XElement xeDocsParam = new XElement(xeTripleSlashParam.Name);
            xeDocsParam.ReplaceAttributes(xeTripleSlashParam.Attributes());
            XmlHelper.SaveFormattedAsXml(xeDocsParam, xeTripleSlashParam.Value);
            DocsParam docsParam = new DocsParam(this, xeDocsParam);
            Changed = true;
            return docsParam;
        }

        protected string GetNodesInPlainText(string name)
        {
            TryGetElement(name, out XElement element);
            return XmlHelper.GetNodesInPlainText(element);
        }

        protected void SaveFormattedAsXml(string name, string value)
        {
            if (TryGetElement(name, out XElement element))
            {
                XmlHelper.SaveFormattedAsXml(element, value);
                Changed = true;
            }
        }

        protected void SaveFormattedAsMarkdown(string name, string value)
        {
            if (TryGetElement(name, out XElement element))
            {
                XmlHelper.SaveFormattedAsMarkdown(element, value);
                Changed = true;
            }
        }

        // Returns true if the element existed, false if it had to be created with "To be added." as value.
        private bool TryGetElement(string name, out XElement element)
        {
            element = XmlHelper.GetChildElement(Docs, name);

            if (element == null)
            {
                element = new XElement(name);
                XmlHelper.AddChildFormattedAsXml(Docs, element, "To be added.");
                return false;
            }

            return true;
        }
    }
}
