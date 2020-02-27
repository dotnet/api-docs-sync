using System.Collections.Generic;
using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public abstract class DocsAPI : IDocsAPI
    {
        public abstract string Identifier { get; }
        public abstract bool Changed { get; set; }
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
            element = Docs.Element(name);

            if (element == null)
            {
                element = new XElement(name);
                XmlHelper.AddChildFormattedAsXml(Docs, element, Configuration.ToBeAdded);
                return false;
            }

            return true;
        }
    }
}
