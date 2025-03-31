namespace Dynamis.Interop;

public record ModuleAddress(string ModuleName, string? SymbolName, nint Displacement, nint OriginalAddress)
{
    public override string ToString()
        => SymbolName is not null
            ? Displacement != 0
                ? $"{ModuleName}!{SymbolName}+{Displacement:X}"
                : $"{ModuleName}!{SymbolName}"
            : $"{ModuleName}+{Displacement:X}";
}
