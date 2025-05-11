using System.Text;

namespace Dynamis.Utility;

public static class StringExtensions
{
    public static IEnumerable<(string Line, bool AppendEndOfLine)> Lines(this string text, bool appendEndOfLine = false)
    {
        var start = 0;
        int next;
        while ((next = text.IndexOf('\n', start)) != -1) {
            var end = next;
            if (end > 0 && text[end] == '\r') {
                --end;
            }

            yield return (text[start..end], true);
            start = next + 1;
        }

        if (start < text.Length || appendEndOfLine) {
            yield return (start == 0 ? text : text[start..], appendEndOfLine);
        }
    }

    public static (string Label, int AcceleratorPosition) ParseAccelerator(this string label)
    {
        var span = label.AsSpan();
        var pos = span.IndexOf('&');
        if (pos < 0) {
            return (label, -1);
        }

        var accelerator = -1;
        var labelBuilder = new StringBuilder();
        do {
            labelBuilder.Append(span[..pos]);
            span = span[pos..];

            var ampersands = span.IndexOfAnyExcept('&');
            if (ampersands == -1) {
                ampersands = span.Length;
            }

            if ((ampersands & 1) != 0 && accelerator == -1) {
                accelerator = labelBuilder.Length - span.Length;
            }

            if (ampersands >= 2) {
                labelBuilder.Append('&', ampersands >> 1);
            }

            span = span[ampersands..];
            pos = span.IndexOf('&');
        } while (pos >= 0);

        labelBuilder.Append(span);
        return (labelBuilder.ToString(), accelerator);
    }

    public static string EscapePsArgument(this string value)
        => $"'{value.Replace("'", "''").Replace("\u2018", "\u2018\u2018").Replace("\u2019", "\u2019\u2019")}'";

    public static ReadOnlySpan<T> BeforeNull<T>(this ReadOnlySpan<T> span) where T : unmanaged, IEquatable<T>
    {
        var pos = span.IndexOf(default(T));
        return pos >= 0 ? span[..pos] : span;
    }

    public static int WriteNullTerminated(this ReadOnlySpan<char> value, Span<byte> span)
    {
        var byteCount = Encoding.UTF8.GetBytes(value, span);
        if (byteCount == span.Length) {
            while ((span[byteCount - 1] & 0xC0) == 0x80) {
                --byteCount;
            }

            --byteCount;
        }

        span[byteCount] = 0;
        return byteCount;
    }

    public static int WriteNullTerminated(this string value, Span<byte> span)
        => WriteNullTerminated(value.AsSpan(), span);

    public static int WriteNullTerminated(this ReadOnlySpan<char> value, Span<char> span)
    {
        if (value.Length > span.Length) {
            value = value[..span.Length];
        }

        var charCount = value.Length;
        value.CopyTo(span);
        if (charCount == span.Length) {
            while (char.IsLowSurrogate(span[charCount - 1])) {
                --charCount;
            }

            --charCount;
        }

        span[charCount] = '\0';
        return charCount;
    }

    public static int WriteNullTerminated(this string value, Span<char> span)
        => WriteNullTerminated(value.AsSpan(), span);

    public static string AfterLast(this string value, char ch)
    {
        var pos = value.LastIndexOf(ch);
        return pos < 0
            ? value
            : value[(pos + 1)..];
    }

    public static string AfterLast(this string value, string substr)
    {
        var pos = value.LastIndexOf(substr, StringComparison.Ordinal);
        return pos < 0
            ? value
            : value[(pos + substr.Length)..];
    }
}
