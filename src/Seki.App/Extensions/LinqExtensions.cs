using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seki.App.Extensions
{
    public static class LinqExtensions
    {
        public static TOut? Get<TOut, TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TOut? defaultValue = default)
        {
            if (dictionary is null || key is null)
                return defaultValue;

            if (!dictionary.ContainsKey(key))
            {
                if (defaultValue is TValue value)
                    dictionary.Add(key, value);

                return defaultValue;
            }

            if (dictionary[key] is TOut o)
                return o;

            return defaultValue;
        }
    }
}
