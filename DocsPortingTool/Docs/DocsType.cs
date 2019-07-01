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
    public class DocsType
    {
        private XDocument XDoc = null;
        private XElement XERoot = null;

        public string FilePath { get; private set; }

        private string _name = string.Empty;
        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_name))
                {
                    _name = XmlHelper.GetAttributeValue(XERoot, "Name");
                }
                return _name;
            }
        }

        private string _fullName = string.Empty;
        public string FullName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_fullName))
                {
                    _fullName = XmlHelper.GetAttributeValue(XERoot, "FullName");
                }
                return _fullName;
            }
        }

        private string _namespace = string.Empty;
        public string Namespace
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_namespace))
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

        public string DocId
        {
            get
            {
                DocsTypeSignature dts = TypeSignatures.FirstOrDefault(x => x.Language == "DocId");
                if (dts == null)
                {
                    string message = $"DocId TypeSignature not found for FullName";
                    Log.Error($"DocId TypeSignature not found for FullName");
                    throw new Exception(message);
                }

                return dts.Value;
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

        private string _baseTypeName = string.Empty;
        public string BaseTypeName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_baseTypeName))
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
        public XElement Docs
        {
            get
            {
                return XmlHelper.GetChildElement(XERoot, "Docs");
            }
        }

        private string _summary = string.Empty;
        public string Summary
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_summary))
                {
                    _summary = XmlHelper.GetChildElementValue(Docs, "summary");
                }
                return _summary;
            }
            set
            {
                XmlHelper.SetChildElementValue(FilePath, Docs, "summary", value);
            }
        }

        public string Remarks
        {
            get
            {
                if (XERemarks != null)
                {
                    return XERemarks.Value;
                }
                return string.Empty;
            }
            set
            {
                XmlHelper.SaveRemark(FilePath, XDoc, XERemarks, value);
            }
        }

        private List<DocsMember> _members;
        public List<DocsMember> Members
        {
            get
            {
                if (_members == null)
                {
                    XElement members = XmlHelper.GetChildElement(XERoot, "Members");
                    if (members != null)
                    {
                        _members = members.Elements("Member").Select(x => new DocsMember(FilePath, XDoc, x)).ToList();
                    }
                    else
                    {
                        _members = new List<DocsMember>();
                    }
                }
                return _members;
            }
        }

        public DocsType(string filePath, XDocument xDoc, XElement xeRoot)
        {
            FilePath = filePath;
            XDoc = xDoc;
            XERoot = xeRoot;
        }

        public void SaveXml()
        {
            XmlHelper.SaveXml(FilePath, XDoc);
        }

        public override string ToString()
        {
            return FullName;
        }

        #region Private members

        private XElement XERemarks
        {
            get
            {
                XElement xeRemarks = null;
                if (Docs != null)
                {
                    xeRemarks = XmlHelper.GetChildElement(Docs, "remarks");
                    if (xeRemarks == null)
                    {
                        XmlHelper.SaveChildElement(FilePath, XDoc, Docs, new XElement("remarks", "To be added."), true);
                        xeRemarks = XmlHelper.GetChildElement(Docs, "remarks");
                    }
                }

                return xeRemarks;
            }
        }

        #endregion
    }
}
