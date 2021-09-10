namespace LeadingTriviaTestData.DirectivesExistingXml.Expected
{
    /// <summary>DirectivesExistingXml</summary>
    /// <remarks>DirectivesExistingXml</remarks>
#if false
    internal
#else
    public
#endif
    class MyType
    {
        #region MyEnum

#if true
        /// <summary>DirectivesExistingXml</summary>
        /// <remarks>DirectivesExistingXml</remarks>
        public
#else
        internal
#endif
        enum MyEnum
        {
            FirstValue = 1,
            SecondValue,
            ThirdValue,
        }

        #endregion

#pragma warning disable
        // This comment should remain above the XML comments
        /// <summary>DirectivesExistingXml</summary>
        /// <remarks>DirectivesExistingXml</remarks>
        public int MyField;
#pragma warning restore

#nullable enable
        /// <summary>DirectivesExistingXml</summary>
        /// <remarks>DirectivesExistingXml</remarks>
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
#nullable restore

#if true
        /// <summary>DirectivesExistingXml</summary>
        /// <remarks>DirectivesExistingXml</remarks>
        public bool MyMethod()
        {
            return true;
        }

        /// <summary>DirectivesExistingXml</summary>
        /// <remarks>DirectivesExistingXml</remarks>
        public interface MyInterface
        {
            bool IsInterface { get; }
        }
#endif
    }
}