using System;

namespace MyNamespace
{
    // Original MyEnum enum comments with information for maintainers, must stay.
    public enum MyEnum
    {
        MyEnumValue0 = 0,

        MyEnumValue1 = 1
    }

    // Original MyType class comments with information for maintainers, must stay.
    public class MyType
    {
        // Original MyType constructor double slash comments on top of triple slash, with information for maintainers, must stay.
        /// <summary>
        /// Original triple slash comments. They should be replaced.
        /// </summary>
        // Original MyType constructor double slash comments on bottom of triple slash, with information for maintainers, must stay.
        public MyType()
        {
        } /* Trailing comments should remain untouched */

        // Original double slash comments, must stay (internal method).
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
        // Original MyProperty property double slash comments with information for maintainers, must stay.
        // This particular example has two rows of double slash comments and both should stay.
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

        // Original MyDelegate delegate comments with information for maintainers, must stay.
        public delegate void MyDelegate<T>(object sender, T e);

        public event MyDelegate MyEvent;

        // Original operator + method comments with information for maintainers, must stay.
        public static MyType operator +(MyType value1, MyType value2)
        {
            return value1;
        }
    }
}
