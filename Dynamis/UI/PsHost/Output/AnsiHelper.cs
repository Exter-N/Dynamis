#if WITH_SMA
using System.Text;
using System.Text.RegularExpressions;
using Dynamis.Utility;

namespace Dynamis.UI.PsHost.Output;

public static partial class AnsiHelper
{
    public static string AnsiCodesToSeString(string input)
        => AnsiCode().Replace(input, AnsiPayloadToSeString);

    public static string StripAnsiCodes(string input)
        => AnsiCode().Replace(input, string.Empty);

    [GeneratedRegex(@"\x1B\[([0-9;]+)m")]
    private static partial Regex AnsiCode();

    private static string AnsiPayloadToSeString(Match match)
    {
        try {
            var payload = Array.ConvertAll(match.Groups[1].Value.Split(';'), int.Parse);
            var foreground = 0;
            var background = 0;
            var bold = false;
            var reset = false;
            foreach (var op in payload) {
                switch (op) {
                    case >= 30 and <= 37 or >= 90 and <= 97:
                        foreground = op;
                        break;
                    case >= 40 and <= 47 or >= 100 and <= 107:
                        background = op;
                        break;
                    case 1:
                        bold = true;
                        break;
                    case 0:
                        foreground = 0;
                        background = 0;
                        bold = false;
                        reset = true;
                        break;
                    default:
                        return match.Value;
                }
            }

            if (bold && foreground < 90) {
                foreground += 60;
            }

            var builder = new StringBuilder();
            if (reset) {
                builder.Append("\x02\x48\x02\x01\x03\x02\x49\x02\x01\x03");
            }

            if (foreground != 0) {
                var consoleColor = (ConsoleColor)(foreground >= 90 ? foreground - 82 : foreground - 30);
                builder.Append("\x02\x48\x02");
                builder.Append((char)(consoleColor.ToSeStringColor() + 1));
                builder.Append('\x03');
            }

            if (background != 0) {
                var consoleColor = (ConsoleColor)(background >= 100 ? background - 92 : background - 40);
                builder.Append("\x02\x49\x02");
                builder.Append((char)(consoleColor.ToSeStringColor() + 1));
                builder.Append('\x03');
            }

            return builder.ToString();
        } catch {
            return match.Value;
        }
    }
}
#endif
