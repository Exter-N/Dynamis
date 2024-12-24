using System.Collections;
using System.Globalization;

namespace Dynamis.Utility;

public record CommandArguments(string[] Arguments) : IReadOnlyList<string>
{
    public int Count
        => Arguments.Length;

    public string this[int index]
        => Arguments[index];

    public bool Equals(int index, string? arg, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        => arg is null
            ? Arguments.Length <= index
            : Arguments.Length > index && arg.Equals(Arguments[index], comparisonType);

    public bool Equals(int index, params string?[]? args)
        => args?.Any(arg => Equals(index, arg)) ?? Arguments.Length <= index;

    public bool Equals(int index, StringComparison comparisonType, params string?[] args)
        => args.Any(arg => Equals(index, arg, comparisonType));

    public bool TryGetInteger(int index, out int value)
    {
        if (Arguments.Length <= index) {
            value = 0;
            return false;
        }

        var arg = Arguments[index];
        return arg.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? int.TryParse(arg.AsSpan(2..), NumberStyles.HexNumber, null, out value)
            : int.TryParse(arg,             out value);
    }

    public IEnumerator<string> GetEnumerator()
        => ((IEnumerable<string>)Arguments).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => Arguments.GetEnumerator();

    public static (CommandArguments Positional, Dictionary<string, CommandArguments> Named) ParseWithNamed(string args)
    {
        var positionalArgs = new List<string>();
        var namedArgs = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var arg in new CommandLexer(args)) {
            if (arg.StartsWith("--")) {
                var kvs = arg.IndexOf('=');
                if (kvs >= 0) {
                    if (!namedArgs.TryGetValue(arg[2..kvs], out var named)) {
                        named = [];
                        namedArgs.Add(arg[2..kvs], named);
                    }

                    named.Add(arg[(kvs + 1)..]);
                } else {
                    if (!namedArgs.TryGetValue(arg[2..], out var named)) {
                        named = [];
                        namedArgs.Add(arg[2..], named);
                    }

                    named.Add(string.Empty);
                }
            } else {
                positionalArgs.Add(arg);
            }
        }

        return (
            positionalArgs.ToArray(),
            namedArgs.ToDictionary(
                kvp => kvp.Key, kvp => new CommandArguments(kvp.Value.ToArray()), StringComparer.OrdinalIgnoreCase
            )
        );
    }

    public static implicit operator CommandArguments(string[] args)
        => new(args);

    public static implicit operator string[](CommandArguments args)
        => args.Arguments;
}
