using System;

namespace MyNamespace
{
    public class MyType
    {
        public MyType()
        {
        }

        internal MyType(int myProperty)
        {
            _myProperty = myProperty;
        }

        private int _myProperty;

        public int MyProperty
        {
            get { return _myProperty; }
            set { _myProperty = value; }
        }

        public int MyField = 1;

        public int MyIntMethod(int param1, int param2)
        {
            return MyField + param1 + param2;
        }

        public void MyVoidMethod()
        {
        }

        public void MyTypeParamMethod<T>()
        {
        }
    }
}
