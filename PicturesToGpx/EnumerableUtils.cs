using System;
using System.Collections.Generic;

namespace PicturesToGpx
{
    public static class EnumerableUtils
    {
        /// <summary>Merge two sorted enumerables by the field.</summary>
        public static IEnumerable<T> Merge<T>(IEnumerable<T> enumerable1, IEnumerable<T> enumerable2, Func<T, T, bool> comparator)
        {
            var enumerator1 = enumerable1.GetEnumerator();
            var enumerator2 = enumerable2.GetEnumerator();

            bool hasValue1 = enumerator1.MoveNext();
            bool hasValue2 = enumerator2.MoveNext();

            while (hasValue2)
            {
                while (hasValue1 && comparator(enumerator1.Current, enumerator2.Current))
                {
                    yield return enumerator1.Current;
                    hasValue1 = enumerator1.MoveNext();
                }

                yield return enumerator2.Current;
                hasValue2 = enumerator2.MoveNext();
            }

            while (hasValue1)
            {
                yield return enumerator1.Current;
                hasValue1 = enumerator1.MoveNext();
            }

            while (hasValue2)
            {
                yield return enumerator2.Current;
                hasValue2 = enumerator2.MoveNext();
            }
        }
    }
}
