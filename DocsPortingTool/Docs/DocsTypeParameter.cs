using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DocsPortingTool.Docs
{
    /// <summary>
    /// Each one of these TypeParameter objects islocated inside the TypeParameters section inside the Member.
    /// </summary>
    public class DocsTypeParameter
    {
        private XElement XETypeParameter = null;
        public string Name
        {
            get
            {
                return XmlHelper.GetAttributeValue(XETypeParameter, "Name");
            }
        }
        private XElement Constraints
        {
            get
            {
                return XmlHelper.GetChildElement(XETypeParameter, "Constraints");
            }
        }
        private List<string> _constraintsParamterAttributes;
        public List<string> ConstraintsParameterAttributes
        {
            get
            {
                if (_constraintsParamterAttributes == null)
                {
                    if (Constraints != null)
                    {
                        _constraintsParamterAttributes = Constraints.Elements("ParameterAttribute").Select(x => XmlHelper.GetRealValue(x)).ToList();
                    }
                    else
                    {
                        _constraintsParamterAttributes = new List<string>();
                    }
                }
                return _constraintsParamterAttributes;
            }
        }

        public string ConstraintsBaseTypeName
        {
            get
            {
                if (Constraints != null)
                {
                    return XmlHelper.GetChildElementValue(Constraints, "BaseTypeName");
                }
                return string.Empty;
            }
        }

        public DocsTypeParameter(XElement xeTypeParameter)
        {
            XETypeParameter = xeTypeParameter;
        }
    }
}