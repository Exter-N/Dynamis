using System.Runtime.InteropServices;

namespace Dynamis.Interop.Win32;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct SymbolInfo
{
    public        uint  SizeOfStruct;
    public        uint  TypeIndex;
    private fixed ulong Reserved[2];
    public        uint  Index;
    public        uint  Size;
    public        ulong ModBase;
    public        uint  Flags;
    public        ulong Value;
    public        ulong Address;
    public        uint  Register;
    public        uint  Scope;
    public        uint  Tag;
    public        uint  NameLen;
    public        uint  MaxNameLen;
    public fixed  char  Name[1];
}
