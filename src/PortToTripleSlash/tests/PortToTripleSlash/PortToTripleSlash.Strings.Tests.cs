// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using ApiDocsSync.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ApiDocsSync.PortToTripleSlash.Tests;

public class PortToTripleSlash_Strings_Tests : BasePortTests
{
    private const string DefaultAssembly = "MyAssembly";

    public PortToTripleSlash_Strings_Tests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Class_TypeDescription(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>This is the MyClass summary.</summary>
    <remarks>These are the MyClass remarks.</remarks>
  </Docs>
  <Members>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
}";

        string expectedCode = $@"namespace MyNamespace;
/// <summary>This is the MyClass summary.</summary>" +
GetRemarks(skipRemarks, "MyClass") +
@"public class MyClass
{
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Struct_TypeDescription(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyStruct";

        string docFile = @"<Type Name=""MyStruct"" FullName=""MyNamespace.MyStruct"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyStruct"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>This is the MyStruct summary.</summary>
    <remarks>These are the MyStruct remarks.</remarks>
  </Docs>
  <Members>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public struct MyStruct
{
}";

        string expectedCode = @"namespace MyNamespace;
/// <summary>This is the MyStruct summary.</summary>" +
GetRemarks(skipRemarks, "MyStruct") +
@"public struct MyStruct
{
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Interface_TypeDescription(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyInterface";

        string docFile = @"<Type Name=""MyInterface"" FullName=""MyNamespace.MyInterface"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyInterface"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>This is the MyInterface summary.</summary>
    <remarks>These are the MyInterface remarks.</remarks>
  </Docs>
  <Members>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public interface MyInterface
{
}";

        string expectedCode = @"namespace MyNamespace;
/// <summary>This is the MyInterface summary.</summary>" +
GetRemarks(skipRemarks, "MyInterface") +
@"public interface MyInterface
{
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Enum_TypeDescription(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyEnum";

        string docFile = @"<Type Name=""MyEnum"" FullName=""MyNamespace.MyEnum"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyEnum"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>This is the MyEnum summary.</summary>
    <remarks>These are the MyEnum remarks.</remarks>
  </Docs>
  <Members>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public enum MyEnum
{
}";

        string expectedCode = @"namespace MyNamespace;
/// <summary>This is the MyEnum summary.</summary>" +
GetRemarks(skipRemarks, "MyEnum") +
@"public enum MyEnum
{
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Class_Ctor_Parameterless(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName="".ctor"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.#ctor"" />
      <Docs>
        <summary>This is the MyClass constructor summary.</summary>
        <remarks>These are the MyClass constructor remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public MyClass() { }
}";

        string expectedCode = @"namespace MyNamespace;
public class MyClass
{
    /// <summary>This is the MyClass constructor summary.</summary>" +
GetRemarks(skipRemarks, "MyClass constructor", "    ") +
@"    public MyClass() { }
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Class_Ctor_IntParameter(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName="".ctor"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.#ctor(System.Int32)"" />
      <Docs>
        <summary>This is the MyClass constructor summary.</summary>
        <param name=""intParam"">This is the MyClass constructor parameter description.</param>
        <remarks>These are the MyClass constructor remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public MyClass(int intParam) { }
}";

        string expectedCode = @"namespace MyNamespace;
public class MyClass
{
    /// <summary>This is the MyClass constructor summary.</summary>
    /// <param name=""intParam"">This is the MyClass constructor parameter description.</param>" +
GetRemarks(skipRemarks, "MyClass constructor", "    ") +
@"    public MyClass(int intParam) { }
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Class_Method_Parameterless_VoidReturn(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyVoidMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.MyVoidMethod"" />
      <Docs>
        <summary>This is the MyVoidMethod summary.</summary>
        <remarks>These are the MyVoidMethod remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public void MyVoidMethod() { }
}";

        string expectedCode = @"namespace MyNamespace;
public class MyClass
{
    /// <summary>This is the MyVoidMethod summary.</summary>" +
GetRemarks(skipRemarks, "MyVoidMethod", "    ") +
@"    public void MyVoidMethod() { }
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Class_Method_IntParameter_IntReturn(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyIntMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.MyIntMethod(System.Int32)"" />
      <Docs>
        <param name=""withArgument"">This is the MyIntMethod withArgument description.</param>
        <summary>This is the MyIntMethod summary.</summary>
        <returns>This is the MyIntMethod returns description.</returns>
        <remarks>These are the MyIntMethod remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public int MyIntMethod(int withArgument) => withArgument;
}";

        string expectedCode = @"namespace MyNamespace;
public class MyClass
{
    /// <summary>This is the MyIntMethod summary.</summary>
    /// <param name=""withArgument"">This is the MyIntMethod withArgument description.</param>
    /// <returns>This is the MyIntMethod returns description.</returns>" +
GetRemarks(skipRemarks, "MyIntMethod", "    ") +
@"    public int MyIntMethod(int withArgument) => withArgument;
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Class_GenericMethod_Parameterless_VoidReturn(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyGenericMethod&lt;T&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.MyGenericMethod``1"" />
      <Docs>
        <typeparam name=""T"">This is the MyGenericMethod type parameter description.</typeparam>
        <summary>This is the MyGenericMethod summary.</summary>
        <remarks>These are the MyGenericMethod remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public void MyGenericMethod<T>() { }
}";

        string expectedCode = @"namespace MyNamespace;
public class MyClass
{
    /// <summary>This is the MyGenericMethod summary.</summary>
    /// <typeparam name=""T"">This is the MyGenericMethod type parameter description.</typeparam>" +
GetRemarks(skipRemarks, "MyGenericMethod", "    ") +
@"    public void MyGenericMethod<T>() { }
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Class_GenericMethod_IntParameter_VoidReturn(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyGenericMethod&lt;T&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.MyGenericMethod``1(System.Int32)"" />
      <Docs>
        <typeparam name=""T"">This is the MyGenericMethod type parameter description.</typeparam>
        <param name=""intParam"">This is the MyGenericMethod parameter description.</param>
        <summary>This is the MyGenericMethod summary.</summary>
        <remarks>These are the MyGenericMethod remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public void MyGenericMethod<T>(int intParam) { }
}";

        string expectedCode = @"namespace MyNamespace;
public class MyClass
{
    /// <summary>This is the MyGenericMethod summary.</summary>
    /// <typeparam name=""T"">This is the MyGenericMethod type parameter description.</typeparam>
    /// <param name=""intParam"">This is the MyGenericMethod parameter description.</param>" +
GetRemarks(skipRemarks, "MyGenericMethod", "    ") +
@"    public void MyGenericMethod<T>(int intParam) { }
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Class_GenericMethod_GenericParameter_GenericReturn(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyGenericMethod&lt;T&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.MyGenericMethod``1(``0)"" />
      <Docs>
        <typeparam name=""T"">This is the MyGenericMethod type parameter description.</typeparam>
        <param name=""withGenericArgument"">This is the MyGenericMethod withGenericArgument description.</param>
        <summary>This is the MyGenericMethod summary.</summary>
        <returns>This is the MyGenericMethod returns description.</returns>
        <remarks>These are the MyGenericMethod remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public T MyGenericMethod<T>(T withGenericArgument) => withGenericArgument;
}";

        string expectedCode = @"namespace MyNamespace;
public class MyClass
{
    /// <summary>This is the MyGenericMethod summary.</summary>
    /// <typeparam name=""T"">This is the MyGenericMethod type parameter description.</typeparam>
    /// <param name=""withGenericArgument"">This is the MyGenericMethod withGenericArgument description.</param>
    /// <returns>This is the MyGenericMethod returns description.</returns>" +
GetRemarks(skipRemarks, "MyGenericMethod", "    ") +
@"    public T MyGenericMethod<T>(T withGenericArgument) => withGenericArgument;
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Class_Method_Exception(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyVoidMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.MyVoidMethod"" />
      <Docs>
        <summary>This is the MyVoidMethod summary.</summary>
        <remarks>These are the MyVoidMethod remarks.</remarks>
        <exception cref=""T:System.NullReferenceException"">The null reference exception thrown by MyVoidMethod.</exception>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public void MyVoidMethod() { }
}";

        string expectedCode = @"namespace MyNamespace;
public class MyClass
{
    /// <summary>This is the MyVoidMethod summary.</summary>
    /// <exception cref=""System.NullReferenceException"">The null reference exception thrown by MyVoidMethod.</exception>" +
GetRemarks(skipRemarks, "MyVoidMethod", "    ") +
@"    public void MyVoidMethod() { }
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Class_Field(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyField"">
      <MemberSignature Language=""DocId"" Value=""F:MyNamespace.MyClass.MyField"" />
      <Docs>
        <summary>This is the MyField summary.</summary>
        <remarks>These are the MyField remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public double MyField;
}";

        string expectedCode = @"namespace MyNamespace;
public class MyClass
{
    /// <summary>This is the MyField summary.</summary>" +
GetRemarks(skipRemarks, "MyField", "    ") +
@"    public double MyField;
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Class_PropertyWithSetter(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MySetProperty"">
      <MemberSignature Language=""DocId"" Value=""P:MyNamespace.MyClass.MySetProperty"" />
      <MemberType>Property</MemberType>
      <Docs>
        <summary>This is the MySetProperty summary.</summary>
        <value>This is the MySetProperty value.</value>
        <remarks>These are the MySetProperty remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public double MySetProperty { set; }
}";

        string expectedCode = @"namespace MyNamespace;
public class MyClass
{
    /// <summary>This is the MySetProperty summary.</summary>
    /// <value>This is the MySetProperty value.</value>" +
GetRemarks(skipRemarks, "MySetProperty", "    ") +
@"    public double MySetProperty { set; }
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Class_PropertyWithGetter(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyGetProperty"">
      <MemberSignature Language=""DocId"" Value=""P:MyNamespace.MyClass.MyGetProperty"" />
      <MemberType>Property</MemberType>
      <Docs>
        <summary>This is the MyGetProperty summary.</summary>
        <value>This is the MyGetProperty value.</value>
        <remarks>These are the MyGetProperty remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public double MyGetProperty { get; }
}";

        string expectedCode = @"namespace MyNamespace;
public class MyClass
{
    /// <summary>This is the MyGetProperty summary.</summary>
    /// <value>This is the MyGetProperty value.</value>" +
GetRemarks(skipRemarks, "MyGetProperty", "    ") +
@"    public double MyGetProperty { get; }
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Class_PropertyWithGetterAndSetter(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyGetSetProperty"">
      <MemberSignature Language=""DocId"" Value=""P:MyNamespace.MyClass.MyGetSetProperty"" />
      <MemberType>Property</MemberType>
      <Docs>
        <summary>This is the MyGetSetProperty summary.</summary>
        <value>This is the MyGetSetProperty value.</value>
        <remarks>These are the MyGetSetProperty remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public double MyGetSetProperty { get; set; }
}";

        string expectedCode = @"namespace MyNamespace;
public class MyClass
{
    /// <summary>This is the MyGetSetProperty summary.</summary>
    /// <value>This is the MyGetSetProperty value.</value>" +
GetRemarks(skipRemarks, "MyGetSetProperty", "    ") +
@"    public double MyGetSetProperty { get; set; }
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Class_Property_Exception(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyGetSetProperty"">
      <MemberSignature Language=""DocId"" Value=""P:MyNamespace.MyClass.MyGetSetProperty"" />
      <MemberType>Property</MemberType>
      <Docs>
        <summary>This is the MyGetSetProperty summary.</summary>
        <value>This is the MyGetSetProperty value.</value>
        <remarks>These are the MyGetSetProperty remarks.</remarks>
        <exception cref=""T:System.NullReferenceException"">The null reference exception thrown by MyGetSetProperty.</exception>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public double MyGetSetProperty { get; set; }
}";

        string expectedCode = @"namespace MyNamespace;
public class MyClass
{
    /// <summary>This is the MyGetSetProperty summary.</summary>
    /// <value>This is the MyGetSetProperty value.</value>
    /// <exception cref=""System.NullReferenceException"">The null reference exception thrown by MyGetSetProperty.</exception>" +
GetRemarks(skipRemarks, "MyGetSetProperty", "    ") +
@"    public double MyGetSetProperty { get; set; }
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Class_Event(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyEvent"">
      <MemberSignature Language=""DocId"" Value=""E:MyNamespace.MyClass.MyEvent"" />
      <Docs>
        <summary>This is the MyEvent summary.</summary>
        <remarks>These are the MyEvent remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public event MyDelegate MyEvent;
}";

        string expectedCode = @"namespace MyNamespace;
public class MyClass
{
    /// <summary>This is the MyEvent summary.</summary>" +
GetRemarks(skipRemarks, "MyEvent", "    ") +
@"    public event MyDelegate MyEvent;
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Class_WithDelegate(bool skipRemarks)
    {
        string topLevelTypeDocId = "T:MyNamespace.MyClass";
        string delegateDocId = "T:MyNamespace.MyClass.MyDelegate";

        string docFile1 = @"<Type Name=""MyDelegate"" FullName=""MyNamespace.MyClass.MyDelegate"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass.MyDelegate"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <param name=""sender"">This is the MyDelegate sender description.</param>
    <summary>This is the MyDelegate summary.</summary>
    <remarks>These are the MyDelegate remarks.</remarks>
  </Docs>
</Type>
";

        string docFile2 = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public delegate void MyDelegate(object sender);
}";

        string expectedCode = @"namespace MyNamespace;
public class MyClass
{
    /// <summary>This is the MyDelegate summary.</summary>
    /// <param name=""sender"">This is the MyDelegate sender description.</param>" +
GetRemarks(skipRemarks, "MyDelegate", "    ") +
@"    public delegate void MyDelegate(object sender);
}";

        List<string> docFiles = new() { docFile1, docFile2 };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { topLevelTypeDocId, expectedCode }, { delegateDocId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task NestedEnum_InClass(bool skipRemarks)
    {
        string topLevelTypeDocId = "T:MyNamespace.MyClass";
        string enumDocId = "T:MyNamespace.MyClass.MyEnum";

        string docFile1 = @"<Type Name=""MyEnum"" FullName=""MyNamespace.MyClass.MyEnum"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass.MyEnum"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>This is the MyEnum summary.</summary>
    <remarks>These are the MyEnum remarks.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""Value1"">
      <MemberSignature Language=""DocId"" Value=""F:MyNamespace.MyClass.MyEnum.Value1"" />
      <Docs>
        <summary>This is the MyEnum.Value1 summary.</summary>
      </Docs>
    </Member>
    <Member MemberName=""Value2"">
      <MemberSignature Language=""DocId"" Value=""F:MyNamespace.MyClass.MyEnum.Value2"" />
      <Docs>
        <summary>This is the MyEnum.Value2 summary.</summary>
      </Docs>
    </Member>
  </Members>
</Type>";

        string docFile2 = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>This is the MyClass summary.</summary>
    <remarks>These are the MyClass remarks.</remarks>
  </Docs>
  <Members>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public enum MyEnum
    {
        Value1,
        Value2
    }
}";

        string expectedCode = @"namespace MyNamespace;
/// <summary>This is the MyClass summary.</summary>" +
GetRemarks(skipRemarks, "MyClass") +
@"public class MyClass
{
    /// <summary>This is the MyEnum summary.</summary>" +
GetRemarks(skipRemarks, "MyEnum", "    ") +
@"    public enum MyEnum
    {
        /// <summary>This is the MyEnum.Value1 summary.</summary>
        Value1,
        /// <summary>This is the MyEnum.Value2 summary.</summary>
        Value2
    }
}";

        List<string> docFiles = new() { docFile1, docFile2 };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { topLevelTypeDocId, expectedCode }, { enumDocId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task NestedStruct_InClass(bool skipRemarks)
    {
        string topLevelTypeDocId = "T:MyNamespace.MyClass";
        string enumDocId = "T:MyNamespace.MyClass.MyStruct";

        string docFile1 = @"<Type Name=""MyStruct"" FullName=""MyNamespace.MyClass.MyStruct"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass.MyStruct"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>This is the MyStruct summary.</summary>
    <remarks>These are the MyStruct remarks.</remarks>
  </Docs>
  <Members>
  </Members>
</Type>";

        string docFile2 = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>This is the MyClass summary.</summary>
    <remarks>These are the MyClass remarks.</remarks>
  </Docs>
  <Members>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public struct MyStruct
    {
    }
}";

        string expectedCode = @"namespace MyNamespace;
/// <summary>This is the MyClass summary.</summary>" +
GetRemarks(skipRemarks, "MyClass") +
@"public class MyClass
{
    /// <summary>This is the MyStruct summary.</summary>" +
GetRemarks(skipRemarks, "MyStruct", "    ") +
@"    public struct MyStruct
    {
    }
}";

        List<string> docFiles = new() { docFile1, docFile2 };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { topLevelTypeDocId, expectedCode }, { enumDocId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Class_Operator(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>To be added.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""op_Addition"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.op_Addition(MyNamespace.MyClass,MyNamespace.MyClass)"" />
      <Docs>
        <param name=""value1"">This is the + operator value1 description.</param>
        <param name=""value2"">This is the + operator value2 description.</param>
        <summary>This is the + operator summary.</summary>
        <returns>This is the + operator returns description.</returns>
        <remarks>These are the + operator remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public static MyClass operator +(MyClass value1, MyClass value2) => value1;
}";

        string expectedCode = @"namespace MyNamespace;
public class MyClass
{
    /// <summary>This is the + operator summary.</summary>
    /// <param name=""value1"">This is the + operator value1 description.</param>
    /// <param name=""value2"">This is the + operator value2 description.</param>
    /// <returns>This is the + operator returns description.</returns>" +
GetRemarks(skipRemarks, "+ operator", "    ") +
@"    public static MyClass operator +(MyClass value1, MyClass value2) => value1;
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Class_Do_Not_Backport_Inherited_Docs(bool skipRemarks)
    {
        // In PortToDocs we find the base class and get the documentation if there's none in the child type.
        // In PortToTripleSlash, we should not do that. We only backport what's found in the child type.

        string interfaceDocId = "T:MyNamespace.MyInterface";

        string classDocId = "T:MyNamespace.MyClass";

        string interfaceDocFile = @"<Type Name=""MyInterface"" FullName=""MyNamespace.MyInterface"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyInterface"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>This is the MyInterface summary.</summary>
    <remarks>These are the MyInterface remarks.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyVoidMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyInterface.MyVoidMethod"" />
      <Docs>
        <summary>This is the MyInterface.MyVoidMethod summary.</summary>
        <remarks>These are the MyInterface.MyVoidMethod remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyGetSetProperty"">
      <MemberSignature Language=""DocId"" Value=""P:MyNamespace.MyInterface.MyGetSetProperty"" />
      <MemberType>Property</MemberType>
      <Docs>
        <summary>This is the MyInterface.MyGetSetProperty summary.</summary>
        <value>This is the MyInterface.MyGetSetProperty value.</value>
        <remarks>These are the MyInterface.MyGetSetProperty remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string classDocFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Base>
    <BaseTypeName>MyNamespace.MyInterface</BaseTypeName>
  </Base>
  <Docs>
    <summary>This is the MyClass summary.</summary>
    <remarks>These are the MyClass remarks.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyVoidMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.MyVoidMethod"" />
      <Docs>
        <summary>To be added.</summary>
        <remarks>These are the MyClass.MyVoidMethod remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyGetSetProperty"">
      <MemberSignature Language=""DocId"" Value=""P:MyNamespace.MyClass.MyGetSetProperty"" />
      <MemberType>Property</MemberType>
      <Docs>
        <summary>To be added.</summary>
        <value>This is the MyClass.MyGetSetProperty value.</value>
        <remarks>To be added.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string interfaceOriginalCode = @"namespace MyNamespace;
public interface MyInterface
{
    public void MyVoidMethod();
    public double MyGetSetProperty { get; set; }
}";

        string interfaceExpectedCode = @"namespace MyNamespace;
/// <summary>This is the MyInterface summary.</summary>" +
GetRemarks(skipRemarks, "MyInterface") +
@"public interface MyInterface
{
    /// <summary>This is the MyInterface.MyVoidMethod summary.</summary>" +
GetRemarks(skipRemarks, "MyInterface.MyVoidMethod", "    ") +
@"    public void MyVoidMethod();
    /// <summary>This is the MyInterface.MyGetSetProperty summary.</summary>
    /// <value>This is the MyInterface.MyGetSetProperty value.</value>" +
GetRemarks(skipRemarks, "MyInterface.MyGetSetProperty", "    ") +
@"    public double MyGetSetProperty { get; set; }
}";

        string classOriginalCode = @"namespace MyNamespace;
public class MyClass : MyInterface
{
    public void MyVoidMethod() { }
    public double MyGetSetProperty { get; set; }
}";

        string classExpectedCode = @"namespace MyNamespace;
/// <summary>This is the MyClass summary.</summary>" +
GetRemarks(skipRemarks, "MyClass") +
@"public class MyClass : MyInterface
{" +
GetRemarks(skipRemarks, "MyClass.MyVoidMethod", "    ") +
@"    public void MyVoidMethod() { }
    /// <value>This is the MyClass.MyGetSetProperty value.</value>
    public double MyGetSetProperty { get; set; }
}";

        List<string> docFiles = new() { interfaceDocFile, classDocFile };
        List<string> originalCodeFiles = new() { interfaceOriginalCode, classOriginalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { interfaceDocId, interfaceExpectedCode }, { classDocId, classExpectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Preserve_DoubleSlash_Comments(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>This is the MyClass type summary.</summary>
    <remarks>These are the MyClass type remarks.</remarks>
  </Docs>
  <Members>
    <Member MemberName="".ctor"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.#ctor"" />
      <Docs>
        <summary>This is the MyClass constructor summary.</summary>
        <remarks>These are the MyClass constructor remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace
{
    // Comment on top of type
    public class MyClass
    {
        // Comment on top of constructor
        public MyClass() { }
    }
}";

        string expectedCode = @"namespace MyNamespace
{
    // Comment on top of type
    /// <summary>This is the MyClass type summary.</summary>" +
GetRemarks(skipRemarks, "MyClass type", "    ") +
@"    public class MyClass
    {
        // Comment on top of constructor
        /// <summary>This is the MyClass constructor summary.</summary>" +
GetRemarks(skipRemarks, "MyClass constructor", "        ") +
@"        public MyClass() { }
    }
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Override_Existing_TripleSlash_Comments(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>New MyClass type summary.</summary>
    <remarks>To be added.</remarks>
  </Docs>
  <Members>
    <Member MemberName="".ctor"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.#ctor"" />
      <Docs>
        <summary>To be added.</summary>
        <remarks>New MyClass constructor remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace {
    /// <summary>Replaceable MyClass type summary.</summary>
    /// <remarks>Unreplaceable MyClass type remarks.</remarks>
    public class MyClass
    {
        /// <summary>Unreplaceable MyClass constructor summary.</summary>
        /// <remarks>Replaceable MyClass constructor remarks.</remarks>
        public MyClass() { }
    }
}";

        string ctorRemarks = skipRemarks ? "\n" : "\n        /// <remarks>New MyClass constructor remarks.</remarks>\n";
        string expectedCode = @"namespace MyNamespace {
    /// <summary>New MyClass type summary.</summary>
    /// <remarks>Unreplaceable MyClass type remarks.</remarks>
    public class MyClass
    {
        /// <summary>Unreplaceable MyClass constructor summary.</summary>" +
ctorRemarks +
@"        public MyClass() { }
    }
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Full_Enum(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyEnum";

        string docFile = @"<Type Name=""MyEnum"" FullName=""MyNamespace.MyEnum"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyEnum"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>This is the MyEnum summary.</summary>
    <remarks>These are the MyEnum remarks.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""Value1"">
      <MemberSignature Language=""DocId"" Value=""F:MyNamespace.MyEnum.Value1"" />
      <Docs>
        <summary>This is the MyEnum.Value1 summary.</summary>
      </Docs>
    </Member>
    <Member MemberName=""Value2"">
      <MemberSignature Language=""DocId"" Value=""F:MyNamespace.MyEnum.Value2"" />
      <Docs>
        <summary>This is the MyEnum.Value2 summary.</summary>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public enum MyEnum
{
    Value1,
    Value2
}";

        string expectedCode = @"namespace MyNamespace;
/// <summary>This is the MyEnum summary.</summary>" +
GetRemarks(skipRemarks, "MyEnum") +
@"public enum MyEnum
{
    /// <summary>This is the MyEnum.Value1 summary.</summary>
    Value1,
    /// <summary>This is the MyEnum.Value2 summary.</summary>
    Value2
}";


        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Full_Class(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyClass";

        string docFile = @"<Type Name=""MyClass"" FullName=""MyNamespace.MyClass"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyClass"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>This is the MyClass summary.</summary>
    <remarks>These are the MyClass remarks.</remarks>
  </Docs>
  <Members>
    <Member MemberName="".ctor"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.#ctor"" />
      <Docs>
        <summary>This is the MyClass constructor summary.</summary>
        <remarks>These are the MyClass constructor remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName="".ctor"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.#ctor(System.Int32)"" />
      <Docs>
        <summary>This is the MyClass constructor summary.</summary>
        <param name=""intParam"">This is the MyClass constructor parameter description.</param>
        <remarks>These are the MyClass constructor remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyVoidMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.MyVoidMethod"" />
      <Docs>
        <summary>This is the MyVoidMethod summary.</summary>
        <remarks>These are the MyVoidMethod remarks.</remarks>
        <exception cref=""T:System.NullReferenceException"">The null reference exception thrown by MyVoidMethod.</exception>
      </Docs>
    </Member>
    <Member MemberName=""MyIntMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.MyIntMethod(System.Int32)"" />
      <Docs>
        <param name=""withArgument"">This is the MyIntMethod withArgument description.</param>
        <summary>This is the MyIntMethod summary.</summary>
        <returns>This is the MyIntMethod returns description.</returns>
        <remarks>These are the MyIntMethod remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyGenericMethod&lt;T&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.MyGenericMethod``1(``0)"" />
      <Docs>
        <typeparam name=""T"">This is the MyGenericMethod type parameter description.</typeparam>
        <param name=""withGenericArgument"">This is the MyGenericMethod withGenericArgument description.</param>
        <summary>This is the MyGenericMethod summary.</summary>
        <returns>This is the MyGenericMethod returns description.</returns>
        <remarks>These are the MyGenericMethod remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyField"">
      <MemberSignature Language=""DocId"" Value=""F:MyNamespace.MyClass.MyField"" />
      <Docs>
        <summary>This is the MyField summary.</summary>
        <remarks>These are the MyField remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MySetProperty"">
      <MemberSignature Language=""DocId"" Value=""P:MyNamespace.MyClass.MySetProperty"" />
      <MemberType>Property</MemberType>
      <Docs>
        <summary>This is the MySetProperty summary.</summary>
        <value>This is the MySetProperty value.</value>
        <remarks>These are the MySetProperty remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyGetProperty"">
      <MemberSignature Language=""DocId"" Value=""P:MyNamespace.MyClass.MyGetProperty"" />
      <MemberType>Property</MemberType>
      <Docs>
        <summary>This is the MyGetProperty summary.</summary>
        <value>This is the MyGetProperty value.</value>
        <remarks>These are the MyGetProperty remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyGetSetProperty"">
      <MemberSignature Language=""DocId"" Value=""P:MyNamespace.MyClass.MyGetSetProperty"" />
      <MemberType>Property</MemberType>
      <Docs>
        <summary>This is the MyGetSetProperty summary.</summary>
        <value>This is the MyGetSetProperty value.</value>
        <remarks>These are the MyGetSetProperty remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""op_Addition"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyClass.op_Addition(MyNamespace.MyClass,MyNamespace.MyClass)"" />
      <Docs>
        <param name=""value1"">This is the + operator value1 description.</param>
        <param name=""value2"">This is the + operator value2 description.</param>
        <summary>This is the + operator summary.</summary>
        <returns>This is the + operator returns description.</returns>
        <remarks>These are the + operator remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public class MyClass
{
    public MyClass() { }
    public MyClass(int intParam) { }
    public void MyVoidMethod() { }
    public int MyIntMethod(int withArgument) => withArgument;
    public T MyGenericMethod<T>(T withGenericArgument) => withGenericArgument;
    public double MyField;
    public double MySetProperty { set => MyField = value; }
    public double MyGetProperty => MyField;
    public double MyGetSetProperty { get; set; }
    public static MyClass operator +(MyClass value1, MyClass value2) => value1;
}";

        string expectedCode = @"namespace MyNamespace;
/// <summary>This is the MyClass summary.</summary>" +
GetRemarks(skipRemarks, "MyClass") +
@"public class MyClass
{
    /// <summary>This is the MyClass constructor summary.</summary>" +
GetRemarks(skipRemarks, "MyClass constructor", "    ") +
@"    public MyClass() { }
    /// <summary>This is the MyClass constructor summary.</summary>
    /// <param name=""intParam"">This is the MyClass constructor parameter description.</param>" +
GetRemarks(skipRemarks, "MyClass constructor", "    ") +
@"    public MyClass(int intParam) { }
    /// <summary>This is the MyVoidMethod summary.</summary>
    /// <exception cref=""System.NullReferenceException"">The null reference exception thrown by MyVoidMethod.</exception>" +
GetRemarks(skipRemarks, "MyVoidMethod", "    ") +
@"    public void MyVoidMethod() { }
    /// <summary>This is the MyIntMethod summary.</summary>
    /// <param name=""withArgument"">This is the MyIntMethod withArgument description.</param>
    /// <returns>This is the MyIntMethod returns description.</returns>" +
GetRemarks(skipRemarks, "MyIntMethod", "    ") +
@"    public int MyIntMethod(int withArgument) => withArgument;
    /// <summary>This is the MyGenericMethod summary.</summary>
    /// <typeparam name=""T"">This is the MyGenericMethod type parameter description.</typeparam>
    /// <param name=""withGenericArgument"">This is the MyGenericMethod withGenericArgument description.</param>
    /// <returns>This is the MyGenericMethod returns description.</returns>" +
GetRemarks(skipRemarks, "MyGenericMethod", "    ") +
@"    public T MyGenericMethod<T>(T withGenericArgument) => withGenericArgument;
    /// <summary>This is the MyField summary.</summary>" +
GetRemarks(skipRemarks, "MyField", "    ") +
@"    public double MyField;
    /// <summary>This is the MySetProperty summary.</summary>
    /// <value>This is the MySetProperty value.</value>" +
GetRemarks(skipRemarks, "MySetProperty", "    ") +
@"    public double MySetProperty { set => MyField = value; }
    /// <summary>This is the MyGetProperty summary.</summary>
    /// <value>This is the MyGetProperty value.</value>" +
GetRemarks(skipRemarks, "MyGetProperty", "    ") +
@"    public double MyGetProperty => MyField;
    /// <summary>This is the MyGetSetProperty summary.</summary>
    /// <value>This is the MyGetSetProperty value.</value>" +
GetRemarks(skipRemarks, "MyGetSetProperty", "    ") +
@"    public double MyGetSetProperty { get; set; }
    /// <summary>This is the + operator summary.</summary>
    /// <param name=""value1"">This is the + operator value1 description.</param>
    /// <param name=""value2"">This is the + operator value2 description.</param>
    /// <returns>This is the + operator returns description.</returns>" +
GetRemarks(skipRemarks, "+ operator", "    ") +
@"    public static MyClass operator +(MyClass value1, MyClass value2) => value1;
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Full_Struct(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyStruct";

        string docFile = @"<Type Name=""MyStruct"" FullName=""MyNamespace.MyStruct"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyStruct"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>This is the MyStruct summary.</summary>
    <remarks>These are the MyStruct remarks.</remarks>
  </Docs>
  <Members>
    <Member MemberName="".ctor"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyStruct.#ctor"" />
      <Docs>
        <summary>This is the MyStruct constructor summary.</summary>
        <remarks>These are the MyStruct constructor remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName="".ctor"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyStruct.#ctor(System.Int32)"" />
      <Docs>
        <summary>This is the MyStruct constructor summary.</summary>
        <param name=""intParam"">This is the MyStruct constructor parameter description.</param>
        <remarks>These are the MyStruct constructor remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyVoidMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyStruct.MyVoidMethod"" />
      <Docs>
        <summary>This is the MyVoidMethod summary.</summary>
        <remarks>These are the MyVoidMethod remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyIntMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyStruct.MyIntMethod(System.Int32)"" />
      <Docs>
        <param name=""withArgument"">This is the MyIntMethod withArgument description.</param>
        <summary>This is the MyIntMethod summary.</summary>
        <returns>This is the MyIntMethod returns description.</returns>
        <remarks>These are the MyIntMethod remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyGenericMethod&lt;T&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyStruct.MyGenericMethod``1(``0)"" />
      <Docs>
        <typeparam name=""T"">This is the MyGenericMethod type parameter description.</typeparam>
        <param name=""withGenericArgument"">This is the MyGenericMethod withGenericArgument description.</param>
        <summary>This is the MyGenericMethod summary.</summary>
        <returns>This is the MyGenericMethod returns description.</returns>
        <remarks>These are the MyGenericMethod remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyField"">
      <MemberSignature Language=""DocId"" Value=""F:MyNamespace.MyStruct.MyField"" />
      <Docs>
        <summary>This is the MyField summary.</summary>
        <remarks>These are the MyField remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MySetProperty"">
      <MemberSignature Language=""DocId"" Value=""P:MyNamespace.MyStruct.MySetProperty"" />
      <MemberType>Property</MemberType>
      <Docs>
        <summary>This is the MySetProperty summary.</summary>
        <value>This is the MySetProperty value.</value>
        <remarks>These are the MySetProperty remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyGetProperty"">
      <MemberSignature Language=""DocId"" Value=""P:MyNamespace.MyStruct.MyGetProperty"" />
      <MemberType>Property</MemberType>
      <Docs>
        <summary>This is the MyGetProperty summary.</summary>
        <value>This is the MyGetProperty value.</value>
        <remarks>These are the MyGetProperty remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyGetSetProperty"">
      <MemberSignature Language=""DocId"" Value=""P:MyNamespace.MyStruct.MyGetSetProperty"" />
      <MemberType>Property</MemberType>
      <Docs>
        <summary>This is the MyGetSetProperty summary.</summary>
        <value>This is the MyGetSetProperty value.</value>
        <remarks>These are the MyGetSetProperty remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""op_Addition"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyStruct.op_Addition(MyNamespace.MyStruct,MyNamespace.MyStruct)"" />
      <Docs>
        <param name=""value1"">This is the + operator value1 description.</param>
        <param name=""value2"">This is the + operator value2 description.</param>
        <summary>This is the + operator summary.</summary>
        <returns>This is the + operator returns description.</returns>
        <remarks>These are the + operator remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public struct MyStruct
{
    public MyStruct() { }
    public MyStruct(int intParam) { }
    public void MyVoidMethod() { }
    public int MyIntMethod(int withArgument) => withArgument;
    public T MyGenericMethod<T>(T withGenericArgument) => withGenericArgument;
    public double MyField;
    public double MySetProperty { set => MyField = value; }
    public double MyGetProperty => MyField;
    public double MyGetSetProperty { get; set; }
    public static MyStruct operator +(MyStruct value1, MyStruct value2) => value1;
}";

        string expectedCode = @"namespace MyNamespace;
/// <summary>This is the MyStruct summary.</summary>" +
GetRemarks(skipRemarks, "MyStruct") +
@"public struct MyStruct
{
    /// <summary>This is the MyStruct constructor summary.</summary>" +
GetRemarks(skipRemarks, "MyStruct constructor", "    ") +
@"    public MyStruct() { }
    /// <summary>This is the MyStruct constructor summary.</summary>
    /// <param name=""intParam"">This is the MyStruct constructor parameter description.</param>" +
GetRemarks(skipRemarks, "MyStruct constructor", "    ") +
@"    public MyStruct(int intParam) { }
    /// <summary>This is the MyVoidMethod summary.</summary>" +
GetRemarks(skipRemarks, "MyVoidMethod", "    ") +
@"    public void MyVoidMethod() { }
    /// <summary>This is the MyIntMethod summary.</summary>
    /// <param name=""withArgument"">This is the MyIntMethod withArgument description.</param>
    /// <returns>This is the MyIntMethod returns description.</returns>" +
GetRemarks(skipRemarks, "MyIntMethod", "    ") +
@"    public int MyIntMethod(int withArgument) => withArgument;
    /// <summary>This is the MyGenericMethod summary.</summary>
    /// <typeparam name=""T"">This is the MyGenericMethod type parameter description.</typeparam>
    /// <param name=""withGenericArgument"">This is the MyGenericMethod withGenericArgument description.</param>
    /// <returns>This is the MyGenericMethod returns description.</returns>" +
GetRemarks(skipRemarks, "MyGenericMethod", "    ") +
@"    public T MyGenericMethod<T>(T withGenericArgument) => withGenericArgument;
    /// <summary>This is the MyField summary.</summary>" +
GetRemarks(skipRemarks, "MyField", "    ") +
@"    public double MyField;
    /// <summary>This is the MySetProperty summary.</summary>
    /// <value>This is the MySetProperty value.</value>" +
GetRemarks(skipRemarks, "MySetProperty", "    ") +
@"    public double MySetProperty { set => MyField = value; }
    /// <summary>This is the MyGetProperty summary.</summary>
    /// <value>This is the MyGetProperty value.</value>" +
GetRemarks(skipRemarks, "MyGetProperty", "    ") +
@"    public double MyGetProperty => MyField;
    /// <summary>This is the MyGetSetProperty summary.</summary>
    /// <value>This is the MyGetSetProperty value.</value>" +
GetRemarks(skipRemarks, "MyGetSetProperty", "    ") +
@"    public double MyGetSetProperty { get; set; }
    /// <summary>This is the + operator summary.</summary>
    /// <param name=""value1"">This is the + operator value1 description.</param>
    /// <param name=""value2"">This is the + operator value2 description.</param>
    /// <returns>This is the + operator returns description.</returns>" +
GetRemarks(skipRemarks, "+ operator", "    ") +
@"    public static MyStruct operator +(MyStruct value1, MyStruct value2) => value1;
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Full_Interface(bool skipRemarks)
    {
        string docId = "T:MyNamespace.MyInterface";

        string docFile = @"<Type Name=""MyInterface"" FullName=""MyNamespace.MyInterface"">
  <TypeSignature Language=""DocId"" Value=""T:MyNamespace.MyInterface"" />
  <AssemblyInfo>
    <AssemblyName>MyAssembly</AssemblyName>
  </AssemblyInfo>
  <Docs>
    <summary>This is the MyInterface summary.</summary>
    <remarks>These are the MyInterface remarks.</remarks>
  </Docs>
  <Members>
    <Member MemberName=""MyVoidMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyInterface.MyVoidMethod"" />
      <Docs>
        <summary>This is the MyVoidMethod summary.</summary>
        <remarks>These are the MyVoidMethod remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyIntMethod"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyInterface.MyIntMethod(System.Int32)"" />
      <Docs>
        <param name=""withArgument"">This is the MyIntMethod withArgument description.</param>
        <summary>This is the MyIntMethod summary.</summary>
        <returns>This is the MyIntMethod returns description.</returns>
        <remarks>These are the MyIntMethod remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyGenericMethod&lt;T&gt;"">
      <MemberSignature Language=""DocId"" Value=""M:MyNamespace.MyInterface.MyGenericMethod``1(``0)"" />
      <Docs>
        <typeparam name=""T"">This is the MyGenericMethod type parameter description.</typeparam>
        <param name=""withGenericArgument"">This is the MyGenericMethod withGenericArgument description.</param>
        <summary>This is the MyGenericMethod summary.</summary>
        <returns>This is the MyGenericMethod returns description.</returns>
        <remarks>These are the MyGenericMethod remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MySetProperty"">
      <MemberSignature Language=""DocId"" Value=""P:MyNamespace.MyInterface.MySetProperty"" />
      <MemberType>Property</MemberType>
      <Docs>
        <summary>This is the MySetProperty summary.</summary>
        <value>This is the MySetProperty value.</value>
        <remarks>These are the MySetProperty remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyGetProperty"">
      <MemberSignature Language=""DocId"" Value=""P:MyNamespace.MyInterface.MyGetProperty"" />
      <MemberType>Property</MemberType>
      <Docs>
        <summary>This is the MyGetProperty summary.</summary>
        <value>This is the MyGetProperty value.</value>
        <remarks>These are the MyGetProperty remarks.</remarks>
      </Docs>
    </Member>
    <Member MemberName=""MyGetSetProperty"">
      <MemberSignature Language=""DocId"" Value=""P:MyNamespace.MyInterface.MyGetSetProperty"" />
      <MemberType>Property</MemberType>
      <Docs>
        <summary>This is the MyGetSetProperty summary.</summary>
        <value>This is the MyGetSetProperty value.</value>
        <remarks>These are the MyGetSetProperty remarks.</remarks>
      </Docs>
    </Member>
  </Members>
</Type>";

        string originalCode = @"namespace MyNamespace;
public interface MyInterface
{
    public void MyVoidMethod();
    public int MyIntMethod(int withArgument);
    public T MyGenericMethod<T>(T withGenericArgument);
    public double MySetProperty { set; }
    public double MyGetProperty { get; }
    public double MyGetSetProperty { get; set; }
}";

        string expectedCode = @"namespace MyNamespace;
/// <summary>This is the MyInterface summary.</summary>" +
GetRemarks(skipRemarks, "MyInterface") +
@"public interface MyInterface
{
    /// <summary>This is the MyVoidMethod summary.</summary>" +
GetRemarks(skipRemarks, "MyVoidMethod", "    ") +
@"    public void MyVoidMethod();
    /// <summary>This is the MyIntMethod summary.</summary>
    /// <param name=""withArgument"">This is the MyIntMethod withArgument description.</param>
    /// <returns>This is the MyIntMethod returns description.</returns>" +
GetRemarks(skipRemarks, "MyIntMethod", "    ") +
@"    public int MyIntMethod(int withArgument);
    /// <summary>This is the MyGenericMethod summary.</summary>
    /// <typeparam name=""T"">This is the MyGenericMethod type parameter description.</typeparam>
    /// <param name=""withGenericArgument"">This is the MyGenericMethod withGenericArgument description.</param>
    /// <returns>This is the MyGenericMethod returns description.</returns>" +
GetRemarks(skipRemarks, "MyGenericMethod", "    ") +
@"    public T MyGenericMethod<T>(T withGenericArgument);
    /// <summary>This is the MySetProperty summary.</summary>
    /// <value>This is the MySetProperty value.</value>" +
GetRemarks(skipRemarks, "MySetProperty", "    ") +
@"    public double MySetProperty { set; }
    /// <summary>This is the MyGetProperty summary.</summary>
    /// <value>This is the MyGetProperty value.</value>" +
GetRemarks(skipRemarks, "MyGetProperty", "    ") +
@"    public double MyGetProperty { get; }
    /// <summary>This is the MyGetSetProperty summary.</summary>
    /// <value>This is the MyGetSetProperty value.</value>" +
GetRemarks(skipRemarks, "MyGetSetProperty", "    ") +
@"    public double MyGetSetProperty { get; set; }
}";

        List<string> docFiles = new() { docFile };
        List<string> originalCodeFiles = new() { originalCode };
        Dictionary<string, string> expectedCodeFiles = new() { { docId, expectedCode } };
        StringTestData data = new(docFiles, originalCodeFiles, expectedCodeFiles, false);

        return TestWithStringsAsync(data, skipRemarks);
    }

    private static string GetRemarks(bool skipRemarks, string apiName, string spacing = "")
    {
        return skipRemarks ? @"
" : $@"
{spacing}/// <remarks>These are the {apiName} remarks.</remarks>
";
    }

    private static Task TestWithStringsAsync(StringTestData data, bool skipRemarks) =>
        TestWithStringsAsync(new Configuration() { SkipInterfaceImplementations = false, SkipRemarks = skipRemarks }, DefaultAssembly, data);

    private static async Task TestWithStringsAsync(Configuration c, string assembly, StringTestData data)
    {
        Assert.NotEmpty(data.XDocs);
        Assert.NotEmpty(data.OriginalCodeFiles);
        Assert.NotEmpty(data.ExpectedCodeFiles);

        c.IncludedAssemblies.Add(assembly);

        CancellationTokenSource cts = new();

        CSharpCompilationOptions compileOptions = new(outputKind: OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release, allowUnsafe: true);

        List<SyntaxTree> syntaxTrees = new();

        CSharpParseOptions parseOptions = new(languageVersion: LanguageVersion.Preview, kind: SourceCodeKind.Regular);
        foreach (string originalCode in data.OriginalCodeFiles)
        {
            CompilationUnitSyntax pcu = SyntaxFactory.ParseCompilationUnit(originalCode, options: parseOptions);
            syntaxTrees.Add(pcu.SyntaxTree);
        }

        CSharpCompilation compilation = CSharpCompilation.Create(assembly, options: compileOptions)
            .AddSyntaxTrees(syntaxTrees);

        // Use only when it is expected to inherit documentation from inherited implementations of .NET APIs
        if (data.AddMsCorLibReferences)
        {
            // reference same mscorlib we're running on
            compilation = compilation.AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        }

        ToTripleSlashPorter porter = new(c);

        UTF8Encoding utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

        int fileNumber = 0;
        foreach (XDocument xDoc in data.XDocs)
        {
            porter.LoadDocsFile(xDoc, $"File{fileNumber}.xml", utf8NoBom);
            fileNumber++;
        }

        bool isMSBuildProject = false;
        await porter.MatchSymbolsAsync(compilation, isMSBuildProject, cts.Token);
        await porter.PortAsync(isMSBuildProject, cts.Token);

        IEnumerable<(string, IEnumerable<ResolvedLocation>)> portingResults = porter.GetResults();
        Assert.True(portingResults.Any(), "No items returned in porting results.");
        foreach ((string resultDocId, IEnumerable<ResolvedLocation> symbolLocations) in portingResults)
        {
            Assert.True(data.ExpectedCodeFiles.TryGetValue(resultDocId, out string expectedCode), $"Could not find docId in dictionary: {resultDocId}");

            Assert.True(symbolLocations.Any(), $"No symbol locations found for {resultDocId}.");
            foreach (ResolvedLocation location in symbolLocations)
            {
                string actualCode = location.NewNode.ToFullString();
                Assert.Equal(expectedCode, actualCode);
            }
        }
    }
}
