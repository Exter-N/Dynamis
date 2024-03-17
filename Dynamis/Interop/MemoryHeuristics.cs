using System.Runtime.CompilerServices;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using Dynamis.Interop.Win32;

namespace Dynamis.Interop;

public sealed class MemoryHeuristics
{
    [Signature("E8 ?? ?? ?? ?? 48 C7 04")] private nint _freeMemory = 0;

    [Signature("E8 ?? ?? ?? ?? 48 8B C3 48 83 C4 ?? 5F 5D")]
    private nint _freeMemory2 = 0;

    public MemoryHeuristics(IGameInteropProvider gameInteropProvider)
    {
        gameInteropProvider.InitializeFromAttributes(this);
    }

    public static nint PageBase(nint address)
    {
        var pageSize = (nint)Environment.SystemPageSize;
        return address & ~(pageSize - 1);
    }

    public static nint NextPage(nint address)
    {
        var pageSize = (nint)Environment.SystemPageSize;
        return (address + pageSize) & ~(pageSize - 1);
    }

    public unsafe uint? EstimateSizeFromDtor(nint dtor)
    {
        if (_freeMemory == 0 && _freeMemory2 == 0) {
            return null;
        }

        var pageSize = (nint)Environment.SystemPageSize;
        if (!VirtualMemory.TryQuery(dtor & ~(pageSize - 1), out var pageInfo)) {
            return null;
        }

        if (!pageInfo.State.HasFlag(MemoryState.Commit) || !pageInfo.Protect.CanRead()
                                                        || !pageInfo.Protect.CanExecute()) {
            return null;
        }

        var endOfPage = (dtor + pageSize) & ~(pageSize - 1);
        var searchPtr = (byte*)dtor.ToPointer();
        var restOfPage = new ReadOnlySpan<byte>(searchPtr, (endOfPage - dtor).ToInt32());
        int candidatePos;
        while (restOfPage.Length >= 10 && (candidatePos = restOfPage.IndexOf((byte)0xBA)) >= 0) {
            searchPtr += candidatePos + 1;
            restOfPage = restOfPage[(candidatePos + 1)..];
            if (restOfPage.Length < 9) {
                break;
            }

            nint calledFunction;
            if (restOfPage[4] == 0xE8) {
                calledFunction = new(searchPtr + 9 + Unsafe.ReadUnaligned<int>(searchPtr + 5));
            } else if (IsMovToRcx(restOfPage[4..7]) && restOfPage[7] == 0xE8 && restOfPage.Length >= 12) {
                calledFunction = new(searchPtr + 12 + Unsafe.ReadUnaligned<int>(searchPtr + 8));
            } else {
                continue;
            }

            if (_freeMemory != 0 && calledFunction == _freeMemory || _freeMemory2 != 0 && calledFunction == _freeMemory2) {
                return Unsafe.ReadUnaligned<uint>(searchPtr);
            }
        }

        return null;
    }

    private static bool IsMovToRcx(ReadOnlySpan<byte> insn)
        => insn[1] switch
        {
            0x89 => (insn[0] & 0xFB) == 0x48 && (insn[2] & 0xC7) == 0xC1,
            0x8B => (insn[0] & 0xFE) == 0x48 && (insn[2] & 0xF8) == 0xC8,
            _    => false,
        };
}
