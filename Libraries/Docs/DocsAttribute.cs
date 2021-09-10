using System.Linq;
using System.Xml.Linq;

namespace Libraries.Docs
{
    public class DocsAttribute
    {
        private readonly XElement XEAttribute;

        public string FrameworkAlternate
        {
            get
            {
                return XmlHelper.GetAttributeValue(XEAttribute, "FrameworkAlternate");
            }
        }

        public string? AttributeName
        {
            get => GetAttributeName("C#");
        }

        public string? GetAttributeName(string language)
        {
            return XEAttribute.Elements("AttributeName").Where(x => XmlHelper.GetAttributeValue(x, "Language") == language).SingleOrDefault()?.Value;
        }

        public DocsAttribute(XElement xeAttribute)
        {
            XEAttribute = xeAttribute;
        }
    }
}