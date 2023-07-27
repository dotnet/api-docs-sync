// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace ApiDocsSync.PortToTripleSlash.Docs
{
    internal class DocsParam
    {
        private readonly XElement XEDocsParam;

        public IDocsAPI ParentAPI { get; }

        public string Name => XmlHelper.GetAttributeValue(XEDocsParam, "name");

        public string Value => XmlHelper.GetNodesInPlainText("param", XEDocsParam);

        public DocsParam(IDocsAPI parentAPI, XElement xeDocsParam)
        {
            ParentAPI = parentAPI;
            XEDocsParam = xeDocsParam;
        }

        public override string ToString() => Name;
    }
}
