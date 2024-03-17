using System.Text.RegularExpressions;

namespace Dynamis.Utility;

internal static partial class TextExtensions
{
    [GeneratedRegex(@"(?<=.)(?=[A-Z])")]
    private static partial Regex SubsequentUppercase();

    [GeneratedRegex(@" \s+|(?! )\s+")]
    private static partial Regex NonNormalizedSpaces();

    public static string InsertSpacesBetweenWords(this string str)
        => SubsequentUppercase().Replace(str, " ");

    public static string NormalizeSpaces(this string str)
        => NonNormalizedSpaces().Replace(str, " ");
}
