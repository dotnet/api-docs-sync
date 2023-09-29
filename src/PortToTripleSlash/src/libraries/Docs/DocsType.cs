// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace ApiDocsSync.PortToTripleSlash.Docs
{
    /// <summary>
    /// Represents the root xml element (unique) of a Docs xml file, called Type.
    /// </summary>
    internal class DocsType : DocsAPI
    {
        private string? _typeName;
        private string? _name;
        private string? _fullName;
        private string? _namespace;
        private string? _baseTypeName;
        private List<string>? _interfaceNames;
        private List<DocsAttribute>? _attributes;
        private List<DocsTypeSignature>? _typesSignatures;

        public DocsType(string filePath, XDocument xDoc, XElement xeRoot, Encoding encoding)
            : base(xeRoot)
        {
            FilePath = filePath;
            XDoc = xDoc;
            FileEncoding = encoding;
            AssemblyInfos.AddRange(XERoot.Elements("AssemblyInfo").Select(x => new DocsAssemblyInfo(x)));
        }

        private List<ResolvedLocation>? _symbolLocations;
        public List<ResolvedLocation> SymbolLocations => _symbolLocations ??= new();

        public XDocument XDoc { get; }

        public Encoding FileEncoding { get; }

        public string TypeName
        {
            get
            {
                if (_typeName == null)
                {
                    // DocId uses ` notation for generic types, but it uses . for nested types
                    // Name uses + for nested types, but it uses &lt;T&gt; for generic types
                    // We need ` notation for generic types and + notation for nested types
                    // Only filename gives us that format, but we have to prepend the namespace
                    if (DocId.Contains('`') || Name.Contains('+'))
                    {
                        _typeName = Namespace + "." + System.IO.Path.GetFileNameWithoutExtension(FilePath);
                    }
                    else
                    {
                        _typeName = FullName;
                    }
                }
                return _typeName;
            }
        }

        public string Name => _name ??= XmlHelper.GetAttributeValue(XERoot, "Name");

        public string FullName => _fullName ??= XmlHelper.GetAttributeValue(XERoot, "FullName");

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

        public List<DocsTypeSignature> TypeSignatures => _typesSignatures ??= XERoot.Elements("TypeSignature").Select(x => new DocsTypeSignature(x)).ToList();

        public XElement? Base => XERoot.Element("Base");

        public string BaseTypeName
        {
            get
            {
                if (Base == null)
                {
                    _baseTypeName = string.Empty;
                }
                else if (_baseTypeName == null)
                {
                    _baseTypeName = XmlHelper.GetChildElementValue(Base, "BaseTypeName");
                }
                return _baseTypeName;
            }
        }

        public XElement? Interfaces => XERoot.Element("Interfaces");

        public List<string> InterfaceNames
        {
            get
            {
                if (Interfaces == null)
                {
                    _interfaceNames = new();
                }
                else if (_interfaceNames == null)
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
                    XElement? e = XERoot.Element("Attributes");
                    if (e == null)
                    {
                        _attributes = new();
                    }
                    else
                    {
                        _attributes = (e != null) ? e.Elements("Attribute").Select(x => new DocsAttribute(x)).ToList() : new List<DocsAttribute>();
                    }
                }
                return _attributes;
            }
        }

        public override string Summary => GetNodesInPlainText("summary");

        public override string Value => string.Empty;

        /// <summary>
        /// Only available when the type is a delegate.
        /// </summary>
        public override string ReturnType
        {
            get
            {
                XElement? xeReturnValue = XERoot.Element("ReturnValue");
                if (xeReturnValue != null)
                {
                    return XmlHelper.GetChildElementValue(xeReturnValue, "ReturnType");
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Only available when the type is a delegate.
        /// </summary>
        public override string Returns => (ReturnType != "System.Void") ? GetNodesInPlainText("returns") : string.Empty;

        public override string Remarks => GetNodesInPlainText("remarks");

        public override List<DocsException> Exceptions { get; } = new();

        public override string ToString() => FullName;

        protected override string GetApiSignatureDocId()
        {
            DocsTypeSignature? dts = TypeSignatures.FirstOrDefault(x => x.Language == "DocId");
            return dts != null ? dts.Value : throw new FormatException($"DocId TypeSignature not found for {FullName}");
        }
    }
}
