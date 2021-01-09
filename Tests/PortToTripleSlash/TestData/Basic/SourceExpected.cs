using System;

namespace MyNamespace
{
    /// <summary>This is the MyType class summary.</summary>
    /// <remarks><![CDATA[
    /// These are the MyType class remarks.
    /// Multiple lines.
    /// ]]></remarks>
    public class MyType
    {
        /// <summary>This is the MyType constructor summary.</summary>
        public MyType()
        {
        }

        internal MyType(int myProperty)
        {
            _myProperty = myProperty;
        }

        private int _myProperty;

        /// <summary>This is the MyProperty summary.</summary>
        /// <value>This is the MyProperty value.</value>
        /// <remarks><![CDATA[
        /// These are the MyProperty remarks.
        /// Multiple lines.
        /// ]]></remarks>
        public int MyProperty
        {
            get { return _myProperty; }
            set { _myProperty = value; }
        }

        /// <summary>This is the MyField summary.</summary>
        /// <remarks><![CDATA[
        /// These are the MyField remarks.
        /// Multiple lines.
        /// ]]></remarks>
        public int MyField = 1;

        /// <summary>This is the MyIntMethod summary.</summary>
        /// <param name="param1">This is the MyIntMethod param1 summary.</param>
        /// <param name="param2">This is the MyIntMethod param2 summary.</param>
        /// <returns>This is the MyIntMethod return value. It mentions the <see cref="System.ArgumentNullException" />.</returns>
        /// <remarks><![CDATA[
        /// These are the MyIntMethod remarks.
        /// Multiple lines.
        /// Mentions the `param1` and the <xref:System.ArgumentNullException>.
        /// ]]></remarks>
        /// <exception cref="System.ArgumentNullException">This is the ArgumentNullException thrown by MyIntMethod. It mentions the <paramref name="param1" />.</exception>
        /// <exception cref="System.IndexOutOfRangeException">This is the IndexOutOfRangeException thrown by MyIntMethod.</exception>
        public int MyIntMethod(int param1, int param2)
        {
            return MyField + param1 + param2;
        }

        /// <summary>This is the MyVoidMethod summary.</summary>
        /// <remarks><![CDATA[
        /// These are the MyVoidMethod remarks.
        /// Multiple lines.
        /// Mentions the <xref:System.ArgumentNullException>.
        /// ]]></remarks>
        /// <exception cref="System.ArgumentNullException">This is the ArgumentNullException thrown by MyVoidMethod. It mentions the <paramref name="param1" />.</exception>
        /// <exception cref="System.IndexOutOfRangeException">This is the IndexOutOfRangeException thrown by MyVoidMethod.</exception>
        public void MyVoidMethod()
        {
        }

        /// <summary>This is the MyTypeParamMethod summary.</summary>
        /// <typeparam name="T">This is the MyTypeParamMethod typeparam T.</typeparam>
        public void MyTypeParamMethod<T>()
        {
        }

        /// <summary>This is the MyDelegate summary.</summary>
        /// <param name="sender">This is the sender parameter.</param>
        /// <param name="e">This is the e parameter.</param>
        /// <seealso cref="System.Delegate"/>
        /// <altmember cref="System.Delegate"/>
        /// <related type="Article" href="https://github.com/dotnet/runtime">The .NET Runtime repo.</related>
        public delegate void MyDelegate(object sender, object e);

        /// <summary>This is the MyEvent summary.</summary>
        public event MyDelegate MyEvent;
    }
}
