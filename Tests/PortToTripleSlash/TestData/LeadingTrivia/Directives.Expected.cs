namespace LeadingTriviaTestData.Directives.Expected
{
#if false
    internal
#else
    /// <summary>Directives</summary>
    /// <remarks>Directives</remarks>
    public
#endif
    class MyType
    {
        #region MyEnum

#if true
        /// <summary>Directives</summary>
        /// <remarks>Directives</remarks>
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
        /// <summary>Directives</summary>
        /// <remarks>Directives</remarks>
        // This comment should remain below the XML comments
        public int MyField;
#pragma warning restore

#nullable enable
        /// <summary>Directives</summary>
        /// <remarks>Directives</remarks>
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
        /// <summary>Directives</summary>
        /// <remarks>Directives</remarks>
        public bool MyMethod()
        {
            return true;
        }

        /// <summary>Directives</summary>
        /// <remarks>Directives</remarks>
        public interface MyInterface
        {
            bool IsInterface { get; }
        }
#endif
    }
}