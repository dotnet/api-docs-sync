using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public class DocsMember
    {
        private XDocument XDoc = null;
        private XElement XEMember = null;

        public string FilePath { get; private set; }
        public string MemberName
        {
            get
            {
                return XmlHelper.GetAttributeValue(XEMember, "MemberName");
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
        public string DocId
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
                XElement xeImplements = XmlHelper.GetChildElement(XEMember, "Implements");
                if (xeImplements != null)
                {
                    XmlHelper.GetChildElementValue(xeImplements, "InterfaceMember");
                }
                return string.Empty;
            }
        }

        private List<DocsAssemblyInfo> _assemblyInfos;
        public List<DocsAssemblyInfo> AssemblyInfos
        {
            get
            {
                if (_assemblyInfos == null)
                {
                    _assemblyInfos = XEMember.Elements("AssemblyInfo").Select(x => new DocsAssemblyInfo(x)).ToList();
                }
                return _assemblyInfos;
            }
        }
        public string ReturnType
        {
            get
            {
                XElement xeReturnValue = XmlHelper.GetChildElement(XEMember, "ReturnValue");
                if (xeReturnValue != null)
                {
                    return XmlHelper.GetChildElementValue(xeReturnValue, "ReturnType");
                }
                return string.Empty;
            }
        }
        private List<DocsParameter> _parameters;
        public List<DocsParameter> Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    XElement xeParameters = XmlHelper.GetChildElement(XEMember, "Parameters");
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
                    XElement xeTypeParameters = XmlHelper.GetChildElement(XEMember, "TypeParameters");
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
                        _typeParams = Docs.Elements("typeparam").Select(x => new DocsTypeParam(FilePath, XDoc, x)).ToList();
                    }
                    else
                    {
                        _typeParams = new List<DocsTypeParam>();
                    }
                }
                return _typeParams;
            }
        }
        public XElement Docs
        {
            get
            {
                return XmlHelper.GetChildElement(XEMember, "Docs");
            }
        }
        private List<DocsParam> _params;
        public List<DocsParam> Params
        {
            get
            {
                if (_params == null)
                {
                    if (Docs != null)
                    {
                        _params = Docs.Elements("param").Select(x => new DocsParam(FilePath, XDoc, x)).ToList();
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
                if (Docs != null)
                {
                    XElement xeReturns = XmlHelper.GetChildElement(Docs, "returns");
                    if (xeReturns != null)
                    {
                        return xeReturns.Value;
                    }
                }
                return null;
            }
            set
            {
                XmlHelper.SetChildElementValue(FilePath, Docs, "returns", value);
            }
        }
        public string Summary
        {
            get
            {
                return XmlHelper.GetChildElementValue(Docs, "summary");
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
                return XERemarks.Value;
            }
            set
            {
                XmlHelper.SaveRemark(FilePath, XDoc, XERemarks, value);
            }
        }
        public string Value
        {
            get
            {
                return XmlHelper.GetChildElementValue(Docs, "value");
            }
            set
            {
                XmlHelper.SetChildElementValue(FilePath, Docs, "value", value);
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
                        _exceptions = Docs.Elements("exception").Select(x => new DocsException(FilePath, XDoc, x)).ToList();
                    }
                    else
                    {
                        _exceptions = new List<DocsException>();
                    }
                }
                return _exceptions;
            }
        }

        public DocsMember(string filePath, XDocument xDoc, XElement xeMember)
        {
            FilePath = filePath;
            XDoc = xDoc;
            XEMember = xeMember;
        }

        public override string ToString()
        {
            return DocId;
        }

        public DocsException SaveException(XElement xeCoreFXException)
        {
            XElement xeDocsException = XmlHelper.SaveChildElement(FilePath, XDoc, Docs, xeCoreFXException);
            DocsException docsException = new DocsException(FilePath, XDoc, xeDocsException);
            return docsException;
        }

        public DocsTypeParam SaveTypeParam(XElement xeCoreFXTypeParam)
        {
            XElement xeDocsTypeParam = XmlHelper.SaveChildElement(FilePath, XDoc, Docs, xeCoreFXTypeParam);
            DocsTypeParam docsTypeParam = new DocsTypeParam(FilePath, XDoc, xeDocsTypeParam);
            return docsTypeParam;
        }

        public DocsParam SaveParam(XElement xeCoreFXParam)
        {
            XElement xeDocsParam = XmlHelper.SaveChildElement(FilePath, XDoc, Docs, xeCoreFXParam);
            DocsParam docsParam = new DocsParam(FilePath, XDoc, xeDocsParam);
            return docsParam;
        }

        #region Private methods

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