// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace ApiDocsSync.PortToTripleSlash.Docs
{
    /// <summary>
    /// Each one of these typeparam objects live inside the Docs section inside the Member object.
    /// </summary>
    internal class DocsTypeParam
    {
        private readonly XElement XEDocsTypeParam;

        public IDocsAPI ParentAPI { get; }

        public string Name => XmlHelper.GetAttributeValue(XEDocsTypeParam, "name");

        public string Value => XmlHelper.GetNodesInPlainText("typeparam", XEDocsTypeParam);

        public DocsTypeParam(IDocsAPI parentAPI, XElement xeDocsTypeParam)
        {
            ParentAPI = parentAPI;
            XEDocsTypeParam = xeDocsTypeParam;
        }
    }
}
