using System;

namespace MyNamespace
{
    // Original MyGenericType<T> class comments with information for maintainers, must stay.
    public class MyGenericType<T>
    {
        // Original MyGenericType<T>.Enumerator class comments with information for maintainers, must stay.
        public class Enumerator { }
    }

    public static class MyGenericType
    {
        public static MyGenericType<TResult> Select<TSource, TResult>(this MyGenericType<TSource> source, Func<TSource, TResult> selector) => null;
    }
}
