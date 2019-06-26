using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    public class DocsMemberSignature
    {
        private XElement XEMemberSignature = null;

        public string Language
        {
            get
            {
                return XmlHelper.GetAttributeValue(XEMemberSignature, "Language");
            }
        }

        public string Value
        {
            get
            {
                return XmlHelper.GetAttributeValue(XEMemberSignature, "Value");
            }
        }

        public DocsMemberSignature(XElement xeMemberSignature)
        {
            XEMemberSignature = xeMemberSignature;
        }
    }
}