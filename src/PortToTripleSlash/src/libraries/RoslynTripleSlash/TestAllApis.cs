// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//using System;

//namespace libraries.RoslynTripleSlash.TestAllApis;

///// <summary>
///// 
///// </summary>
//interface MyInterface
//{
//}

///// <summary>
///// 
///// </summary>
///// <typeparam name="T"></typeparam>
//interface MyInterfaceT<T>
//{
//}

///// <summary>
///// 
///// </summary>
//struct MyStruct
//{
//}

///// <summary>
///// 
///// </summary>
///// <typeparam name="T"></typeparam>
//struct MyStruct<T>
//{
//}

///// <summary>
///// 
///// </summary>
//class MyClass
//{
//}

///// <summary>
///// 
///// </summary>
///// <typeparam name="T"></typeparam>
//class MyClass<T>
//{
//}

//class Example
//{
//    /// <summary>
//    /// 
//    /// </summary>
//    /// <returns></returns>
//    delegate int MyDelegate();

//    /// <summary>
//    /// 
//    /// </summary>
//    /// <typeparam name="T"></typeparam>
//    /// <param name="x"></param>
//    /// <returns></returns>
//    delegate int MyDelegateT<T>(int x);

//    /// <summary>
//    /// 
//    /// </summary>
//    event MyDelegate MyEvent = null!;

//    /// <summary>
//    /// 
//    /// </summary>
//    event MyDelegateT<int> MyEventT = null!;

//    /// <summary>
//    /// 
//    /// </summary>
//    /// <param name="a"></param>
//    /// <param name="b"></param>
//    /// <returns></returns>
//    public static Example operator +(Example a, Example b)
//    {
//        _ = a;
//        _ = b;
//        return null!;
//    }

//    /// <summary>
//    /// 
//    /// </summary>
//    /// <value></value>
//    public int MyProperty { get; }

//    /// <summary>
//    /// 
//    /// </summary>
//    /// <value></value>
//    public MyStruct<int> MyPropertyT { get; set; }

//    /// <summary>
//    /// 
//    /// </summary>
//    public int myField;

//    /// <summary>
//    /// 
//    /// </summary>
//    public void MyMethod()
//    {

//    }

//    /// <summary>
//    /// 
//    /// </summary>
//    /// <typeparam name="T"></typeparam>
//    /// <param name="y"></param>
//    /// <returns></returns>
//    public int MyMethodT<T>(double y)
//    {
//        _ = y;
//        return 0;
//    }

//    /// <summary>
//    /// 
//    /// </summary>
//    /// <param name="a"></param>
//    /// <param name="b"></param>
//    public record MyRecord(int a, int b);

//    /// <summary>
//    /// 
//    /// </summary>
//    public enum MyEnum
//    {
//        /// <summary>
//        /// 
//        /// </summary>
//        MyValue1
//    }
//}
