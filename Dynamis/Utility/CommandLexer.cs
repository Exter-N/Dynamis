using System.Globalization;
using System.Text;

namespace Dynamis.Utility;

public ref struct CommandLexer(ReadOnlySpan<char> command)
{
    private ReadOnlySpan<char> _rest = command;

    public string Current { get; private set; } = string.Empty;

    public CommandLexer GetEnumerator()
        => this;

    public void Reset()
    {
    }

    public bool MoveNext()
    {
        if (_rest.IsEmpty) {
            Current = string.Empty;
            return false;
        }

        var sb = new StringBuilder();
        var inDoubleQuotes = false;
        var inSingleQuotes = false;
        var forceNonEmpty = false;
        for (var i = 0; i < _rest.Length; ++i) {
            var ch = _rest[i];
            switch (ch) {
                case ' ':
                    if (inDoubleQuotes || inSingleQuotes) {
                        sb.Append(ch);
                    } else if (sb.Length > 0 || forceNonEmpty) {
                        Current = sb.ToString();
                        _rest = _rest[(i + 1)..];
                        return true;
                    }

                    break;
                case '"':
                    if (inSingleQuotes) {
                        sb.Append('"');
                    } else {
                        forceNonEmpty = true;
                        inDoubleQuotes = !inDoubleQuotes;
                    }
                    break;
                case '\'':
                    if (inDoubleQuotes) {
                        sb.Append('\'');
                    } else {
                        forceNonEmpty = true;
                        inSingleQuotes = !inSingleQuotes;
                    }
                    break;
                case '\\':
                    sb.Append(inSingleQuotes ? '\\' : ParseEscapeSequence(ref i));
                    break;
                default:
                    sb.Append(ch);
                    break;
            }
        }

        Current = sb.ToString();
        _rest = ReadOnlySpan<char>.Empty;
        return Current.Length > 0 || forceNonEmpty;
    }

    private char ParseEscapeSequence(ref int i)
        => i + 1 < _rest.Length
            ? _rest[++i] switch
            {
                '0'    => '\0',
                'a'    => '\a',
                'b'    => '\b',
                'c'    => ParseControlEscapeSequence(ref i),
                'e'    => '\x1b',
                'f'    => '\f',
                'n'    => '\n',
                'r'    => '\r',
                't'    => '\t',
                'u'    => ParseHex4EscapeSequence(ref i),
                'v'    => '\v',
                'x'    => ParseHex2EscapeSequence(ref i),
                var ch => ch,
            }
            : '\\';

    private char ParseControlEscapeSequence(ref int i)
        => i + 1 < _rest.Length && char.IsAsciiLetter(_rest[i + 1])
            ? (char)(_rest[++i] & ~0x60)
            : 'c';

    private char ParseHex2EscapeSequence(ref int i)
    {
        if (i + 2 >= _rest.Length || !byte.TryParse(_rest[(i + 1)..(i + 3)], NumberStyles.HexNumber, null, out var ret)) {
            return 'x';
        }

        i += 2;
        return (char)ret;
    }

    private char ParseHex4EscapeSequence(ref int i)
    {
        if (i + 4 >= _rest.Length || !ushort.TryParse(_rest[(i + 1)..(i + 5)], NumberStyles.HexNumber, null, out var ret)) {
            return 'u';
        }

        i += 4;
        return (char)ret;
    }

    public void Dispose()
    {
    }
}
