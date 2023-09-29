// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ApiDocsSync.PortToTripleSlash.Docs
{
    internal class DocsMember : DocsAPI
    {
        private string? _memberName;
        private List<DocsMemberSignature>? _memberSignatures;
        private List<DocsException>? _exceptions;

        public DocsMember(string filePath, DocsType parentType, XElement xeMember)
            : base(xeMember)
        {
            FilePath = filePath;
            ParentType = parentType;
            AssemblyInfos.AddRange(XERoot.Elements("AssemblyInfo").Select(x => new DocsAssemblyInfo(x)));
        }

        public DocsType ParentType { get; private set; }

        public string MemberName => _memberName ??= XmlHelper.GetAttributeValue(XERoot, "MemberName");

        public List<DocsMemberSignature> MemberSignatures => _memberSignatures ??= XERoot.Elements("MemberSignature").Select(x => new DocsMemberSignature(x)).ToList();

        public string MemberType => XmlHelper.GetChildElementValue(XERoot, "MemberType");

        public string ImplementsInterfaceMember
        {
            get
            {
                XElement? xeImplements = XERoot.Element("Implements");
                return (xeImplements != null) ? XmlHelper.GetChildElementValue(xeImplements, "InterfaceMember") : string.Empty;
            }
        }

        public override string ReturnType
        {
            get
            {
                XElement? xeReturnValue = XERoot.Element("ReturnValue");
                return xeReturnValue != null ? XmlHelper.GetChildElementValue(xeReturnValue, "ReturnType") : string.Empty;
            }
        }

        public override string Returns => (ReturnType != "System.Void") ? GetNodesInPlainText("returns") : string.Empty;

        public override string Summary => GetNodesInPlainText("summary");

        public override string Remarks => GetNodesInPlainText("remarks");

        public override string Value => (MemberType == "Property") ? GetNodesInPlainText("value") : string.Empty;

        public override List<DocsException> Exceptions
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

        public override string ToString() => DocId;

        protected override string GetApiSignatureDocId()
        {
            DocsMemberSignature? dts = MemberSignatures.FirstOrDefault(x => x.Language == "DocId");
            return dts != null ? dts.Value : throw new FormatException($"DocId TypeSignature not found for {MemberName}");
        }
    }
}
