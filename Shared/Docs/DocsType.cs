using Shared;
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
        private XElement XERoot = null;

        private string _name = null;
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

        private string _fullName = null;
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

        private string _namespace = null;
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

        private List<DocsTypeSignature> _typesSignatures;
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

        private string _docId = null;
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

        private List<DocsAssemblyInfo> _assemblyInfos;
        public List<DocsAssemblyInfo> AssemblyInfos
        {
            get
            {
                if (_assemblyInfos == null)
                {
                    _assemblyInfos = XERoot.Elements("AssemblyInfo").Select(x => new DocsAssemblyInfo(x)).ToList();
                }
                return _assemblyInfos;
            }
        }
        public XElement Base
        {
            get
            {
                return XmlHelper.GetChildElement(XERoot, "Base");
            }
        }

        private string _baseTypeName = null;
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
                return XmlHelper.GetChildElement(XERoot, "Interfaces");
            }
        }

        private List<string> _interfaceNames;
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

        private List<DocsAttribute> _attributes;
        public List<DocsAttribute> Attributes
        {
            get
            {
                if (_attributes == null)
                {
                    XElement e = XmlHelper.GetChildElement(XERoot, "Attributes");
                    if (e != null)
                    {
                        _attributes = e.Elements("Attribute").Select(x => new DocsAttribute(x)).ToList();
                    }
                }
                return _attributes;
            }
        }

        private List<DocsParameter> _parameters;
        public override List<DocsParameter> Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    XElement xeParameters = XmlHelper.GetChildElement(XERoot, "Parameters");
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
                return XmlHelper.GetChildElement(XERoot, "Docs");
            }
        }
        
        private List<DocsParam> _params;
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

        private string _summary = null;
        public string Summary
        {
            get
            {
                if (_summary == null)
                {
                    _summary = XmlHelper.GetChildElementValue(Docs, "summary");
                }
                return _summary;
            }
            set
            {
                XElement xeSummary = XmlHelper.GetChildElement(Docs, "summary");
                if (xeSummary == null)
                {
                    xeSummary = new XElement("summary", "To be added.");
                    AddChildAsNormalElement(Docs, xeSummary, true);
                }
                else
                {
                    FormatAsNormalElement(xeSummary);
                }
            }
        }

        public string Remarks
        {
            get
            {
                return XERemarks.Value;
            }
            set
            {
                XmlHelper.FormatAsMarkdown(this, XERemarks, value);
            }
        }

        public DocsType(string filePath, XDocument xDoc, XElement xeRoot)
        {
            FilePath = filePath;
            XDoc = xDoc;
            XERoot = xeRoot;
        }

        public override string ToString()
        {
            return FullName;
        }

        #region Private members

        private XElement _xeRemarks = null;
        private XElement XERemarks
        {
            get
            {
                _xeRemarks = null;
                if (Docs != null)
                {
                    _xeRemarks = XmlHelper.GetChildElement(Docs, "remarks");
                    if (_xeRemarks == null)
                    {
                        _xeRemarks = new XElement("remarks", "To be added.");
                        AddChildAsNormalElement(Docs, _xeRemarks, true);
                    }
                }

                return _xeRemarks;
            }
        }

        #endregion
    }
}
