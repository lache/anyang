using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Core
{
    public class MultiDictionary<TKey, TValue> : Dictionary<TKey, List<TValue>>
    {
        public MultiDictionary()
        {
        }

        public MultiDictionary(IDictionary<TKey, List<TValue>> other)
            : base(other)
        {
        }

        public void Add(TKey key, TValue value)
        {
            if (!ContainsKey(key))
                Add(key, new List<TValue>());

            base[key].Add(value);
        }

        public void Remove(TKey key, TValue value)
        {
            if (ContainsKey(key))
                base[key].Remove(value);
        }
    }
}
