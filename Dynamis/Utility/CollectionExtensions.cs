namespace Dynamis.Utility;

internal static class CollectionExtensions
{
    public static Dictionary<TValue, TKey> Inverse<TKey, TValue>(this Dictionary<TKey, TValue> forward) where TKey : notnull where TValue : class
    {
        var reverse = new Dictionary<TValue, TKey>();
        foreach (var (k, v) in forward) {
            reverse.TryAdd(v, k);
        }

        return reverse;
    }
}
