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
        private string? _name;
        private string? _fullName;
        private string? _namespace;
        private string? _docId;
        private string? _baseTypeName;
        private List<string>? _interfaceNames;
        private List<DocsAttribute>? _attributes;
        private List<DocsTypeSignature>? _typesSignatures;

        public DocsType(string filePath, XDocument xDoc, XElement xeRoot)
            : base(xeRoot)
        {
            FilePath = filePath;
            XDoc = xDoc;
            AssemblyInfos.AddRange(XERoot.Elements("AssemblyInfo").Select(x => new DocsAssemblyInfo(x)));
        }

        public XDocument XDoc { get; set; }

        public override bool Changed { get; set; }

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

        public override string ToString()
        {
            return FullName;
        }
    }
}
