namespace Dynamis.Configuration;

public static class EnumExtensions
{
    public static string Label(this SymbolHandlerMode mode)
        => mode switch
        {
            SymbolHandlerMode.Disable => "Disable",
            SymbolHandlerMode.Default => "Default",
            SymbolHandlerMode.ForceInitialize => "Force Initialize",
            _ => throw new ArgumentException($"Invalid symbol handler mode {mode}", nameof(mode)),
        };
}
