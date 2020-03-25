using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public abstract class DocsAPI : IDocsAPI
    {
        public abstract bool Changed { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public abstract string DocId { get; }
        public abstract XElement Docs { get; }
        public abstract List<DocsParameter> Parameters { get; }
        public abstract List<DocsParam> Params { get; }
        public abstract string Summary { get; set; }
        public abstract string Remarks { get; set; }

        protected readonly List<DocsAssemblyInfo> _assemblyInfos = new List<DocsAssemblyInfo>();
        public List<DocsAssemblyInfo> AssemblyInfos { get { return _assemblyInfos; } }

        private string? _docIdEscaped = null;
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

        public string Prefix
        {
            get
            {
                if (this is DocsMember)
                {
                    return "MEMBER";
                }
                if (this is DocsType)
                {
                    return "TYPE";
                }
                throw new ArgumentException("Unrecognized IDocsAPI object");
            }
        }

        protected string GetNodesInPlainText(string name)
        {
            if (TryGetElement(name, out XElement element))
            {
                if (name == "remarks")
                {
                    XElement formatElement = element.Element("format");
                    if (formatElement != null)
                    {
                        element = formatElement;
                    }
                }

                return XmlHelper.GetNodesInPlainText(element);
            }
            return string.Empty;
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
