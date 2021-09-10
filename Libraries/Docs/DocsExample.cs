using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Libraries.Docs
{
    public class DocsExample : DocsMarkdownElement
    {
        public DocsExample(XElement xeExample) : base(xeExample)
        {
        }

        protected override string ExtractElements(string markdown)
        {
            markdown = base.ExtractElements(markdown);
            markdown = RemoveMarkdownHeading(markdown, "Examples?");

            return markdown;
        }
    }
}
