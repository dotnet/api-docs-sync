﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ApiDocsSync.PortToDocs.Docs
{
    internal class DocsAssemblyInfo
    {
        private readonly XElement XEAssemblyInfo;
        public string AssemblyName
        {
            get
            {
                return XmlHelper.GetChildElementValue(XEAssemblyInfo, "AssemblyName");
            }
        }

        private List<string>? _assemblyVersions;
        public List<string> AssemblyVersions
        {
            get
            {
                if (_assemblyVersions == null)
                {
                    _assemblyVersions = XEAssemblyInfo.Elements("AssemblyVersion").Select(x => XmlHelper.GetNodesInPlainText(x)).ToList();
                }
                return _assemblyVersions;
            }
        }

        public DocsAssemblyInfo(XElement xeAssemblyInfo)
        {
            XEAssemblyInfo = xeAssemblyInfo;
        }

        public override string ToString() => AssemblyName;
    }
}
