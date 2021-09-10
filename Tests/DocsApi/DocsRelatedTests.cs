using System.Xml.Linq;
using Xunit;

namespace Libraries.Docs.Tests
{
    public class DocsRelatedTests
    {
        [Fact]
        public void ExtractsArticleType()
        {
            var parent = new TestDocsApi();
            var related = new DocsRelated(parent, XElement.Parse(@"
                <related type=""Article"" href=""/dotnet/csharp/programming-guide/concepts/iterators"">Iterators (C#)</related>
            "));

            Assert.Equal("Article", related.ArticleType);
        }

        [Fact]
        public void ExtractsHref()
        {
            var parent = new TestDocsApi();
            var related = new DocsRelated(parent, XElement.Parse(@"
                <related type=""Article"" href=""/dotnet/csharp/programming-guide/concepts/iterators"">Iterators (C#)</related>
            "));

            Assert.Equal("/dotnet/csharp/programming-guide/concepts/iterators", related.Href);
        }

        [Theory]
        [InlineData(
            @"<related type=""Article"" href=""/dotnet/csharp/programming-guide/concepts/iterators"">Iterators (C#)</related>",
            @"Iterators (C#)")]
        [InlineData(
            @"<related type=""Article"" href=""/dotnet/framework/configure-apps/file-schema/compiler/compilers-element"">&lt;compilers&gt; Element</related>",
            @"&lt;compilers&gt; Element")]
        [InlineData(
            @"<related type=""article"" href=""/dotnet/standard/memory-and-spans/memory-t-usage-guidelines"">Memory&lt;T&gt; and Span&lt;T&gt; usage guidelines</related>",
            @"Memory&lt;T&gt; and Span&lt;T&gt; usage guidelines")]
        public void ExtractsValueAsPlainText(string xml, string expected)
        {
            var parent = new TestDocsApi();
            var related = new DocsRelated(parent, XElement.Parse(xml));
            
            Assert.Equal(expected, related.Value);
        }
    }
}
