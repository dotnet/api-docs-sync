using System;
using System.Linq;
using System.Xml.Linq;

namespace Libraries.Docs
{
    public class DocsSummary : DocsTextElement
    {
        public DocsSummary(XElement xeSummary) : base(xeSummary)
        {
        }
    }
}
