namespace LeadingTriviaTestData.WhitespaceOnly.Original
{
    public class MyType
    {
        public enum MyEnum
        {
            FirstValue = 1,
            SecondValue,
            ThirdValue,
        }

        public int MyField;

        public string MyProperty
        {
            get
            {
                return "";
            }
            set
            {

            }
        }

        public bool MyMethod()
        {
            return true;
        }

        public interface MyInterface
        {
            bool IsInterface { get; }
        }
    }
}