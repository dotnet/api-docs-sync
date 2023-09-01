using System;

namespace MyNamespace
{
    // Original MyGenericType<T> class comments with information for maintainers, must stay.
    /// <summary>This is the MyGenericType{T} class summary.</summary>
    /// <remarks>Contains the nested class <see cref="MyNamespace.MyGenericType{T}.Enumerator" />.</remarks>
    public class MyGenericType<T>
    {
        // Original MyGenericType<T>.Enumerator class comments with information for maintainers, must stay.
        /// <summary>This is the MyGenericType{T}.Enumerator class summary.</summary>
        public class Enumerator { }
    }
}
