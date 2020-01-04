using System.Collections.Generic;

namespace LicenseGatherer.Core
{
    public static class DictionaryExtensions
    {
        public static void SafeAddRange<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
            IEnumerable<KeyValuePair<TKey, TValue>> entries) where TKey : notnull
        {
            foreach (var (key, value) in entries)
            {
                dictionary[key] = value;
            }
        }
    }
}
