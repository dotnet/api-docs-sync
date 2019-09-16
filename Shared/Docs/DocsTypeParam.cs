using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    /// <summary>
    /// Each one of these typeparam objects live inside the Docs section inside the Member object.
    /// </summary>
    public class DocsTypeParam
    {
        private XElement XETypeParam = null;
        public IDocsAPI ParentAPI
        {
            get; private set;
        }
        public string Name
        {
            get
            {
                return XmlHelper.GetAttributeValue(XETypeParam, "name");
            }
        }
        public string Value
        {
            get
            {
                return XmlHelper.GetRealValue(XETypeParam);
            }
            set
            {
                XmlHelper.FormatAsNormalElement(ParentAPI, XETypeParam, value);
            }
        }

        public DocsTypeParam(IDocsAPI parentAPI, XElement xeTypeParam)
        {
            ParentAPI = parentAPI;
            XETypeParam = xeTypeParam;
        }
    }
}