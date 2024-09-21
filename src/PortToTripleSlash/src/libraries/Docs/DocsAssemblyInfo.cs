// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ApiDocsSync.PortToTripleSlash.Docs
{
    internal class DocsAssemblyInfo
    {
        private readonly XElement XEAssemblyInfo;

        public string AssemblyName => XmlHelper.GetChildElementValue(XEAssemblyInfo, "AssemblyName");

        private List<string>? _assemblyVersions;
        public List<string> AssemblyVersions => _assemblyVersions ??= XEAssemblyInfo.Elements("AssemblyVersion").Select(x => XmlHelper.GetNodesInPlainText("AssemblyVersion", x)).ToList();

        public DocsAssemblyInfo(XElement xeAssemblyInfo) => XEAssemblyInfo = xeAssemblyInfo;

        public override string ToString() => AssemblyName;
    }
}
