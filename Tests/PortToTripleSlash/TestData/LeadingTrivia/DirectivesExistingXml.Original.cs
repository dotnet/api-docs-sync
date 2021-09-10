namespace LeadingTriviaTestData.DirectivesExistingXml.Original
{
    /// <summary>
    /// This is the original summary
    /// </summary>
    /// <remarks>
    /// These are the existing remarks
    /// </remarks>
#if false
    internal
#else
    public
#endif
    class MyType
    {
        #region MyEnum

#if true
        /// <summary>
        /// This is the original summary
        /// </summary>
        /// <remarks>
        /// These are the existing remarks
        /// </remarks>
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
        /// <summary>
        /// This is the original summary
        /// </summary>
        /// <remarks>
        /// These are the existing remarks
        /// </remarks>
        public int MyField;
#pragma warning restore

#nullable enable
        /// <summary>
        /// This is the original summary
        /// </summary>
        /// <remarks>
        /// These are the existing remarks
        /// </remarks>
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
        /// <summary>
        /// This is the original summary
        /// </summary>
        /// <remarks>
        /// These are the existing remarks
        /// </remarks>
        public bool MyMethod()
        {
            return true;
        }

        /// <summary>
        /// This is the original summary
        /// </summary>
        /// <remarks>
        /// These are the existing remarks
        /// </remarks>
        public interface MyInterface
        {
            bool IsInterface { get; }
        }
#endif
    }
}