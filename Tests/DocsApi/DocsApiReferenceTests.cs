using Xunit;

namespace Libraries.Docs.Tests
{
    public class DocsApiReferenceTests
    {
        [Theory]
        [InlineData("System.Boolean", "bool")]
        [InlineData("System.Byte", "byte")]
        [InlineData("System.Char", "char")]
        [InlineData("System.Decimal", "decimal")]
        [InlineData("System.Double", "double")]
        [InlineData("System.Int16", "short")]
        [InlineData("System.Int32", "int")]
        [InlineData("System.Int64", "long")]
        [InlineData("System.Object", "object")]
        [InlineData("System.SByte", "sbyte")]
        [InlineData("System.Single", "float")]
        [InlineData("System.String", "string")]
        [InlineData("System.UInt16", "ushort")]
        [InlineData("System.UInt32", "uint")]
        [InlineData("System.UInt64", "ulong")]
        [InlineData("System.Void", "void")]
        [InlineData("System.Int32.ToString(System.String)", "int.ToString(string)")]
        public void ReturnsShorthandPrimitives(string apiReference, string expected)
        {
            var reference = new DocsApiReference(apiReference);
            Assert.Equal(expected, reference.Api);
        }

        [Theory]
        [InlineData("T:System.Int32", "int")]
        [InlineData("M:System.Int32.ToString()", "int.ToString()")]
        [InlineData("O:System.Int32.ToString(System.String)", "int.ToString(string)")]
        public void RemovesPrefixFromApi(string apiReference, string expected)
        {
            var reference = new DocsApiReference(apiReference);
            Assert.Equal(expected, reference.Api);
        }

        [Theory]
        [InlineData("System.Int32", "int")]
        [InlineData("System.Int32.ToString()", "int.ToString()")]
        [InlineData("System.Int32.ToString(System.String)", "int.ToString(string)")]
        [InlineData("T:System.Int32", "T:int")]
        [InlineData("M:System.Int32.ToString()", "M:int.ToString()")]
        [InlineData("O:System.Int32.ToString(System.String)", "O:int.ToString(string)")]
        public void OverridesToStringWithPrefixAndApi(string apiReference, string expected)
        {
            var reference = new DocsApiReference(apiReference);
            Assert.Equal(expected, reference.ToString());
        }

        [Theory]
        [InlineData("MyNamespace.MyGenericType.Select`1", "MyNamespace.MyGenericType.Select{T}")]
        [InlineData("MyNamespace.MyGenericType.Select`2", "MyNamespace.MyGenericType.Select{T1,T2}")]
        [InlineData("MyNamespace.MyGenericType.Select``2(MyNamespace.MyGenericType{``0},System.Func{``0,``1})", "MyNamespace.MyGenericType.Select{T1,T2}(MyNamespace.MyGenericType{T1},System.Func{T1,T2})")]
        [InlineData("MyNamespace.MyGenericType.Select%60%602%28MyNamespace.MyGenericType%7B%60%600%7D%2CSystem.Func%7B%60%600%2C%60%601%7D%29", "MyNamespace.MyGenericType.Select{T1,T2}(MyNamespace.MyGenericType{T1},System.Func{T1,T2})")]
        public void ParsesGenerics(string apiReference, string expected)
        {
            var reference = new DocsApiReference(apiReference);
            Assert.Equal(expected, reference.Api);
        }

        [Theory]
        [InlineData("System.Int32", false)]
        [InlineData("T:System.Int32", false)]
        [InlineData("M:System.Int32.ToString()", false)]
        [InlineData("O:System.Int32.ToString(System.String)", true)]
        public void IdentifiesOverloadReferences(string apiReference, bool expected)
        {
            var reference = new DocsApiReference(apiReference);
            Assert.Equal(expected, reference.IsOverload);
        }

        [Theory]
        [InlineData(@"Accessibility: <xref:Accessibility>", @"Accessibility: <see cref=""Accessibility"" />")]
        [InlineData(@"SyndicationCategory: <xref:System.ServiceModel.Syndication.SyndicationCategory>", @"SyndicationCategory: <see cref=""System.ServiceModel.Syndication.SyndicationCategory"" />")]
        [InlineData(@"Label: <xref:System.ServiceModel.Syndication.SyndicationCategory.Label*>", @"Label: <see cref=""System.ServiceModel.Syndication.SyndicationCategory.Label"" />")]
        [InlineData(@"==: <xref:System.Windows.Media.Matrix.op_Equality*?displayProperty=nameWithType>", @"==: <see cref=""System.Windows.Media.Matrix.op_Equality"" />")]
        public void ReplacesMarkdownXrefsWithSeeCrefs(string markdown, string expected)
        {
            var replaced = DocsApiReference.ReplaceMarkdownXrefWithSeeCref(markdown);
            Assert.Equal(expected, replaced);
        }
    }
}
