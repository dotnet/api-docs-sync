using System;

namespace MyNamespace
{
    public enum MyEnum
    {
        MyEnumValue0 = 0,

        MyEnumValue1 = 1
    }

    public class MyType
    {
        /// <summary>
        /// Original triple slash comments. They should be replaced.
        /// </summary>
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

        /// <summary>
        /// Original triple slash comments. They should be replaced.
        /// </summary>
        // Original double slash comments. They should be replaced.
        public int MyProperty
        {
            get { return _myProperty; /* Internal comments should remain untouched. */ }
            set { _myProperty = value; } // Internal comments should remain untouched
        }

        public int MyField = 1;

        public int MyIntMethod(int param1, int param2)
        {
            // Internal comments should remain untouched.
            return MyField + param1 + param2;
        }

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

        public void MyTypeParamMethod<T>(int param1)
        {
        }

        public delegate void MyDelegate<T>(object sender, T e);

        public event MyDelegate MyEvent;

        public static MyType operator +(MyType value1, MyType value2)
        {
            return value1;
        }
    }
}
