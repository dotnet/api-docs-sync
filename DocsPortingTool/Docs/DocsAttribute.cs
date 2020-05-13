using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public class DocsAttribute
    {
        private readonly XElement XEAttribute = null;

        public string FrameworkAlternate
        {
            get
            {
                return XmlHelper.GetAttributeValue(XEAttribute, "FrameworkAlternate");
            }
        }
        public string AttributeName
        {
            get
            {
                return XmlHelper.GetChildElementValue(XEAttribute, "AttributeName");
            }
        }

        public DocsAttribute(XElement xeAttribute)
        {
            XEAttribute = xeAttribute;
        }
    }
}