namespace Dynamis.Utility;

public static class EnumExtensions
{
    public static string ToShortString<T>(this T value) where T : Enum
        => value.ToString().Replace(value.GetType().Name + "_", string.Empty);

    public static string ToShortString<T>(this T value, string stripPrefix) where T : Enum
        => value.ToString().Replace(stripPrefix, string.Empty);
}
