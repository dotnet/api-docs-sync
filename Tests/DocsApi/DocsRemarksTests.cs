using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace Libraries.Docs.Tests
{
    public class DocsRemarksTests
    {
        [Theory]
        [InlineData(
            @"<remarks>Remarks.</remarks>",
            @"Remarks.")]
        [InlineData(
            @"<remarks>Remarks referencing <see cref=""T:System.Int32"" />.</remarks>",
            @"Remarks referencing <see cref=""T:System.Int32"" />.")]
        [InlineData(
            @"<remarks>
                Multiline
                Remarks
                Referencing
                <see cref=""T:System.Int32"" />.
            </remarks>",
            @"
                Multiline
                Remarks
                Referencing
                <see cref=""T:System.Int32"" />.
            ")]
        public void GetsRawText(string xml, string expected)
        {
            var remarks = new DocsRemarks(XElement.Parse(xml));
            Assert.Equal(expected, remarks.RawText);
        }

        [Theory]
        [InlineData(
            @"<remarks>Remarks.</remarks>",
            @"Remarks.")]
        [InlineData(
            @"<remarks>Remarks referencing <see cref=""T:System.Int32"" />.</remarks>",
            @"Remarks referencing <see cref=""int"" />.")]
        [InlineData(
            @"<remarks>
                Multiline

                Remarks

                Referencing

                <see cref=""T:System.Int32"" />

                With Blank Lines.
            </remarks>",
            @"Multiline
Remarks
Referencing
<see cref=""int"" />
With Blank Lines.")]
        public void GetsParsedText(string xml, string expected)
        {
            var remarks = new DocsRemarks(XElement.Parse(xml));
            Assert.Equal(expected, remarks.ParsedText);
        }

        [Theory]
        [InlineData(
            @"<remarks>
                <format type=""text/markdown""><![CDATA[
                ## Remarks
                Markdown remarks
                ]]></format>
            </remarks>",
            @"Markdown remarks")]
        [InlineData(
            @"<remarks>
                <format type=""text/markdown""><![CDATA[
                ##Remarks
                Markdown remarks
                ]]></format>
            </remarks>",
            @"Markdown remarks")]
        public void RemovesRemarksHeader(string xml, string expected)
        {
            var remarks = new DocsRemarks(XElement.Parse(xml));
            Assert.Equal(expected, remarks.ParsedText);
        }

        [Theory]
        [InlineData( // [!INCLUDE
            @"<remarks><format type=""text/markdown""><![CDATA[ [!INCLUDE ]]></format></remarks>",
            @"<format type=""text/markdown""><![CDATA[ [!INCLUDE ]]></format>")]
        [InlineData( // [!NOTE
            @"<remarks><format type=""text/markdown""><![CDATA[ [!NOTE ]]></format></remarks>",
            @"<format type=""text/markdown""><![CDATA[ [!NOTE ]]></format>")]
        [InlineData( // [!IMPORTANT
            @"<remarks><format type=""text/markdown""><![CDATA[ [!IMPORTANT ]]></format></remarks>",
            @"<format type=""text/markdown""><![CDATA[ [!IMPORTANT ]]></format>")]
        [InlineData( // [!TIP
            @"<remarks><format type=""text/markdown""><![CDATA[ [!TIP ]]></format></remarks>",
            @"<format type=""text/markdown""><![CDATA[ [!TIP ]]></format>")]
        [InlineData( // [!code-cpp
            @"<remarks><format type=""text/markdown""><![CDATA[ [!code-cpp ]]></format></remarks>",
            @"<format type=""text/markdown""><![CDATA[ [!code-cpp ]]></format>")]
        [InlineData( // [!code-csharp
            @"<remarks><format type=""text/markdown""><![CDATA[ [!code-csharp ]]></format></remarks>",
            @"<format type=""text/markdown""><![CDATA[ [!code-csharp ]]></format>")]
        [InlineData( // [!code-vb
            @"<remarks><format type=""text/markdown""><![CDATA[ [!code-vb ]]></format></remarks>",
            @"<format type=""text/markdown""><![CDATA[ [!code-vb ]]></format>")]
        public void RetainsMarkdownFormatForUnparseableContent(string xml, string expected)
        {
            var remarks = new DocsRemarks(XElement.Parse(xml));
            Assert.Equal(expected, remarks.ParsedText);
        }

        [Theory]
        [InlineData( // [!INCLUDE -- Without CDATA
            @"<remarks><format type=""text/markdown"">Has an inline include. [!INCLUDE[include-file](~/includes/include-file.md)]</format></remarks>",
            @"<format type=""text/markdown"">Has an inline include. [!INCLUDE[include-file](~/includes/include-file.md)]</format>")]
        [InlineData( // [!INCLUDE -- With CDATA
            @"<remarks><format type=""text/markdown""><![CDATA[Has an inline include. [!INCLUDE[include-file](~/includes/include-file.md)]]]></format></remarks>",
            @"<format type=""text/markdown""><![CDATA[Has an inline include. [!INCLUDE[include-file](~/includes/include-file.md)]]]></format>")]
        [InlineData( // [!INCLUDE -- With CDATA and newlines
            @"<remarks><format type=""text/markdown""><![CDATA[
                Has an inline include. [!INCLUDE[include-file](~/includes/include-file.md)]
            ]]></format></remarks>",
            @"<format type=""text/markdown""><![CDATA[
Has an inline include. [!INCLUDE[include-file](~/includes/include-file.md)]
]]></format>")]
        public void RetainsMarkdownStructure(string xml, string expected)
        {
            var remarks = new DocsRemarks(XElement.Parse(xml));
            Assert.Equal(expected, remarks.ParsedText);
        }

        [Theory]
        [InlineData(
            @"<remarks><format type=""text/markdown""><![CDATA[
                ## Remarks

                Has an inline include. [!INCLUDE[include-file](~/includes/include-file.md)]
                ]]></format></remarks>",
            @"<format type=""text/markdown""><![CDATA[
Has an inline include. [!INCLUDE[include-file](~/includes/include-file.md)]
]]></format>")]
        public void RetainsMarkdownStructureButRemovesHeader(string xml, string expected)
        {
            var remarks = new DocsRemarks(XElement.Parse(xml));
            Assert.Equal(expected, remarks.ParsedText);
        }

        [Fact]
        public void ReplacesMarkdownXrefWithTags()
        {
            var xml = @"<remarks><format type=""text/markdown""><![CDATA[
                ## Remarks
                See <xref:Accessibility>.
            ]]></format></remarks>";

            var expected = @"See <see cref=""Accessibility"" />.";
            var remarks = new DocsRemarks(XElement.Parse(xml));

            Assert.Equal(expected, remarks.ParsedText);
        }

        [Fact]
        public void ReplacesMarkdownLinksWithTags()
        {
            var xml = @"<remarks><format type=""text/markdown""><![CDATA[
                See [the web](https://dot.net).
            ]]></format></remarks>";

            var expected = @"See <a href=""https://dot.net"">the web</a>.";
            var remarks = new DocsRemarks(XElement.Parse(xml));

            Assert.Equal(expected, remarks.ParsedText);
        }

        [Theory]
        [InlineData(@"Use `async` methods.", @"Use <see langword=""async"" /> methods.")]
        [InlineData(@"The `T` generic type parameter must be a struct.", @"The <typeparamref name=""T"" /> generic type parameter must be a struct.")]
        [InlineData(@"The `length` parameter cannot be negative.", @"The <paramref name=""length"" /> parameter cannot be negative.")]
        [InlineData(@"See `System.ComponentModel.DataAnnotations.Validator`.", @"See <see cref=""System.ComponentModel.DataAnnotations.Validator"" />.")]
        public void ReplacesMarkdownBacktickReferencesWithTags(string markdown, string expected)
        {
            var xml = $@"<remarks><format type=""text/markdown""><![CDATA[
                {markdown}
            ]]></format></remarks>";

            var testDoc = new TestDocsApi();
            var typeParamT = new DocsTypeParam(testDoc, XElement.Parse(@"<typeparam name=""T"">The struct.</typeparam>"));
            var paramLength = new DocsParam(testDoc, XElement.Parse(@"<param name=""length"">The length.</param>"));

            var remarks = new DocsRemarks(XElement.Parse(xml))
            {
                TypeParams = new[] { typeParamT },
                Params = new[] { paramLength }
            };

            Assert.Equal(expected, remarks.ParsedText);
        }

        [Theory]
        [InlineData(@"
            <remarks><format type=""text/markdown"">
                ## EXAMPLE
                EXAMPLE CONTENT
            </format></remarks>")]
        [InlineData(@"
            <remarks><format type=""text/markdown"">
                ##EXAMPLES
                EXAMPLE CONTENT
            </format></remarks>")]
        public void RemovesExamplesFromRemarks(string xml)
        {
            var remarks = new DocsRemarks(XElement.Parse(xml));
            Assert.DoesNotContain("EXAMPLE CONTENT", remarks.ParsedText);
        }

        [Theory]
        [InlineData(@"
            <remarks><format type=""text/markdown"">
                ## EXAMPLE
                EXAMPLE CONTENT
            </format></remarks>",
            @"")]
        [InlineData(@"
            <remarks><format type=""text/markdown"">
                ##EXAMPLES
                EXAMPLE CONTENT
            </format></remarks>",
            @"")]
        [InlineData(@"
            <remarks><format type=""text/markdown"">
                ## REMARKS

                REMARK CONTENT

                ##EXAMPLES

                EXAMPLE CONTENT
            </format></remarks>",
            @"REMARK CONTENT")]
        public void TrimsRemarksAfterRemovingExamples(string xml, string expected)
        {
            var remarks = new DocsRemarks(XElement.Parse(xml));
            Assert.Equal(expected, remarks.ParsedText);
        }

        [Theory]
        [InlineData(@"
            <remarks><format type=""text/markdown"">
                ## EXAMPLE
                EXAMPLE CONTENT
            </format></remarks>",
            @"EXAMPLE CONTENT")]
        [InlineData(@"
            <remarks><format type=""text/markdown"">
                ##EXAMPLES
                EXAMPLE CONTENT
            </format></remarks>",
            @"EXAMPLE CONTENT")]
        [InlineData(@"
            <remarks><format type=""text/markdown"">
                ## REMARKS

                REMARK CONTENT

                ##EXAMPLES

                EXAMPLE CONTENT
            </format></remarks>",
            @"EXAMPLE CONTENT")]
        public void GetsExampleContent(string xml, string expected)
        {
            var remarks = new DocsRemarks(XElement.Parse(xml));
            Assert.Equal(expected, remarks.ExampleContent?.ParsedText);
        }

        [Fact]
        public void GetsNodes()
        {
            var xml = @"<remarks>Remarks referencing a <see cref=""T:System.Type"" />.</remarks>";
            var remarks = new DocsRemarks(XElement.Parse(xml));

            var expected = new XNode[]
            {
                new XText("Remarks referencing a "),
                XElement.Parse(@"<see cref=""T:System.Type"" />"),
                new XText(".")
            };

            Assert.Equal(expected.Select(x => x.ToString()), remarks.RawNodes.ToArray().Select(x => x.ToString()));
        }

        [Fact]
        public void CanIncludeSeeElements()
        {
            var xml = @"<remarks><see cref=""T:System.Type"" /></remarks>";
            var remarks = new DocsRemarks(XElement.Parse(xml));
            var see = remarks.RawNodes.Single();

            Assert.Equal(XmlNodeType.Element, see.NodeType);
        }

        [Fact]
        public void CanExposeRawSeeElements()
        {
            var xml = @"<remarks><see cref=""T:System.Type"" /></remarks>";
            var remarks = new DocsRemarks(XElement.Parse(xml));
            var see = remarks.RawNodes.Single();

            Assert.Equal("see", ((XElement)see).Name);
        }

        [Fact]
        public void CanExposeRawSeeCrefValues()
        {
            var xml = @"<remarks><see cref=""T:System.Type"" /></remarks>";
            var remarks = new DocsRemarks(XElement.Parse(xml));
            var see = remarks.RawNodes.Single();

            Assert.Equal("T:System.Type", ((XElement)see).Attribute("cref").Value);
        }

        [Fact]
        public void ParsesNodes()
        {
            var xml = @"<remarks>Remarks referencing a <see cref=""T:System.Collections.Generic.IEnumerable`1"" />.</remarks>";
            var remarks = new DocsRemarks(XElement.Parse(xml));

            var expected = new XNode[]
            {
                new XText("Remarks referencing a "),
                XElement.Parse(@"<see cref=""System.Collections.Generic.IEnumerable{T}"" />"),
                new XText(".")
            };

            Assert.Equal(expected.Select(x => x.ToString()), remarks.ParsedNodes.ToArray());
        }
    }
}
