// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ApiDocsSync.PortToTripleSlash.Docs
{
    /// <summary>
    /// Each one of these TypeParameter objects islocated inside the TypeParameters section inside the Member.
    /// </summary>
    internal class DocsTypeParameter
    {
        private readonly XElement XETypeParameter;

        public string Name => XmlHelper.GetAttributeValue(XETypeParameter, "Name");

        private XElement? Constraints => XETypeParameter.Element("Constraints");

        private List<string>? _constraintsParameterAttributes;
        public List<string> ConstraintsParameterAttributes => _constraintsParameterAttributes ??= Constraints != null
                        ? Constraints.Elements("ParameterAttribute").Select(x => XmlHelper.GetNodesInPlainText("ParameterAttribute", x)).ToList()
                        : new List<string>();

        public string ConstraintsBaseTypeName => Constraints != null ? XmlHelper.GetChildElementValue(Constraints, "BaseTypeName") : string.Empty;

        public DocsTypeParameter(XElement xeTypeParameter) => XETypeParameter = xeTypeParameter;
    }
}
