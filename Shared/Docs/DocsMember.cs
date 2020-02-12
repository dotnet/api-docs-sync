using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public class DocsMember : DocsAPI
    {
        private XElement XEMember = null;

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
        public override List<DocsParameter> Parameters
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
                return XmlHelper.GetChildElement(XEMember, "Docs");
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
                XElement xeReturns = XmlHelper.GetChildElement(Docs, "returns");
                if (xeReturns == null)
                {
                    xeReturns = new XElement("returns", "To be added.");
                    AddChildAsNormalElement(Docs, xeReturns, true);
                }
                else
                {
                    FormatAsNormalElement(xeReturns);
                }
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
        public string Value
        {
            get
            {
                return XmlHelper.GetChildElementValue(Docs, "value");
            }
            set
            {
                XElement xeValue = XmlHelper.GetChildElement(Docs, "value");
                if (xeValue == null)
                {
                    FormatAsNormalElement(xeValue);
                }
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

        public DocsException AddException(XElement xeException)
        {
            AddChildAsNormalElement(Docs, xeException, true);
            DocsException docsException = new DocsException(this, xeException);
            return docsException;
        }

        public DocsTypeParam AddTypeParam(XElement xeTripleSlashParam)
        {
            XElement xeDocsTypeParam = new XElement(xeTripleSlashParam);
            AddChildAsNormalElement(Docs, xeDocsTypeParam, true);
            DocsTypeParam docsTypeParam = new DocsTypeParam(this, xeDocsTypeParam);
            return docsTypeParam;
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
                        xeRemarks = new XElement("remarks", "To be added.");
                        AddChildAsNormalElement(Docs, xeRemarks, true);
                    }
                }

                return xeRemarks;
            }
        }

        #endregion
    }
}