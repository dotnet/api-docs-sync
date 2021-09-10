namespace LeadingTriviaTestData.WhitespaceOnly.Expected
{
    /// <summary>WhitespaceOnly</summary>
    /// <remarks>WhitespaceOnly</remarks>
    public class MyType
    {
        /// <summary>WhitespaceOnly</summary>
        /// <remarks>WhitespaceOnly</remarks>
        public enum MyEnum
        {
            FirstValue = 1,
            SecondValue,
            ThirdValue,
        }

        /// <summary>WhitespaceOnly</summary>
        /// <remarks>WhitespaceOnly</remarks>
        public int MyField;

        /// <summary>WhitespaceOnly</summary>
        /// <remarks>WhitespaceOnly</remarks>
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

        /// <summary>WhitespaceOnly</summary>
        /// <remarks>WhitespaceOnly</remarks>
        public bool MyMethod()
        {
            return true;
        }

        /// <summary>WhitespaceOnly</summary>
        /// <remarks>WhitespaceOnly</remarks>
        public interface MyInterface
        {
            bool IsInterface { get; }
        }
    }
}