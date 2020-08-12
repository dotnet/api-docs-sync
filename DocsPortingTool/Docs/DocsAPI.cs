using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public abstract class DocsAPI : IDocsAPI
    {
        private string? _docIdEscaped = null;
        private List<DocsParam>? _params;
        private List<DocsParameter>? _parameters;
        private List<DocsTypeParameter>? _typeParameters;
        private List<DocsTypeParam>? _typeParams;
        private List<DocsAssemblyInfo>? _assemblyInfos;

        protected readonly XElement XERoot;

        protected DocsAPI(XElement xeRoot) => XERoot = xeRoot;

        public abstract bool Changed { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public abstract string DocId { get; }

        /// <summary>
        /// The Parameter elements found inside the Parameters section.
        /// </summary>
        public List<DocsParameter> Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    XElement xeParameters = XERoot.Element("Parameters");
                    if (xeParameters != null)
                    {
                        _parameters = xeParameters.Elements("Parameter").Select(x => new DocsParameter(x)).ToList();
                    }
                    else
                    {
                        _parameters = new List<DocsParameter>();
                    }
                }
                return _parameters;
            }
        }

        /// <summary>
        /// The TypeParameter elements found inside the TypeParameters section.
        /// </summary>
        public List<DocsTypeParameter> TypeParameters
        {
            get
            {
                if (_typeParameters == null)
                {
                    XElement xeTypeParameters = XERoot.Element("TypeParameters");
                    if (xeTypeParameters != null)
                    {
                        _typeParameters = xeTypeParameters.Elements("TypeParameter").Select(x => new DocsTypeParameter(x)).ToList();
                    }
                    else
                    {
                        _typeParameters = new List<DocsTypeParameter>();
                    }
                }
                return _typeParameters;
            }
        }

        public XElement Docs
        {
            get
            {
                return XERoot.Element("Docs");
            }
        }

        /// <summary>
        ///  The param elements found inside the Docs section.
        /// </summary>
        public List<DocsParam> Params
        {
            get
            {
                if (_params == null)
                {
                    if (Docs != null)
                    {
                        _params = Docs.Elements("param").Select(x => new DocsParam(this, x)).ToList();
                    }
                    else
                    {
                        _params = new List<DocsParam>();
                    }
                }
                return _params;
            }
        }

        /// <summary>
        /// The typeparam elements found inside the Docs section.
        /// </summary>
        public List<DocsTypeParam> TypeParams
        {
            get
            {
                if (_typeParams == null)
                {
                    if (Docs != null)
                    {
                        _typeParams = Docs.Elements("typeparam").Select(x => new DocsTypeParam(this, x)).ToList();
                    }
                    else
                    {
                        _typeParams = new List<DocsTypeParam>();
                    }
                }
                return _typeParams;
            }
        }

        public abstract string Summary { get; set; }

        public abstract string Remarks { get; set; }

        public List<DocsAssemblyInfo> AssemblyInfos
        {
            get
            {
                if (_assemblyInfos == null)
                {
                    _assemblyInfos = new List<DocsAssemblyInfo>();
                }
                return _assemblyInfos;
            }
        }

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

        public APIKind Kind
        {
            get
            {
                return this switch
                {
                    DocsMember _ => APIKind.Member,
                    DocsType _ => APIKind.Type,
                    _ => throw new ArgumentException("Unrecognized IDocsAPI object")
                };
            }
        }

        public DocsTypeParam AddTypeParam(string name, string value)
        {
            XElement typeParam = new XElement("typeparam");
            typeParam.SetAttributeValue("name", name);
            XmlHelper.AddChildFormattedAsXml(Docs, typeParam, value);
            Changed = true;
            return new DocsTypeParam(this, typeParam);
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
