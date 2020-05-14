using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    /// <summary>
    /// Represents the root xml element (unique) of a Docs xml file, called Type.
    /// </summary>
    public class DocsType : DocsAPI
    {
        public XDocument XDoc { get; set; }

        private readonly XElement XERoot;

        public override bool Changed { get; set; }

        private string? _name;
        public string Name
        {
            get
            {
                if (_name == null)
                {
                    _name = XmlHelper.GetAttributeValue(XERoot, "Name");
                }
                return _name;
            }
        }

        private string? _fullName;
        public string FullName
        {
            get
            {
                if (_fullName == null)
                {
                    _fullName = XmlHelper.GetAttributeValue(XERoot, "FullName");
                }
                return _fullName;
            }
        }

        private string? _namespace;
        public string Namespace
        {
            get
            {
                if (_namespace == null)
                {
                    int lastDotPosition = FullName.LastIndexOf('.');
                    _namespace = lastDotPosition < 0 ? FullName : FullName.Substring(0, lastDotPosition);
                }
                return _namespace;
            }
        }

        private List<DocsTypeSignature>? _typesSignatures;
        public List<DocsTypeSignature> TypeSignatures
        {
            get
            {
                if (_typesSignatures == null)
                {
                    _typesSignatures = XERoot.Elements("TypeSignature").Select(x => new DocsTypeSignature(x)).ToList();
                }
                return _typesSignatures;
            }
        }

        private string? _docId;
        public override string DocId
        {
            get
            {
                if (_docId == null)
                {
                    DocsTypeSignature dts = TypeSignatures.FirstOrDefault(x => x.Language == "DocId");
                    if (dts == null)
                    {
                        string message = $"DocId TypeSignature not found for FullName";
                        Log.Error($"DocId TypeSignature not found for FullName");
                        throw new Exception(message);
                    }
                    _docId = dts.Value;
                }
                return _docId;
            }
        }

        public XElement Base
        {
            get
            {
                return XERoot.Element("Base");
            }
        }

        private string? _baseTypeName;
        public string BaseTypeName
        {
            get
            {
                if (_baseTypeName == null)
                {
                    _baseTypeName = XmlHelper.GetChildElementValue(Base, "BaseTypeName");
                }
                return _baseTypeName;
            }
        }
        public XElement Interfaces
        {
            get
            {
                return XERoot.Element("Interfaces");
            }
        }

        private List<string>? _interfaceNames;
        public List<string> InterfaceNames
        {
            get
            {
                if (_interfaceNames == null)
                {
                    _interfaceNames = Interfaces.Elements("Interface").Select(x => XmlHelper.GetChildElementValue(x, "InterfaceName")).ToList();
                }
                return _interfaceNames;
            }
        }

        private List<DocsAttribute>? _attributes;
        public List<DocsAttribute> Attributes
        {
            get
            {
                if (_attributes == null)
                {
                    XElement e = XERoot.Element("Attributes");
                    _attributes = (e != null) ? e.Elements("Attribute").Select(x => new DocsAttribute(x)).ToList() : new List<DocsAttribute>();
                }
                return _attributes;
            }
        }

        private List<DocsParameter>? _parameters;
        public override List<DocsParameter> Parameters
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
        
        public override XElement Docs
        {
            get
            {
                return XERoot.Element("Docs");
            }
        }
        
        private List<DocsParam>? _params;
        public override List<DocsParam> Params
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

        public override string Summary
        {
            get
            {
                return GetNodesInPlainText("summary");
            }
            set
            {
                SaveFormattedAsXml("summary", value);
            }
        }

        public override string Remarks
        {
            get
            {
                return GetNodesInPlainText("remarks");
            }
            set
            {
                SaveFormattedAsMarkdown("remarks", value);
            }
        }

        public DocsType(string filePath, XDocument xDoc, XElement xeRoot)
        {
            FilePath = filePath;
            XDoc = xDoc;
            XERoot = xeRoot;
            _assemblyInfos.AddRange(XERoot.Elements("AssemblyInfo").Select(x => new DocsAssemblyInfo(x)));
        }

        public override string ToString()
        {
            return FullName;
        }
    }
}
