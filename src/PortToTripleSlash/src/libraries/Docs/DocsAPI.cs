// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Linq;

namespace ApiDocsSync.PortToTripleSlash.Docs
{
    internal abstract class DocsAPI : IDocsAPI
    {
        private string? _docId;
        private string? _docIdUnprefixed;
        private List<DocsParam>? _params;
        private List<DocsParameter>? _parameters;
        private List<DocsTypeParameter>? _typeParameters;
        private List<DocsTypeParam>? _typeParams;
        private List<DocsAssemblyInfo>? _assemblyInfos;
        private List<string>? _seeAlsoCrefs;
        private List<string>? _altMemberCrefs;
        private List<DocsRelated>? _relateds;

        protected readonly XElement XERoot;

        protected DocsAPI(XElement xeRoot) => XERoot = xeRoot;

        public bool IsUndocumented =>
            Summary.IsDocsEmpty() ||
            Returns.IsDocsEmpty() ||
            Params.Any(p => p.Value.IsDocsEmpty()) ||
            TypeParams.Any(tp => tp.Value.IsDocsEmpty());

        public string FilePath { get; set; } = string.Empty;

        public string DocId => _docId ??= GetApiSignatureDocId();

        public string DocIdUnprefixed => _docIdUnprefixed ??= DocId[2..];

        /// <summary>
        /// The Parameter elements found inside the Parameters section.
        /// </summary>
        public List<DocsParameter> Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    XElement? xeParameters = XERoot.Element("Parameters");
                    _parameters = xeParameters == null ? (List<DocsParameter>)new() : xeParameters.Elements("Parameter").Select(x => new DocsParameter(x)).ToList();
                }
                return _parameters;
            }
        }

        /// <summary>
        /// The TypeParameter elements found inside the TypeParameters section.
        /// </summary>
        public List<DocsTypeParameter> TypeParameters
        {
            get
            {
                if (_typeParameters == null)
                {
                    XElement? xeTypeParameters = XERoot.Element("TypeParameters");
                    _typeParameters = xeTypeParameters == null ? (List<DocsTypeParameter>)new() : xeTypeParameters.Elements("TypeParameter").Select(x => new DocsTypeParameter(x)).ToList();
                }
                return _typeParameters;
            }
        }

        public XElement Docs => XERoot.Element("Docs") ?? throw new NullReferenceException($"Docs section was null in {FilePath}");

        /// <summary>
        ///  The param elements found inside the Docs section.
        /// </summary>
        public List<DocsParam> Params => _params ??= Docs != null ? Docs.Elements("param").Select(x => new DocsParam(this, x)).ToList() : new List<DocsParam>();

        /// <summary>
        /// The typeparam elements found inside the Docs section.
        /// </summary>
        public List<DocsTypeParam> TypeParams => _typeParams ??= Docs != null ? Docs.Elements("typeparam").Select(x => new DocsTypeParam(this, x)).ToList() : (List<DocsTypeParam>)new();

        public List<string> SeeAlsoCrefs => _seeAlsoCrefs ??= Docs != null ? Docs.Elements("seealso").Select(x => XmlHelper.GetAttributeValue(x, "cref").DocIdEscaped()).ToList() : (List<string>)new();

        public List<string> AltMembers => _altMemberCrefs ??= Docs != null ? Docs.Elements("altmember").Select(x => XmlHelper.GetAttributeValue(x, "cref").DocIdEscaped()).ToList() : (List<string>)new();

        public List<DocsRelated> Relateds => _relateds ??= Docs != null ? Docs.Elements("related").Select(x => new DocsRelated(this, x)).ToList() : (List<DocsRelated>)new();

        public abstract string Summary { get; }

        public abstract string Value { get; }

        public abstract string ReturnType { get; }

        public abstract string Returns { get; }

        public abstract string Remarks { get; }

        public abstract List<DocsException> Exceptions { get; }

        public List<DocsAssemblyInfo> AssemblyInfos => _assemblyInfos ??= new List<DocsAssemblyInfo>();

        public APIKind Kind => this switch
        {
            DocsMember _ => APIKind.Member,
            DocsType _ => APIKind.Type,
            _ => throw new ArgumentException("Unrecognized IDocsAPI object")
        };

        // For Types, these elements are called TypeSignature.
        // For Members, these elements are called MemberSignature.
        protected abstract string GetApiSignatureDocId();

        protected string GetNodesInPlainText(string name) => TryGetElement(name, out XElement? element) ? XmlHelper.GetNodesInPlainText(name, element) : string.Empty;

        // Returns true if the element existed or had to be created with "To be added." as value. Returns false the element was not found and a new one was not created.
        private bool TryGetElement(string name, [NotNullWhen(returnValue: true)] out XElement? element)
        {
            element = null;

            if (Docs == null)
            {
                return false;
            }

            element = Docs.Element(name);

            return element != null;
        }
    }
}
