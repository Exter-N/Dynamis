using System.Diagnostics.CodeAnalysis;

namespace Dynamis.Utility;

internal static class CollectionExtensions
{
    public static Dictionary<TValue, TKey> Inverse<TKey, TValue>(this Dictionary<TKey, TValue> forward)
        where TKey : notnull where TValue : class
    {
        var reverse = new Dictionary<TValue, TKey>();
        foreach (var (k, v) in forward) {
            reverse.TryAdd(v, k);
        }

        return reverse;
    }

    public static bool TryGetValue<T>(this IReadOnlyDictionary<string, T> dictionary, string key, bool ignoreCase,
        [MaybeNullWhen(false)] out T result)
    {
        if (dictionary.TryGetValue(key, out result)) {
            return true;
        }

        if (!ignoreCase) {
            return false;
        }

        foreach (var (dKey, value) in dictionary) {
            if (string.Equals(key, dKey, StringComparison.OrdinalIgnoreCase)) {
                result = value;
                return true;
            }
        }

        return false;
    }
}
