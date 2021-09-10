using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace Libraries.Docs.Tests
{
    public class DocsExampleTests
    {
        [Theory]
        [InlineData(
            @"<example>Example.</example>",
            @"Example.")]
        [InlineData(
            @"<example>Example referencing <see cref=""T:System.Int32"" />.</example>",
            @"Example referencing <see cref=""T:System.Int32"" />.")]
        [InlineData(
            @"<example>
                Multiline
                Example
                Referencing
                <see cref=""T:System.Int32"" />.
            </example>",
            @"
                Multiline
                Example
                Referencing
                <see cref=""T:System.Int32"" />.
            ")]
        public void GetsRawText(string xml, string expected)
        {
            var example = new DocsExample(XElement.Parse(xml));
            Assert.Equal(expected, example.RawText);
        }

        [Theory]
        [InlineData(
            @"<example>Example.</example>",
            @"Example.")]
        [InlineData(
            @"<example>Example referencing <see cref=""T:System.Int32"" />.</example>",
            @"Example referencing <see cref=""int"" />.")]
        [InlineData(
            @"<example>
                Multiline

                Example

                Referencing

                <see cref=""T:System.Int32"" />

                With Blank Lines.
            </example>",
            @"Multiline
Example
Referencing
<see cref=""int"" />
With Blank Lines.")]
        [InlineData(
            @"<example>
                <format type=""text/markdown""><![CDATA[
                ## Example
                Markdown example
                ]]></format>
            </example>",
            @"Markdown example")]
        public void GetsParsedText(string xml, string expected)
        {
            var example = new DocsExample(XElement.Parse(xml));
            Assert.Equal(expected, example.ParsedText);
        }

        [Theory]
        [InlineData( // [!INCLUDE
            @"<example><format type=""text/markdown""><![CDATA[ [!INCLUDE ]]></format></example>",
            @"<format type=""text/markdown""><![CDATA[ [!INCLUDE ]]></format>")]
        [InlineData( // [!NOTE
            @"<example><format type=""text/markdown""><![CDATA[ [!NOTE ]]></format></example>",
            @"<format type=""text/markdown""><![CDATA[ [!NOTE ]]></format>")]
        [InlineData( // [!IMPORTANT
            @"<example><format type=""text/markdown""><![CDATA[ [!IMPORTANT ]]></format></example>",
            @"<format type=""text/markdown""><![CDATA[ [!IMPORTANT ]]></format>")]
        [InlineData( // [!TIP
            @"<example><format type=""text/markdown""><![CDATA[ [!TIP ]]></format></example>",
            @"<format type=""text/markdown""><![CDATA[ [!TIP ]]></format>")]
        [InlineData( // [!code-cpp
            @"<example><format type=""text/markdown""><![CDATA[ [!code-cpp ]]></format></example>",
            @"<format type=""text/markdown""><![CDATA[ [!code-cpp ]]></format>")]
        [InlineData( // [!code-csharp
            @"<example><format type=""text/markdown""><![CDATA[ [!code-csharp ]]></format></example>",
            @"<format type=""text/markdown""><![CDATA[ [!code-csharp ]]></format>")]
        [InlineData( // [!code-vb
            @"<example><format type=""text/markdown""><![CDATA[ [!code-vb ]]></format></example>",
            @"<format type=""text/markdown""><![CDATA[ [!code-vb ]]></format>")]
        public void RetainsMarkdownFormatForUnparseableContent(string xml, string expected)
        {
            var example = new DocsExample(XElement.Parse(xml));
            Assert.Equal(expected, example.ParsedText);
        }

        [Theory]
        [InlineData( // [!INCLUDE -- Without CDATA
            @"<example><format type=""text/markdown"">Has an inline include. [!INCLUDE[include-file](~/includes/include-file.md)]</format></example>",
            @"<format type=""text/markdown"">Has an inline include. [!INCLUDE[include-file](~/includes/include-file.md)]</format>")]
        [InlineData( // [!INCLUDE -- With CDATA
            @"<example><format type=""text/markdown""><![CDATA[Has an inline include. [!INCLUDE[include-file](~/includes/include-file.md)]]]></format></example>",
            @"<format type=""text/markdown""><![CDATA[Has an inline include. [!INCLUDE[include-file](~/includes/include-file.md)]]]></format>")]
        [InlineData( // [!INCLUDE -- With CDATA and newlines
            @"<example><format type=""text/markdown""><![CDATA[
                Has an inline include. [!INCLUDE[include-file](~/includes/include-file.md)]
            ]]></format></example>",
            @"<format type=""text/markdown""><![CDATA[
Has an inline include. [!INCLUDE[include-file](~/includes/include-file.md)]
]]></format>")]
        public void RetainsMarkdownStructure(string xml, string expected)
        {
            var example = new DocsExample(XElement.Parse(xml));
            Assert.Equal(expected, example.ParsedText);
        }

        [Fact]
        public void ReplacesMarkdownCodeSnippetsWithTags()
        {
            var xml = @"<example><format type=""text/markdown""><![CDATA[
Here's an example:

```csharp
Console.WriteLine(""Hello World!"");
```

            ]]></format></example>";

            var expected = @"Here's an example:
<code class=""lang-csharp"">
Console.WriteLine(""Hello World!"");
</code>";

            var example = new DocsExample(XElement.Parse(xml));
            Assert.Equal(expected, example.ParsedText);
        }

        [Fact]
        public void GetsNodes()
        {
            var xml = @"<example>Example referencing a <see cref=""T:System.Type"" />.</example>";
            var example = new DocsExample(XElement.Parse(xml));

            var expected = new XNode[]
            {
                new XText("Example referencing a "),
                XElement.Parse(@"<see cref=""T:System.Type"" />"),
                new XText(".")
            };

            Assert.Equal(expected.Select(x => x.ToString()), example.RawNodes.ToArray().Select(x => x.ToString()));
        }

        [Fact]
        public void CanIncludeSeeElements()
        {
            var xml = @"<example><see cref=""T:System.Type"" /></example>";
            var example = new DocsExample(XElement.Parse(xml));
            var see = example.RawNodes.Single();

            Assert.Equal(XmlNodeType.Element, see.NodeType);
        }

        [Fact]
        public void CanExposeRawSeeElements()
        {
            var xml = @"<example><see cref=""T:System.Type"" /></example>";
            var example = new DocsExample(XElement.Parse(xml));
            var see = example.RawNodes.Single();

            Assert.Equal("see", ((XElement)see).Name);
        }

        [Fact]
        public void CanExposeRawSeeCrefValues()
        {
            var xml = @"<example><see cref=""T:System.Type"" /></example>";
            var example = new DocsExample(XElement.Parse(xml));
            var see = example.RawNodes.Single();

            Assert.Equal("T:System.Type", ((XElement)see).Attribute("cref").Value);
        }

        [Fact]
        public void ParsesNodes()
        {
            var xml = @"<example>Example referencing a <see cref=""T:System.Collections.Generic.IEnumerable`1"" />.</example>";
            var example = new DocsExample(XElement.Parse(xml));

            var expected = new XNode[]
            {
                new XText("Example referencing a "),
                XElement.Parse(@"<see cref=""System.Collections.Generic.IEnumerable{T}"" />"),
                new XText(".")
            };

            Assert.Equal(expected.Select(x => x.ToString()), example.ParsedNodes.ToArray());
        }
    }
}
