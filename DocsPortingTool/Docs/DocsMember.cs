using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public class DocsMember : DocsAPI
    {
        private readonly XElement XEMember = null;
        public DocsType ParentType { get; private set; }

        public override bool Changed
        {
            get => ParentType.Changed;
            set
            {
                if (value == true)
                    ParentType.Changed = true;
            }
        }

        private string _memberName = null;
        public string MemberName
        {
            get
            {
                if (_memberName == null)
                {
                    _memberName = XmlHelper.GetAttributeValue(XEMember, "MemberName");
                }
                return _memberName;
            }
        }

        private List<DocsMemberSignature> _memberSignatures;
        public List<DocsMemberSignature> MemberSignatures
        {
            get
            {
                if (_memberSignatures == null)
                {
                    _memberSignatures = XEMember.Elements("MemberSignature").Select(x => new DocsMemberSignature(x)).ToList();
                }
                return _memberSignatures;
            }
        }

        private string _docId = null;
        public override string DocId
        {
            get
            {
                if (_docId == null)
                {
                    _docId = string.Empty;
                    DocsMemberSignature ms = MemberSignatures.FirstOrDefault(x => x.Language == "DocId");
                    if (ms == null)
                    {
                        string message = string.Format("Could not find a DocId MemberSignature for '{0}'", MemberName);
                        Log.Error(message);
                        throw new NullReferenceException(message);
                    }
                    else
                    {
                        _docId = ms.Value;
                    }
                }
                return _docId;
            }
        }

        public string MemberType
        {
            get
            {
                return XmlHelper.GetChildElementValue(XEMember, "MemberType");
            }
        }
        public string ImplementsInterfaceMember
        {
            get
            {
                XElement xeImplements = XEMember.Element("Implements");
                if (xeImplements != null)
                {
                    return XmlHelper.GetChildElementValue(xeImplements, "InterfaceMember");
                }
                return string.Empty;
            }
        }

        public string ReturnType
        {
            get
            {
                XElement xeReturnValue = XEMember.Element("ReturnValue");
                if (xeReturnValue != null)
                {
                    return XmlHelper.GetChildElementValue(xeReturnValue, "ReturnType");
                }
                return string.Empty;
            }
        }
        private List<DocsParameter> _parameters;
        public override List<DocsParameter> Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    XElement xeParameters = XEMember.Element("Parameters");
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
        /// These are the TypeParameter elements found inside the TypeParameters section.
        /// </summary>
        private List<DocsTypeParameter> _typeParameters;
        public List<DocsTypeParameter> TypeParameters
        {
            get
            {
                if (_typeParameters == null)
                {
                    XElement xeTypeParameters = XEMember.Element("TypeParameters");
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
        /// <summary>
        /// These are the typeparam elements found inside the Docs section.
        /// </summary>
        private List<DocsTypeParam> _typeParams;
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
        public override XElement Docs
        {
            get
            {
                return XEMember.Element("Docs");
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
        public string Returns
        {
            get
            {
                return (ReturnType != "System.Void") ? GetNodesInPlainText("returns") : null;
            }
            set
            {
                SaveFormattedAsXml("returns", value);
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
        public string Value
        {
            get
            {
                return (MemberType == "Property") ? GetNodesInPlainText("value") : null;
            }
            set
            {
                SaveFormattedAsXml("value", value);
            }
        }
        private List<string> _altMemberCref;
        public List<string> AltMemberCref
        {
            get
            {
                if (_altMemberCref == null)
                {
                    if (Docs != null)
                    {
                        _altMemberCref = Docs.Elements("altmember").Select(x => XmlHelper.GetAttributeValue(x, "cref")).ToList();
                    }
                    else
                    {
                        _altMemberCref = new List<string>();
                    }
                }
                return _altMemberCref;
            }
        }
        private List<DocsException> _exceptions;
        public List<DocsException> Exceptions
        {
            get
            {
                if (_exceptions == null)
                {
                    if (Docs != null)
                    {
                        _exceptions = Docs.Elements("exception").Select(x => new DocsException(this, x)).ToList();
                    }
                    else
                    {
                        _exceptions = new List<DocsException>();
                    }
                }
                return _exceptions;
            }
        }

        public DocsMember(string filePath, DocsType parentType, XElement xeMember)
        {
            FilePath = filePath;
            ParentType = parentType;
            XEMember = xeMember;
            _assemblyInfos.AddRange(XEMember.Elements("AssemblyInfo").Select(x => new DocsAssemblyInfo(x)));
        }

        public override string ToString()
        {
            return DocId;
        }

        public DocsException AddException(string cref, string value)
        {
            XElement exception = new XElement("exception");
            exception.SetAttributeValue("cref", cref);
            XmlHelper.AddChildFormattedAsXml(Docs, exception, value);
            Changed = true;
            return new DocsException(this, exception);
        }

        public DocsTypeParam AddTypeParam(string name, string value)
        {
            XElement typeParam = new XElement("typeparam");
            typeParam.SetAttributeValue("name", name);
            XmlHelper.AddChildFormattedAsXml(Docs, typeParam, value);
            Changed = true;
            return new DocsTypeParam(this, typeParam);
        }
    }
}