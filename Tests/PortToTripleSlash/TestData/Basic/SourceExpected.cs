using System;

namespace MyNamespace
{
    /// <summary>This is MyType class summary.</summary>
    /// <remarks><![CDATA[
    ///
    /// ## Remarks
    ///
    /// These are the MyType class remarks.
    ///
    /// Multiple lines.
    ///
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
        ///
        /// ## Remarks
        ///
        /// These are the MyProperty remarks.
        ///
        /// Multiple lines.
        ///
        /// ]]></remarks>
        public int MyProperty
        {
            get { return _myProperty; }
            set { _myProperty = value; }
        }

        /// <summary>This is the MyField summary.</summary>
        /// <remarks><![CDATA[
        ///
        /// ## Remarks
        ///
        /// These are the MyField remarks.
        ///
        /// Multiple lines.
        ///
        /// ]]></remarks>
        public int MyField = 1;

        /// <summary>This is the MyIntMethod summary.</summary>
        /// <returns>This is the MyIntMethod return value. It mentions the <see cref="System.ArgumentNullException" />.</returns>
        /// <remarks><![CDATA[
        ///
        /// ## Remarks
        ///
        /// These are the MyIntMethod remarks.
        ///
        /// Multiple lines.
        ///
        /// Mentions the `param1` and the <xref:System.ArgumentNullException>.
        ///
        /// ]]></remarks>
        /// <exception cref="System.ArgumentNullException">This is the ArgumentNullException thrown by MyIntMethod. It mentions the <paramref name="param1" />.</exception>
        /// <exception cref="System.IndexOutOfRangeException">This is the IndexOutOfRangeException thrown by MyIntMethod.</exception>
        public int MyIntMethod(int param1)
        {
            return MyField + param1;
        }

        /// <summary>This is the MyVoidMethod summary.</summary>
        /// <returns>This is the MyVoidMethod return value. It mentions the <see cref="System.ArgumentNullException" />.</returns>
        /// <remarks><![CDATA[
        ///
        /// ## Remarks
        ///
        /// These are the MyVoidMethod remarks.
        ///
        /// Multiple lines.
        ///
        /// Mentions the `param1` and the <xref:System.ArgumentNullException>.
        ///
        /// ]]></remarks>
        /// <exception cref="System.ArgumentNullException">This is the ArgumentNullException thrown by MyVoidMethod. It mentions the <paramref name="param1" />.</exception>
        /// <exception cref="System.IndexOutOfRangeException">This is the IndexOutOfRangeException thrown by MyVoidMethod.</exception>
        public void MyVoidMethod()
        {
        }
    }
}
