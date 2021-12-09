using System.Xml.Linq;
using Xunit;

namespace Libraries.Docs.Tests
{
    public class DocsExceptionTests
    {
        [Theory]
        [InlineData(
            @"<exception cref=""T:System.InvalidOperationException"">
                    If a null reference is returned from <paramref name=""projectInstanceFactory"" /></exception>",
            @"T:System.InvalidOperationException")]
        public void ExtractsCref(string xml, string expected)
        {
            var parent = new TestDocsApi();
            var exception = new DocsException(parent, XElement.Parse(xml));

            Assert.Equal(expected, exception.Cref);
        }

        [Theory]
        [InlineData(
            @"<exception cref=""T:System.InvalidOperationException"">
                    If a null reference is returned from <paramref name=""projectInstanceFactory"" /></exception>",
            @"If a null reference is returned from <paramref name=""projectInstanceFactory"" />")]
        [InlineData(
            @"<exception cref=""T:System.IndexOutOfRangeException"">This is the IndexOutOfRangeException thrown by MyVoidMethod.

-or-

This is the second case.

Empty newlines should be respected.</exception>",
            @"This is the IndexOutOfRangeException thrown by MyVoidMethod.

-or-

This is the second case.

Empty newlines should be respected.")]
        public void ExtractsValueInPlainText(string xml, string expected)
        {
            var parent = new TestDocsApi();
            var exception = new DocsException(parent, XElement.Parse(xml));

            Assert.Equal(expected, exception.Value);
        }
    }
}
