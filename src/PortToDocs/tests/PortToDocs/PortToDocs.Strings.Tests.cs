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

        [Fact]
        public void See_Cref()
        {
            // References to other APIs, using <see cref="DocId"/> in intellisense xml, need to be transformed to <see cref="X:DocId"/> in docs xml (notice the prefix), or <xref:DocId> in markdown.

            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyType"">
      <summary>See <see cref=""T:MyNamespace.MyType""/>.</summary>
    </member>
    <member name=""M:MyNamespace.MyType.MyMethod"">
      <summary>The summary of MyMethod. See <see cref=""M:MyNamespace.MyType.MyMethod"" />.</summary>
      <remarks>See <see cref=""M:MyNamespace.MyType.MyMethod"" />.</remarks>
    </member>
  </members>
</doc>";

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
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>See <see cref=""T:MyNamespace.MyType"" />.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <summary>The summary of MyMethod. See <see cref=""M:MyNamespace.MyType.MyMethod"" />.</summary>
        <remarks>
          <format type=""text/markdown""><![CDATA[

## Remarks

See <xref:MyNamespace.MyType.MyMethod>.

          ]]></format>
        </remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            TestWithStrings(originalIntellisense, originalDocs, expectedDocs);
        }

        [Fact]
        public void See_Cref_Primitive()
        {
            // Need to make sure that see crefs pointing to primitives are also converted properly.

            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyType"">
      <summary>Type summary.</summary>
    </member>
    <member name=""M:MyNamespace.MyType.MyMethod"">
      <summary>Summary: <see cref=""bool""/>, <see cref=""byte""/>, <see cref=""sbyte""/>, <see cref=""char""/>, <see cref=""decimal""/>, <see cref=""double""/>, <see cref=""float""/>, <see cref=""int""/>, <see cref=""uint""/>, <see cref=""nint""/>, <see cref=""nuint""/>, <see cref=""long""/>, <see cref=""ulong""/>, <see cref=""short""/>, <see cref=""ushort""/>, <see cref=""object""/>, <see cref=""dynamic""/>, <see cref=""string""/>.</summary>
      <remarks>Remarks: <see cref=""bool""/>, <see cref=""byte""/>, <see cref=""sbyte""/>, <see cref=""char""/>, <see cref=""decimal""/>, <see cref=""double""/>, <see cref=""float""/>, <see cref=""int""/>, <see cref=""uint""/>, <see cref=""nint""/>, <see cref=""nuint""/>, <see cref=""long""/>, <see cref=""ulong""/>, <see cref=""short""/>, <see cref=""ushort""/>, <see cref=""object""/>, <see cref=""dynamic""/>, <see cref=""string""/>.</remarks>
    </member>
  </members>
</doc>";

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
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            // Notice that `dynamic` is converted to langword, to prevent converting it to System.Object.
            string expectedDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>Type summary.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <summary>Summary: <see cref=""T:System.Boolean"" />, <see cref=""T:System.Byte"" />, <see cref=""T:System.SByte"" />, <see cref=""T:System.Char"" />, <see cref=""T:System.Decimal"" />, <see cref=""T:System.Double"" />, <see cref=""T:System.Single"" />, <see cref=""T:System.Int32"" />, <see cref=""T:System.UInt32"" />, <see cref=""T:System.IntPtr"" />, <see cref=""T:System.UIntPtr"" />, <see cref=""T:System.Int64"" />, <see cref=""T:System.UInt64"" />, <see cref=""T:System.Int16"" />, <see cref=""T:System.UInt16"" />, <see cref=""T:System.Object"" />, <see langword=""dynamic"" />, <see cref=""T:System.String"" />.</summary>
        <remarks>
          <format type=""text/markdown""><![CDATA[

## Remarks

Remarks: `bool`, `byte`, `sbyte`, `char`, `decimal`, `double`, `float`, `int`, `uint`, `nint`, `nuint`, `long`, `ulong`, `short`, `ushort`, `object`, `dynamic`, `string`.

          ]]></format>
        </remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            TestWithStrings(originalIntellisense, originalDocs, expectedDocs);
        }

        [Fact]
        public void See_Cref_Ctor_Remark()
        {
            // References to constructors, which look like
            // <see cref="M:Foo.Bar.#ctor"/> or <see cref="M:Foo.Bar.#ctor(System.Type)"/> in intellisense xml,
            // need to be transformed to <xref:Foo.Bar.%23ctor> or <xref:Foo.Bar.%23ctor(System.Type)> in markdown.

            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyType"">
      <summary>They type summary.</summary>
    </member>
    <member name=""M:MyNamespace.MyType.#ctor"">
      <summary>Summary of parameterless constructor.</summary>
      <remarks>A link to itself: <see cref=""M:MyNamespace.MyType.#ctor""/>.</remarks>
    </member>
    <member name=""M:MyNamespace.MyType.#ctor(System.Object)"">
      <param name=""myParam"">Parameter summary.</param>
      <summary>Summary of constructor with parameter.</summary>
      <remarks>A link to itself: <see cref=""M:MyNamespace.MyType.#ctor(System.Object)""/>.</remarks>
    </member>
  </members>
</doc>";

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
    <Member MemberName="".ctor"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.#ctor"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <Docs>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
    <Member MemberName="".ctor"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.#ctor(System.Object)"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <Docs>
        <param name=""myParam"">To be added.</param>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>They type summary.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName="".ctor"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.#ctor"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <Docs>
        <summary>Summary of parameterless constructor.</summary>
        <remarks>
          <format type=""text/markdown""><![CDATA[

## Remarks

A link to itself: <xref:MyNamespace.MyType.%23ctor>.

          ]]></format>
        </remarks>
      </Docs>
    </Member>
    <Member MemberName="".ctor"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.#ctor(System.Object)"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <Docs>
        <param name=""myParam"">Parameter summary.</param>
        <summary>Summary of constructor with parameter.</summary>
        <remarks>
          <format type=""text/markdown""><![CDATA[

## Remarks

A link to itself: <xref:MyNamespace.MyType.%23ctor(System.Object)>.

          ]]></format>
        </remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            TestWithStrings(originalIntellisense, originalDocs, expectedDocs);
        }

        [Fact]
        public void See_Cref_Generic()
        {
            // References to other APIs in remarks, should be converted to xref in markdown. Make sure generic APIs get converted properly. 

            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyGenericType`1"">
      <typeparam name=""T"">I am the type of MyGenericType.</typeparam>
      <summary>I have a funny suffix in my full name.</summary>
    </member>
    <member name=""M:MyNamespace.MyGenericType`1.MyMethod``2(System.Object)"">
      <typeparam name=""T"">The type T of the method.</typeparam>
      <typeparam name=""U"">The type U of the method.</typeparam>
      <summary>I have a reference to the generic type <see cref=""T:MyNamespace.MyGenericType`1""/> and to myself <see cref=""M:MyNamespace.MyGenericType`1.MyMethod``2(System.Object)""/>.</summary>
      <remarks>I have a reference to the generic type <see cref=""T:MyNamespace.MyGenericType`1""/> and to myself <see cref=""M:MyNamespace.MyGenericType`1.MyMethod``2(System.Object)""/>.</remarks>
    </member>
  </members>
</doc>";

            string originalDocs = @"<Type Name=""MyGenericType&lt;T&gt;"" FullName=""MyNamespace.MyGenericType&lt;T&gt;"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyGenericType`1"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <typeparam name=""T"">To be added.</typeparam>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod&lt;T,U&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyGenericType`1.MyMethod``2(System.Object)"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <typeparam name=""T"">To be added.</typeparam>
        <typeparam name=""U"">To be added.</typeparam>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedDocs = @"<Type Name=""MyGenericType&lt;T&gt;"" FullName=""MyNamespace.MyGenericType&lt;T&gt;"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyGenericType`1"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <typeparam name=""T"">I am the type of MyGenericType.</typeparam>
    <summary>I have a funny suffix in my full name.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod&lt;T,U&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyGenericType`1.MyMethod``2(System.Object)"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <typeparam name=""T"">The type T of the method.</typeparam>
        <typeparam name=""U"">The type U of the method.</typeparam>
        <summary>I have a reference to the generic type <see cref=""T:MyNamespace.MyGenericType`1"" /> and to myself <see cref=""M:MyNamespace.MyGenericType`1.MyMethod``2(System.Object)"" />.</summary>
        <remarks>
          <format type=""text/markdown""><![CDATA[

## Remarks

I have a reference to the generic type <xref:MyNamespace.MyGenericType%601> and to myself <xref:MyNamespace.MyGenericType%601.MyMethod%60%602(System.Object)>.

          ]]></format>
        </remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            TestWithStrings(originalIntellisense, originalDocs, expectedDocs);
        }

        [Fact]
        public void SeeAlso_Cref()
        {
            // Normally, references to other APIs are indicated with <see cref="X:DocId"/> in xml, or with <xref:DocId> in markdown. But there are some rare cases where <seealso cref="X:DocId"/> is used, and we need to make sure to handle them just as see crefs.

            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyType"">
      <summary>See <seealso cref=""T:MyNamespace.MyType""/>.</summary>
    </member>
    <member name=""M:MyNamespace.MyType.MyMethod"">
      <summary>The summary of MyMethod. See <seealso cref=""M:MyNamespace.MyType.MyMethod"" />.</summary>
      <remarks>See <seealso cref=""M:MyNamespace.MyType.MyMethod"" />.</remarks>
    </member>
  </members>
</doc>";

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
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>See <seealso cref=""T:MyNamespace.MyType"" />.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <summary>The summary of MyMethod. See <seealso cref=""M:MyNamespace.MyType.MyMethod"" />.</summary>
        <remarks>
          <format type=""text/markdown""><![CDATA[

## Remarks

See <xref:MyNamespace.MyType.MyMethod>.

          ]]></format>
        </remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            TestWithStrings(originalIntellisense, originalDocs, expectedDocs);
        }

        [Fact]
        public void See_Langword()
        {
            // Reserved words are indicated with <see langword="word" />. They need to be copied as <see langword="word" /> in xml, or transformed to `word` in markdown.

            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyType"">
      <summary>Langword <see langword=""null""/>.</summary>
    </member>
    <member name=""M:MyNamespace.MyType.MyMethod"">
      <summary>The summary of MyMethod. Langword <see langword=""false""/>.</summary>
      <remarks>Langword <see langword=""true""/>.</remarks>
    </member>
  </members>
</doc>";

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
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>Langword <see langword=""null"" />.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Docs>
        <summary>The summary of MyMethod. Langword <see langword=""false"" />.</summary>
        <remarks>
          <format type=""text/markdown""><![CDATA[

## Remarks

Langword `true`.

          ]]></format>
        </remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            TestWithStrings(originalIntellisense, originalDocs, expectedDocs);
        }

        [Fact]
        public void ParamRefName()
        {
            // Parameter references are indicated with <paramref name="paramName" />. They need to be copied as <paramref name="paramName" /> in xml, or transformed to `paramName` in markdown.

            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyType"">
      <summary>Type summary.</summary>
    </member>
    <member name=""M:MyNamespace.MyType.MyMethod(System.String)"">
      <summary>The summary of MyMethod. Paramref <paramref name=""myParam""/>.</summary>
      <param name=""myParam"">My parameter description.</param>
      <remarks>Paramref <paramref name=""myParam""/>.</remarks>
    </member>
  </members>
</doc>";

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
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod(System.String)"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Parameters>
        <Parameter Name=""myParam"" Type=""System.String"" />
      </Parameters>
      <Docs>
        <param name=""myParam"">To be added.</param>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedDocs = @"<Type Name=""MyType"" FullName=""MyNamespace.MyType"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>Type summary.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod(System.String)"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <Parameters>
        <Parameter Name=""myParam"" Type=""System.String"" />
      </Parameters>
      <Docs>
        <param name=""myParam"">My parameter description.</param>
        <summary>The summary of MyMethod. Paramref <paramref name=""myParam"" />.</summary>
        <remarks>
          <format type=""text/markdown""><![CDATA[

## Remarks

Paramref `myParam`.

          ]]></format>
        </remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            TestWithStrings(originalIntellisense, originalDocs, expectedDocs);
        }

        [Fact]
        public void TypeParamRefName()
        {
            // TypeParameter references are indicated with <typeparamref name="typeParamName" />. They need to be copied as <typeparamref name="typeParamName" /> in xml, or transformed to `typeParamName` in markdown.

            string originalIntellisense = @"<?xml version=""1.0""?>
<doc>
  <assembly>
    <name>MyAssembly</name>
  </assembly>
  <members>
    <member name=""T:MyNamespace.MyType`1"">
      <typeparam name=""T"">Description of the typeparam of the type.</typeparam>
      <summary>Type summary. Typeparamref <typeparamref name=""T""/>.</summary>
    </member>
    <member name=""M:MyNamespace.MyType.MyMethod``1"">
      <summary>The summary of MyMethod. Typeparamref <typeparamref name=""T""/>.</summary>
      <typeparam name=""T"">Description of the typeparam of the method.</typeparam>
      <remarks>Typeparamref <typeparamref name=""T""/>.</remarks>
    </member>
  </members>
</doc>";

            string originalDocs = @"<Type Name=""MyType&lt;T&gt;"" FullName=""MyNamespace.MyType&lt;T&gt;"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType`1"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <TypeParameters>
    <TypeParameter Name=""T"">
      <Constraints>
        <BaseTypeName>System.ValueType</BaseTypeName>
      </Constraints>
    </TypeParameter>
  </TypeParameters>
  <Docs>
    <typeparam name=""T"">To be added.</typeparam>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod&lt;T&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod``1"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <TypeParameters>
        <TypeParameter Name=""T"">
          <Constraints>
            <BaseTypeName>System.ValueType</BaseTypeName>
          </Constraints>
        </TypeParameter>
      </TypeParameters>
      <Docs>
        <typeparam name=""T"">To be added.</typeparam>
        <summary>To be added.</summary>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

            string expectedDocs = @"<Type Name=""MyType&lt;T&gt;"" FullName=""MyNamespace.MyType&lt;T&gt;"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyType`1"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <TypeParameters>
    <TypeParameter Name=""T"">
      <Constraints>
        <BaseTypeName>System.ValueType</BaseTypeName>
      </Constraints>
    </TypeParameter>
  </TypeParameters>
  <Docs>
    <typeparam name=""T"">Description of the typeparam of the type.</typeparam>
    <summary>Type summary. Typeparamref <typeparamref name=""T"" />.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyMethod&lt;T&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyType.MyMethod``1"" />
      <MemberType>Method</MemberType>
      <AssemblyInfo>
        <AssemblyName>MyAssembly</AssemblyName>
      </AssemblyInfo>
      <ReturnValue>
        <ReturnType>System.Void</ReturnType>
      </ReturnValue>
      <TypeParameters>
        <TypeParameter Name=""T"">
          <Constraints>
            <BaseTypeName>System.ValueType</BaseTypeName>
          </Constraints>
        </TypeParameter>
      </TypeParameters>
      <Docs>
        <typeparam name=""T"">Description of the typeparam of the method.</typeparam>
        <summary>The summary of MyMethod. Typeparamref <typeparamref name=""T"" />.</summary>
        <remarks>
          <format type=""text/markdown""><![CDATA[

## Remarks

Typeparamref `T`.

          ]]></format>
        </remarks>
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
