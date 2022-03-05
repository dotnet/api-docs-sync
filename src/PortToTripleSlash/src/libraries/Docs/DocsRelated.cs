// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace ApiDocsSync.Libraries.Docs
{
    internal class DocsRelated
    {
        private readonly XElement XERelatedArticle;

        public IDocsAPI ParentAPI
        {
            get; private set;
        }

        public string ArticleType => XmlHelper.GetAttributeValue(XERelatedArticle, "type");

        public string Href => XmlHelper.GetAttributeValue(XERelatedArticle, "href");

        public string Value
        {
            get => XmlHelper.GetNodesInPlainText(XERelatedArticle);
            set
            {
                XmlHelper.SaveFormattedAsXml(XERelatedArticle, value);
                ParentAPI.Changed = true;
            }
        }

        public DocsRelated(IDocsAPI parentAPI, XElement xeRelatedArticle)
        {
            ParentAPI = parentAPI;
            XERelatedArticle = xeRelatedArticle;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
