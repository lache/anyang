using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Server.Core
{
    public class FlushableArray<T>
    {
        private static readonly T[] EmptyArray = Enumerable.Empty<T>().ToArray();
        private ConcurrentBag<T> _bag = new ConcurrentBag<T>();

        public void Add(T item)
        {
            _bag.Add(item);
        }

        public T[] Flush()
        {
            if (_bag.IsEmpty)
                return EmptyArray;

            var oldBag = Interlocked.Exchange(ref _bag, new ConcurrentBag<T>());
            return oldBag.ToArray();
        }
    }
}
