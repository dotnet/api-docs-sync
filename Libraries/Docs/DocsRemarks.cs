using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Libraries.Docs
{
    public class DocsRemarks : DocsMarkdownElement
    {
        public DocsRemarks(XElement xeRemarks) : base(xeRemarks)
        {
        }

        private DocsExample? _exampleContent;

        public DocsExample? ExampleContent
        {
            get
            {
                EnsureParsed();
                return _exampleContent;
            }
            set
            {
                _exampleContent = value;
            }
        }

        private static readonly Regex ExampleSectionPattern = new(@"^\s*##\s*Examples?\s*(?<examples>.*)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        protected override string ExtractElements(string markdown)
        {
            markdown = base.ExtractElements(markdown);
            markdown = RemoveMarkdownHeading(markdown, "Remarks");
            markdown = ExtractExamples(markdown);

            return markdown;
        }

        private string ExtractExamples(string markdown)
        {
            var match = ExampleSectionPattern.Match(markdown);

            if (match.Success)
            {
                string exampleContent = match.Groups["examples"].Value;
                string exampleXml = $@"<example><format type=""text/markdown""><![CDATA[
{exampleContent}
]]></format></example>";

                // Extract the examples (as a side effect)
                ExampleContent = new DocsExample(XElement.Parse(exampleXml));

                // Return all of the markdown content before the examples begin
                return markdown.Substring(0, match.Index);
            }

            return markdown;
        }
    }
}
