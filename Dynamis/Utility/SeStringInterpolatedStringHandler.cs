using System.Runtime.CompilerServices;
using System.Text;
using Dalamud.Game.Text.SeStringHandling;

namespace Dynamis.Utility;

[InterpolatedStringHandler]
public ref struct SeStringInterpolatedStringHandler
{
    private readonly SeStringBuilder                               _sb;
    private readonly StringBuilder                                 _text;
    private          StringBuilder.AppendInterpolatedStringHandler _textHandler;

    public SeStringBuilder StringBuilder
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _sb;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SeStringInterpolatedStringHandler(int literalLength, int formattedCount)
        : this(literalLength, formattedCount, new SeStringBuilder())
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SeStringInterpolatedStringHandler(int literalLength, int formattedCount, IFormatProvider? provider)
        : this(literalLength, formattedCount, new(), provider)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SeStringInterpolatedStringHandler(int literalLength, int formattedCount, SeStringBuilder sb)
    {
        _sb = sb;
        _text = new();
        _textHandler = new(literalLength, formattedCount, _text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SeStringInterpolatedStringHandler(int literalLength, int formattedCount, SeStringBuilder sb,
        IFormatProvider? provider)
    {
        _sb = sb;
        _text = new();
        _textHandler = new(literalLength, formattedCount, _text, provider);
    }

    public void Flush()
    {
        if (_text.Length == 0) {
            return;
        }

        _sb.AddText(_text.ToString());
        _text.Length = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string value)
        => _textHandler.AppendLiteral(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(Payload payload)
    {
        Flush();
        _sb.Add(payload);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(BitmapFontIcon icon)
    {
        Flush();
        _sb.AddIcon(icon);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(SeString str)
    {
        Flush();
        _sb.Append(str);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(IEnumerable<Payload> payloads)
    {
        Flush();
        _sb.Append(payloads);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<TValue>(TValue value)
        => _textHandler.AppendFormatted(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<TValue>(TValue value, string? format)
        => _textHandler.AppendFormatted(value, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<TValue>(TValue value, int alignment)
        => _textHandler.AppendFormatted(value, alignment);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<TValue>(TValue value, int alignment, string? format)
        => _textHandler.AppendFormatted(value, alignment, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(scoped ReadOnlySpan<char> value)
        => _textHandler.AppendFormatted(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null)
        => _textHandler.AppendFormatted(value, alignment, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(string? value)
        => _textHandler.AppendFormatted(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(string? value, int alignment = 0, string? format = null)
        => _textHandler.AppendFormatted(value, alignment, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(object? value, int alignment = 0, string? format = null)
        => _textHandler.AppendFormatted(value, alignment, format);
}
