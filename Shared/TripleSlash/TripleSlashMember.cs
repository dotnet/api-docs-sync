using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DocsPortingTool.TripleSlash
{
    public class TripleSlashMember
    {
        private XElement XEMember = null;

        private string _assembly = string.Empty;
        public string Assembly
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_assembly))
                {
                    string[] splittedParenthesis = Name.Split('(', StringSplitOptions.RemoveEmptyEntries);
                    string withoutParenthesisAndPrefix = splittedParenthesis[0].Substring(2);
                    string[] splittedDots = withoutParenthesisAndPrefix.Split('.', StringSplitOptions.RemoveEmptyEntries);

                    _assembly = string.Join('.', splittedDots.Take(splittedDots.Length - 1));
                }

                return _assembly;
            }
        }

        private string _name = string.Empty;
        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_name))
                {
                    _name = XmlHelper.GetAttributeValue(XEMember, "name");
                }
                return _name;
            }
        }

        private List<TripleSlashParam> _params;
        public List<TripleSlashParam> Params
        {
            get
            {
                if (_params == null)
                {
                    _params = XEMember.Elements("param").Select(x => new TripleSlashParam(x)).ToList();
                }
                return _params;
            }
        }

        private List<TripleSlashTypeParam> _typeParams;
        public List<TripleSlashTypeParam> TypeParams
        {
            get
            {
                if (_typeParams == null)
                {
                    _typeParams = XEMember.Elements("typeparam").Select(x => new TripleSlashTypeParam(x)).ToList();
                }
                return _typeParams;
            }
        }

        private List<TripleSlashException> _exceptions;
        public IEnumerable<TripleSlashException> Exceptions
        {
            get
            {
                if (_exceptions == null)
                {
                    _exceptions = XEMember.Elements("exception").Select(x => new TripleSlashException(x)).ToList();
                }
                return _exceptions;
            }
        }

        private string _summary = string.Empty;
        public string Summary
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_summary))
                {
                    if (XmlHelper.TryGetChildElement(XEMember, "summary", out XElement xElement))
                    {
                        _summary = XmlHelper.GetNodesInPlainText(xElement);
                    }
                }
                return _summary;
            }
        }

        public string _value = string.Empty;
        public string Value
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_value))
                {
                    if (XmlHelper.TryGetChildElement(XEMember, "value", out XElement xElement))
                    {
                        _value = XmlHelper.GetNodesInPlainText(xElement);
                    }
                }
                return _value;
            }
        }

        private string _returns = string.Empty;
        public string Returns
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_returns))
                {
                    if (XmlHelper.TryGetChildElement(XEMember, "returns", out XElement xElement))
                    {
                        _returns = XmlHelper.GetNodesInPlainText(xElement);
                    }
                }
                return _returns;
            }
        }

        private string _remarks = string.Empty;
        public string Remarks
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_remarks))
                {
                    if (XmlHelper.TryGetChildElement(XEMember, "remarks", out XElement xElement))
                    {
                        _remarks = XmlHelper.GetNodesInPlainText(xElement);
                    }
                }
                return _remarks;
            }
        }

        public TripleSlashMember(XElement xeMember)
        {
            XEMember = xeMember;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
