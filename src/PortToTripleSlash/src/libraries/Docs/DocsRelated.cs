// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace ApiDocsSync.PortToTripleSlash.Docs
{
    internal class DocsRelated
    {
        private readonly XElement XERelatedArticle;

        public IDocsAPI ParentAPI { get; }

        public string ArticleType => XmlHelper.GetAttributeValue(XERelatedArticle, "type");

        public string Href => XmlHelper.GetAttributeValue(XERelatedArticle, "href");

        public string Value => XmlHelper.GetNodesInPlainText("related", XERelatedArticle);

        public DocsRelated(IDocsAPI parentAPI, XElement xeRelatedArticle)
        {
            ParentAPI = parentAPI;
            XERelatedArticle = xeRelatedArticle;
        }

        public override string ToString() => Value;
    }
}
