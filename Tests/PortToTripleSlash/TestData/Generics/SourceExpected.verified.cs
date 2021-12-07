using System;

namespace MyNamespace
{
    /// <summary>This is the MyGenericType{T} class summary.</summary>
    /// <remarks>The <see cref="MyGenericType{T}" /> type contains the nested class <see cref="MyNamespace.MyGenericType{T}.Enumerator" />.</remarks>
    // Original MyGenericType<T> class comments with information for maintainers, must stay.
    public class MyGenericType<T>
    {
        /// <summary>This is the MyGenericType{T}.Enumerator class summary.</summary>
        // Original MyGenericType<T>.Enumerator class comments with information for maintainers, must stay.
        public class Enumerator { }
    }

    /// <summary>This is the MyGenericType static class summary.</summary>
    public static class MyGenericType
    {
        /// <summary>Projects each element into a new form.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by <paramref name="selector" />.</typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on.</param>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <remarks>Here's a reference to <see cref="MyNamespace.MyGenericType.Select{T1,T2}(MyNamespace.MyGenericType{T1},System.Func{T1,T2})" />.</remarks>
        /// <altmember cref="System.Linq.Enumerable.Any{T}(System.Collections.Generic.IEnumerable{T},System.Func{T,System.Boolean})"/>
        public static MyGenericType<TResult> Select<TSource, TResult>(this MyGenericType<TSource> source, Func<TSource, TResult> selector) => null;
    }
}
