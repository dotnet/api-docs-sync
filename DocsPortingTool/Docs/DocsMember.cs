using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public class DocsMember : DocsAPI
    {
        private string? _memberName;
        private List<DocsMemberSignature>? _memberSignatures;
        private string? _docId;
        private List<string>? _altMemberCref;
        private List<DocsException>? _exceptions;

        public DocsMember(string filePath, DocsType parentType, XElement xeMember)
            : base(xeMember)
        {
            FilePath = filePath;
            ParentType = parentType;
            AssemblyInfos.AddRange(XERoot.Elements("AssemblyInfo").Select(x => new DocsAssemblyInfo(x)));
        }

        public DocsType ParentType { get; private set; }

        public override bool Changed
        {
            get => ParentType.Changed;
            set => ParentType.Changed |= value;
        }

        public string MemberName
        {
            get
            {
                if (_memberName == null)
                {
                    _memberName = XmlHelper.GetAttributeValue(XERoot, "MemberName");
                }
                return _memberName;
            }
        }

        public List<DocsMemberSignature> MemberSignatures
        {
            get
            {
                if (_memberSignatures == null)
                {
                    _memberSignatures = XERoot.Elements("MemberSignature").Select(x => new DocsMemberSignature(x)).ToList();
                }
                return _memberSignatures;
            }
        }

        public override string DocId
        {
            get
            {
                if (_docId == null)
                {
                    _docId = string.Empty;
                    DocsMemberSignature? ms = MemberSignatures.FirstOrDefault(x => x.Language == "DocId");
                    if (ms == null)
                    {
                        string message = string.Format("Could not find a DocId MemberSignature for '{0}'", MemberName);
                        Log.Error(message);
                        throw new MissingMemberException(message);
                    }
                     _docId = ms.Value;
                }
                return _docId;
            }
        }

        public string MemberType
        {
            get
            {
                return XmlHelper.GetChildElementValue(XERoot, "MemberType");
            }
        }

        public string ImplementsInterfaceMember
        {
            get
            {
                XElement xeImplements = XERoot.Element("Implements");
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
                XElement xeReturnValue = XERoot.Element("ReturnValue");
                if (xeReturnValue != null)
                {
                    return XmlHelper.GetChildElementValue(xeReturnValue, "ReturnType");
                }
                return string.Empty;
            }
        }

        public string Returns
        {
            get
            {
                return (ReturnType != "System.Void") ? GetNodesInPlainText("returns") : string.Empty;
            }
            set
            {
                if (ReturnType != "System.Void")
                {
                    SaveFormattedAsXml("returns", value, addIfMissing: false);
                }
                else
                {
                    Log.Warning($"Attempted to save a returns item for a method that returns System.Void: {DocIdEscaped}");
                }
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
                SaveFormattedAsXml("summary", value, addIfMissing: true);
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
                SaveFormattedAsMarkdown("remarks", value, addIfMissing: !Analyzer.IsEmpty(value), isMember: true);
            }
        }

        public string Value
        {
            get
            {
                return (MemberType == "Property") ? GetNodesInPlainText("value") : string.Empty;
            }
            set
            {
                if (MemberType == "Property")
                {
                    SaveFormattedAsXml("value", value, addIfMissing: true);
                }
                else
                {
                    Log.Warning($"Attempted to save a value element for an API that is not a property: {DocIdEscaped}");
                }
            }
        }

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
    }
}