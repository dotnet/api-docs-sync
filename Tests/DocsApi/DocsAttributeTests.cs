using System.Xml.Linq;
using Xunit;

namespace Libraries.Docs.Tests
{
    public class DocsAttributeTests
    {
        [Theory]
        [InlineData(
            @"<Attribute FrameworkAlternate=""netframework-4.0"" />",
            @"netframework-4.0")]
        public void ExtractsFrameworkAlternate(string xml, string expected)
        {
            var attribute = new DocsAttribute(XElement.Parse(xml));
            Assert.Equal(expected, attribute.FrameworkAlternate);
        }

        [Theory]
        [InlineData("C#", @"[System.Runtime.TargetedPatchingOptOut(""Performance critical to inline this type of method across NGen image boundaries"")]")]
        [InlineData("F#", @"[<System.Runtime.TargetedPatchingOptOut(""Performance critical to inline this type of method across NGen image boundaries"")>]")]
        public void ExtractsAttributeNameByLanguage(string language, string expected)
        {
            var attribute = new DocsAttribute(XElement.Parse(@"
                <Attribute FrameworkAlternate=""netframework-4.0"">
                    <AttributeName Language=""C#"">[System.Runtime.TargetedPatchingOptOut(""Performance critical to inline this type of method across NGen image boundaries"")]</AttributeName>
                    <AttributeName Language=""F#"">[&lt;System.Runtime.TargetedPatchingOptOut(""Performance critical to inline this type of method across NGen image boundaries"")&gt;]</AttributeName>
                </Attribute>
            "));

            var actual = attribute.GetAttributeName(language);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(@"
            <Attribute FrameworkAlternate=""netframework-4.0"">
                <AttributeName Language=""C#"">[System.Runtime.TargetedPatchingOptOut(""Performance critical to inline this type of method across NGen image boundaries"")]</AttributeName>
                <AttributeName Language=""F#"">[&lt;System.Runtime.TargetedPatchingOptOut(""Performance critical to inline this type of method across NGen image boundaries"")&gt;]</AttributeName>
            </Attribute>",
            @"[System.Runtime.TargetedPatchingOptOut(""Performance critical to inline this type of method across NGen image boundaries"")]")]
        public void ExtractsAttributeNameForCsharpByDefault(string xml, string expected)
        {
            var attribute = new DocsAttribute(XElement.Parse(xml));
            var actual = attribute.GetAttributeName("C#");

            Assert.Equal(expected, actual);
        }
    }
}
