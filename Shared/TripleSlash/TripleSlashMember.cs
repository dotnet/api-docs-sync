using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DocsPortingTool.TripleSlash
{
    public class TripleSlashMember
    {
        private XElement XEMember;

        public string Assembly { get; private set; }


        private string _namespace = string.Empty;
        public string Namespace
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_namespace))
                {
                    string[] splittedParenthesis = Name.Split('(', StringSplitOptions.RemoveEmptyEntries);
                    string withoutParenthesisAndPrefix = splittedParenthesis[0].Substring(2);
                    string[] splittedDots = withoutParenthesisAndPrefix.Split('.', StringSplitOptions.RemoveEmptyEntries);

                    _namespace = Configuration.ReplaceNamespace(string.Join('.', splittedDots.Take(splittedDots.Length - 1)));
                }

                return _namespace;
            }
        }

        private string _name = string.Empty;
        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_name))
                {
                    _name = Configuration.ReplaceNamespace(XmlHelper.GetAttributeValue(XEMember, "name"));
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
                    XElement xElement = XEMember.Element("summary");
                    if (xElement != null)
                        _summary = XmlHelper.GetNodesInPlainText(xElement);
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
                    try
                    {
                        XElement xElement = XEMember.Element("value");
                        if (xElement != null)
                            _value = XmlHelper.GetNodesInPlainText(xElement);
                    }
                    catch { }
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
                    try
                    {
                        XElement xElement = XEMember.Element("returns");
                        if (xElement != null)
                            _returns = XmlHelper.GetNodesInPlainText(xElement);
                    }
                    catch { }
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
                    try
                    {
                        XElement xElement = XEMember.Element("remarks");
                        if (xElement != null)
                            _remarks = XmlHelper.GetNodesInPlainText(xElement);
                    }
                    catch { }
                }
                return _remarks;
            }
        }

        public TripleSlashMember(XElement xeMember, string assembly)
        {
            if (xeMember == null)
            {
                throw new ArgumentNullException(nameof(xeMember));
            }
            if (string.IsNullOrEmpty(assembly))
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            XEMember = xeMember;
            Assembly = assembly;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
