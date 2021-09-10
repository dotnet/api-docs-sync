using System.Text;
using System.Xml.Linq;
using Xunit;

namespace Libraries.Docs.Tests
{
    public class DocsTypeTests
    {
        [Theory]
        [InlineData( // No remarks
            @"<Docs />",
            @"")]
        [InlineData( // Plain text
            @"<Docs><remarks>These are remarks</remarks></Docs>",
            @"These are remarks")]
        [InlineData( // With &lt; and &gt; sequences
            @"<Docs><remarks>These are remarks with &lt;xml&gt; like content</remarks></Docs>",
            @"These are remarks with &lt;xml&gt; like content")]
        [InlineData( // With markdown content
            @"<Docs>
                <remarks>
                  <format type=""text/markdown""><![CDATA[
                  ## Remarks

                  This is *markdown* with <bold>HTML</bold> embedded.

                  ]]></format>
                </remarks>
            </Docs>",
            @"<![CDATA[
                  ## Remarks

                  This is *markdown* with <bold>HTML</bold> embedded.

                  ]]>"
        )]
        public void ExtractsRemarksAsPlainText(string xml, string expected)
        {
            var doc = XDocument.Parse(@$"<Type Name=""DocsTypeTests"" FullName=""Libraries.Docs.Tests.DocsTypeTests"">{xml}</Type>");
            var type = new DocsType("MyType.xml", doc, doc.Root, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            Assert.Equal(expected, type.Remarks);
        }
    }
}
