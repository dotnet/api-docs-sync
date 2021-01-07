using System;

namespace MyNamespace
{
    public class MyClass
    {
        public MyClass()
        {
        }

        internal MyClass(int myProperty)
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

        public int MyIntMethod(int param1)
        {
            return MyField + param1;
        }

        public void MyVoidMethod()
        {
        }
    }
}
