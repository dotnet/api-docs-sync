using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DocsPortingTool.TripleSlash
{
    public class TripleSlashMember
    {
        private readonly XElement XEMember;

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

        private string? _name;
        public string Name
        {
            get
            {
                if (_name == null)
                {
                    _name = Configuration.ReplaceNamespace(XmlHelper.GetAttributeValue(XEMember, "name"));
                }
                return _name;
            }
        }

        private List<TripleSlashParam>? _params;
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

        private List<TripleSlashTypeParam>? _typeParams;
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

        private List<TripleSlashException>? _exceptions;
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

        private string? _summary;
        public string Summary
        {
            get
            {
                if (_summary == null)
                {
                    XElement xElement = XEMember.Element("summary");
                    _summary = (xElement != null) ? XmlHelper.GetNodesInPlainText(xElement) : string.Empty;
                }
                return _summary;
            }
        }

        public string? _value;
        public string Value
        {
            get
            {
                if (_value == null)
                {
                    XElement xElement = XEMember.Element("value");
                    _value = (xElement != null) ? XmlHelper.GetNodesInPlainText(xElement) : string.Empty;
                }
                return _value;
            }
        }

        private string? _returns;
        public string Returns
        {
            get
            {
                if (_returns == null)
                {
                        XElement xElement = XEMember.Element("returns");
                        _returns = (xElement != null) ? XmlHelper.GetNodesInPlainText(xElement) : string.Empty;
                }
                return _returns;
            }
        }

        private string? _remarks;
        public string Remarks
        {
            get
            {
                if (_remarks == null)
                {
                        XElement xElement = XEMember.Element("remarks");
                        _remarks = (xElement !=  null) ? XmlHelper.GetNodesInPlainText(xElement) : string.Empty;
                }
                return _remarks;
            }
        }

        public TripleSlashMember(XElement xeMember, string assembly)
        {
            if (string.IsNullOrEmpty(assembly))
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            XEMember = xeMember ?? throw new ArgumentNullException(nameof(xeMember));
            Assembly = assembly;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
