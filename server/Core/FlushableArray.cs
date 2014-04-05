using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Core
{
    public class FlushableArray<T>
    {
        private ConcurrentBag<T> _bag = new ConcurrentBag<T>();

        public void Add(T item)
        {
            _bag.Add(item);
        }

        public IEnumerable<T> Flush()
        {
            if (_bag.IsEmpty)
                return Enumerable.Empty<T>();

            var oldBag = Interlocked.Exchange(ref _bag, new ConcurrentBag<T>());
            return oldBag.ToArray();
        }
    }
}
