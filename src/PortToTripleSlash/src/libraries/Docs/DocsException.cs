// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace ApiDocsSync.PortToTripleSlash.Docs
{
    internal class DocsException
    {
        private readonly XElement XEException;

        public IDocsAPI ParentAPI { get; }

        public string Cref => XmlHelper.GetAttributeValue(XEException, "cref").DocIdEscaped();

        public string Value => XmlHelper.GetNodesInPlainText("exception", XEException);

        public string OriginalValue { get; private set; }

        public DocsException(IDocsAPI parentAPI, XElement xException)
        {
            ParentAPI = parentAPI;
            XEException = xException;
            OriginalValue = Value;
        }

        public override string ToString() => $"{Cref} - {Value}";
    }
}
