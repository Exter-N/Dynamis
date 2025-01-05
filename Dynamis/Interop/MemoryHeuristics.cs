using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using Iced.Intel;

namespace Dynamis.Interop;

public sealed class MemoryHeuristics
{
    private readonly Ipfd.Ipfd _ipfd;

    [Signature("E8 ?? ?? ?? ?? 48 C7 04")] private nint _freeMemory = 0;

    [Signature("E8 ?? ?? ?? ?? 48 8B C3 48 83 C4 ?? 5F 5D")]
    private nint _freeMemory2 = 0;

    public MemoryHeuristics(Ipfd.Ipfd ipfd, IGameInteropProvider gameInteropProvider)
    {
        _ipfd = ipfd;
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

    public static IEnumerable<Instruction> GetFunctionInstructions(Decoder decoder)
    {
        var branchTargets = new HashSet<nint>();
        foreach (var instr in decoder) {
            yield return instr;
            if (instr.FlowControl is FlowControl.UnconditionalBranch or FlowControl.ConditionalBranch) {
                branchTargets.Add(unchecked((nint)instr.NearBranchTarget));
            }

            if (instr.FlowControl is FlowControl.UnconditionalBranch or FlowControl.IndirectBranch
                or FlowControl.Return) {
                if (!branchTargets.Contains(unchecked((nint)decoder.IP))) {
                    break;
                }
            }
        }
    }

    public nuint EstimateDisplacementFromVfunc(nint vfunc)
    {
        var codeReader = new ExecutableMemoryCodeReader(vfunc, _ipfd);
        var decoder = codeReader.CreateDecoder();
        using var enumerator = GetFunctionInstructions(decoder).GetEnumerator();
        if (!enumerator.MoveNext()) {
            return 0;
        }

        var instr = enumerator.Current;
        if (instr is not
            {
                OpCode.Mnemonic: Mnemonic.Sub,
                Op0Kind: OpKind.Register,
                Op0Register: Register.RCX,
            }) {
            return 0;
        }

        var displacement = GetImmediateOp1(instr);
        if (!displacement.HasValue) {
            return 0;
        }

        if (!enumerator.MoveNext()) {
            return 0;
        }

        instr = enumerator.Current;
        if (instr.FlowControl is not FlowControl.UnconditionalBranch) {
            return 0;
        }

        return (nuint)displacement.Value + EstimateDisplacementFromVfunc((nint)instr.NearBranchTarget);
    }

    public (uint Size, nuint Displacement)? EstimateSizeAndDisplacementFromDtor(nint dtor)
    {
        if (_freeMemory == 0 && _freeMemory2 == 0) {
            return null;
        }

        var codeReader = new ExecutableMemoryCodeReader(dtor, _ipfd);
        var decoder = codeReader.CreateDecoder();
        var index = -1;
        uint? size = null;
        nuint? displacement = null;
        foreach (var instr in GetFunctionInstructions(decoder)) {
            ++index;
            if (instr is
                {
                    OpCode.Mnemonic: Mnemonic.Mov,
                    Op0Kind: OpKind.Register,
                    Op0Register: Register.EDX,
                }) {
                size = (uint?)GetImmediateOp1(instr);
            }

            if (instr.FlowControl is FlowControl.Call) {
                var callee = (nint)instr.MemoryDisplacement64;
                if (callee == _freeMemory || callee == _freeMemory2) {
                    return size.HasValue
                        ? (size.Value, 0)
                        : null;
                }
            }

            if (index == 0 && instr is
                {
                    OpCode.Mnemonic: Mnemonic.Sub,
                    Op0Kind: OpKind.Register,
                    Op0Register: Register.RCX,
                }) {
                displacement = (nuint?)GetImmediateOp1(instr);
            }

            if (index == 1 && instr.FlowControl is FlowControl.UnconditionalBranch) {
                if (!displacement.HasValue) {
                    return null;
                }

                var originalSizeAndDisplacement = EstimateSizeAndDisplacementFromDtor((nint)instr.NearBranchTarget);
                if (!originalSizeAndDisplacement.HasValue) {
                    return null;
                }

                return (originalSizeAndDisplacement.Value.Size,
                    originalSizeAndDisplacement.Value.Displacement + displacement.Value);
            }
        }

        return null;
    }

    private static ulong? GetImmediateOp1(Instruction instr)
        => instr.Op1Kind switch
        {
            OpKind.Immediate8      => instr.Immediate8,
            OpKind.Immediate8to16  => (ulong)instr.Immediate8to16,
            OpKind.Immediate8to32  => (ulong)instr.Immediate8to32,
            OpKind.Immediate8to64  => (ulong)instr.Immediate8to64,
            OpKind.Immediate16     => instr.Immediate16,
            OpKind.Immediate32     => instr.Immediate32,
            OpKind.Immediate32to64 => (ulong)instr.Immediate32to64,
            OpKind.Immediate64     => instr.Immediate64,
            _                      => null,
        };
}
