using System;

namespace MyNamespace
{
    /// <summary>This is the MyType class summary.</summary>
    /// <remarks>These are the MyType class remarks.
    /// Multiple lines.</remarks>
    public class MyType
    {
        /// <summary>This is the MyType constructor summary.</summary>
        public MyType()
        {
        } /* Trailing comments should remain untouched */

        // Original double slash comments. They should not be replaced (internal).
        internal MyType(int myProperty)
        {
            _myProperty = myProperty;
        } // Trailing comments should remain untouched

        /// <summary>
        /// Triple slash comments above private members should remain untouched.
        /// </summary>
        private int _otherProperty;

        // Double slash comments above private members should remain untouched.
        private int _myProperty;

        /// <summary>This is the MyProperty summary.</summary>
        /// <value>This is the MyProperty value.</value>
        /// <remarks>These are the MyProperty remarks.
        /// Multiple lines and a reference to the field <see cref="MyNamespace.MyType.MyField" /> and the xref uses displayProperty, which should be ignored when porting.</remarks>
        public int MyProperty
        {
            get { return _myProperty; /* Internal comments should remain untouched. */ }
            set { _myProperty = value; } // Internal comments should remain untouched
        }

        /// <summary>This is the MyField summary.</summary>
        /// <remarks>These are the MyField remarks.
        /// Multiple lines.</remarks>
        public int MyField = 1;

        /// <summary>This is the MyIntMethod summary.</summary>
        /// <param name="param1">This is the MyIntMethod param1 summary.</param>
        /// <param name="param2">This is the MyIntMethod param2 summary.</param>
        /// <returns>This is the MyIntMethod return value. It mentions the <see cref="System.ArgumentNullException" />.</returns>
        /// <remarks>These are the MyIntMethod remarks.
        /// Multiple lines.
        /// Mentions the <paramref name="param1" />, the <see cref="System.ArgumentNullException" /> and the <paramref name="param2" />.
        /// There are also a <see langword="true" /> and a <see langword="null" />.</remarks>
        /// <exception cref="System.ArgumentNullException">This is the ArgumentNullException thrown by MyIntMethod. It mentions the <paramref name="param1" />.</exception>
        /// <exception cref="System.IndexOutOfRangeException">This is the IndexOutOfRangeException thrown by MyIntMethod.</exception>
        public int MyIntMethod(int param1, int param2)
        {
            // Internal comments should remain untouched.
            return MyField + param1 + param2;
        }

        /// <summary>This is the MyVoidMethod summary.</summary>
        /// <remarks>These are the MyVoidMethod remarks.
        /// Multiple lines.
        /// Mentions the <see cref="System.ArgumentNullException" />.</remarks>
        /// <exception cref="System.ArgumentNullException">This is the ArgumentNullException thrown by MyVoidMethod. It mentions the <paramref name="param1" />.</exception>
        /// <exception cref="System.IndexOutOfRangeException">This is the IndexOutOfRangeException thrown by MyVoidMethod.
        /// -or-
        /// This is the second case.
        /// Empty newlines should be respected.</exception>
        public void MyVoidMethod()
        {
        }

        /// <summary>
        /// This method simulates a newly added API that did not have documentation in the docs xml.
        /// The developer added the documentation in triple slash comments, so they should be preserved
        /// and considered the source of truth.
        /// </summary>
        /// <remarks>
        /// These remarks are the source of truth.
        /// </remarks>
        public void UndocumentedMethod()
        {
        }

        /// <summary>This is the MyTypeParamMethod summary.</summary>
        /// <param name="param1">This is the MyTypeParamMethod parameter param1.</param>
        /// <typeparam name="T">This is the MyTypeParamMethod typeparam T.</typeparam>
        /// <remarks>This is a reference to the typeparam <typeparamref name="T" />.
        /// This is a reference to the parameter <paramref name="param1" />.</remarks>
        public void MyTypeParamMethod<T>(int param1)
        {
        }

        /// <summary>This is the MyDelegate summary.</summary>
        /// <param name="sender">This is the sender parameter.</param>
        /// <param name="e">This is the e parameter.</param>
        /// <typeparam name="T">This is the MyDelegate typeparam T.</typeparam>
        /// <seealso cref="System.Delegate"/>
        /// <altmember cref="System.Delegate"/>
        /// <related type="Article" href="https://github.com/dotnet/runtime">The .NET Runtime repo.</related>
        public delegate void MyDelegate<T>(object sender, T e);

        /// <summary>This is the MyEvent summary.</summary>
        public event MyDelegate MyEvent;

        /// <summary>Adds two MyType instances.</summary>
        /// <param name="value1">The first type to add.</param>
        /// <param name="value2">The second type to add.</param>
        /// <returns>The added types.</returns>
        public static MyType operator +(MyType value1, MyType value2)
        {
            return value1;
        }
    }
}