// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ApiDocsSync.Libraries.Tests
{
    public class PortToDocs_Strings_Tests : BasePortTests
    {
        public PortToDocs_Strings_Tests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TypeParam_MismatchedNames_DoNotPort()
        {
            // The only way a typeparam is getting ported is if the name is exactly the same in the TypeParameter section as in the intellisense xml.
            // Or, if the tool is invoked with DisabledPrompts=true.

            // TypeParam name = TRenamedValue
            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyType"">
      <summary>This is the type summary.</summary>
    </member>
    <member name=""M:MyNamespace.MyType.MyTypeParamMethod``1"">
      <typeparam name=""TRenamedValue"">The renamed typeparam of MyTypeParamMethod.</typeparam>
      <summary>The summary of MyTypeParamMethod.</summary>
    </member>
  </members>
</doc>";

            // TypeParam name = TValue
            string originalDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyTypeParamMethod&lt;TValue&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyTypeParamMethod``1"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <TypeParameters>
        <TypeParameter Name=""TValue"" />
      </TypeParameters>
      <Docs>
        <typeparam name=""TValue"">To be added.</typeparam>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            // TypeParam summary not ported
            string expectedDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>This is the type summary.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyTypeParamMethod&lt;TValue&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyTypeParamMethod``1"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <TypeParameters>
        <TypeParameter Name=""TValue"" />
      </TypeParameters>
      <Docs>
        <typeparam name=""TValue"">To be added.</typeparam>
        <summary>The summary of MyTypeParamMethod.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            TestWithStrings(originalIntellisense, originalDocs, expectedDocs);
        }

        private static void TestWithStrings(string originalIntellisense, string originalDocs, string expectedDocs)
        {
            Configuration configuration = new Configuration();
            configuration.IncludedAssemblies.Add(TestData.TestAssembly);
            var porter = new ToDocsPorter(configuration);

            XDocument xIntellisense = XDocument.Parse(originalIntellisense);
            porter.LoadIntellisenseXmlFile(xIntellisense, "IntelliSense.xml");

            XDocument xDocs = XDocument.Parse(originalDocs);
            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            porter.LoadDocsFile(xDocs, "Docs.xml", encoding: utf8NoBom);

            porter.Start();

            string actualDocs = xDocs.ToString();
            Assert.Equal(expectedDocs, actualDocs);
        }
    }
}
