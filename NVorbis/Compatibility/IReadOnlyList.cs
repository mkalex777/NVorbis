using System;
using System.Collections;
using System.Collections.Generic;


namespace NVorbis
{
#if NETFX_40
    public interface IReadOnlyList<T> : IEnumerable<T>
    {
        T this[int index] { get; }
        int Count { get; }
    }

    static class ArrayExtension
    {
        public static IReadOnlyList<T> ToReadOnlyList<T>(this T[] array)
        {
            if (array == null) return null;
            return new ReadOnlyList<T>(array);
        }

        public static IReadOnlyList<T> ToReadOnlyList<T>(this IList<T> list)
        {
            if (list == null) return null;
            return new ReadOnlyList<T>(list);
        }

        private class ReadOnlyList<T> : IReadOnlyList<T>
        {
            private IList<T> _list;
            public ReadOnlyList(IList<T> list)
            {
                if (list == null)
                    throw new ArgumentNullException("list");
                this._list = list;
            }

            public T this[int index]
            {
                get { return _list[index]; }
            }

            public int Count
            {
                get { return _list.Count; }
            }

            public IEnumerator<T> GetEnumerator()
            {
                IEnumerable<T> enumerable = _list as IEnumerable<T>;
                return enumerable.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                IEnumerable enumerable = _list as IEnumerable;
                return enumerable.GetEnumerator();
            }
        }
    }
#else //NETFX_40
    static class ArrayExtension
    {
        public static IReadOnlyList<T> ToReadOnlyList<T>(this T[] array)
        {
            return (IReadOnlyList<T>)array;
        }

        public static IReadOnlyList<T> ToReadOnlyList<T>(this IList<T> list)
        {
            return (IReadOnlyList<T>)list;
        }
    }
#endif //NETFX_40
}
