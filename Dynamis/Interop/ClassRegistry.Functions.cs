using System.Collections;
using Iced.Intel;

namespace Dynamis.Interop;

public sealed partial class ClassRegistry
{
    public ClassInfo GetFunctionClass(nint functionAddress, bool safeReads)
    {
        var fnClassName = DetermineClassName(new(ClassIdentifierKind.Function, functionAddress));
        ClassInfo? classInfo;
        lock (_classCache) {
            if (_classCache.TryGetValue(fnClassName, out classInfo)) {
                return classInfo;
            }

            var body = GetFunctionBody(functionAddress, safeReads);
            ConnectJumps(body.Instructions);
            AllocateJumpDisplayColumns(body.Instructions);
            AddAnnotations(body.Instructions);

            classInfo = new ClassInfo
            {
                Name = fnClassName,
                Kind = ClassKind.Function,
                EstimatedSize = (uint)body.Size,
                FunctionBody = body.Instructions,
            };

            classInfo.SizeFromContext = classInfo.EstimatedSize;

            _classCache.Add(fnClassName, classInfo);
        }

        return classInfo;
    }

    private (FunctionInstruction[] Instructions, nint Size) GetFunctionBody(nint functionAddress, bool safeReads)
    {
        var codeReader = new ExecutableMemoryCodeReader(functionAddress, safeReads ? ipfd : null);
        var decoder = codeReader.CreateDecoder();
        var instructions = MemoryHeuristics.GetFunctionInstructions(decoder)
                                           .Select(instr => new FunctionInstruction(instr, GetMemoryOperand(instr)))
                                           .ToArray();

        return (instructions, unchecked((nint)decoder.IP - functionAddress));
    }

    private static void ConnectJumps(FunctionInstruction[] body)
    {
        var addresses = new Dictionary<nint, int>();
        for (var i = 0; i < body.Length; ++i) {
            addresses.Add(unchecked((nint)body[i].Instruction.IP), i);
        }

        for (var i = 0; i < body.Length; ++i) {
            ref var fInstr = ref body[i];
            if (fInstr.MemoryOperand is
                {
                    WillExecute: true,
                    Address: var address,
                } && addresses.TryGetValue(address, out var j)) {
                fInstr.LocalJump = (j, -1);
            }
        }
    }

    private static void AllocateJumpDisplayColumns(FunctionInstruction[] body)
    {
        static int JumpFromHalfRow((int From, int, bool) jump)
            => (jump.From << 1) | 1;

        static int JumpToHalfRow((int, int To, bool) jump)
            => jump.To << 1;

        static int JumpLength((int From, int To, bool) jump)
            => Math.Abs(JumpToHalfRow(jump) - JumpFromHalfRow(jump));

        var jumps = new List<(int From, int To, bool Allocated)>();
        for (var i = 0; i < body.Length; ++i) {
            if (body[i].LocalJump is
                {
                } jump) {
                jumps.Add((i, jump.Target, false));
            }
        }

        jumps.Sort(
            (lhs, rhs) =>
            {
                var lengthCmp = JumpLength(lhs).CompareTo(JumpLength(rhs));
                return lengthCmp != 0 ? lengthCmp : lhs.From.CompareTo(rhs.From);
            }
        );

        var currentColumn = 0;
        var freeHalfRows = new BitArray(body.Length << 1, true);
        while (jumps.Count > 0) {
            for (var i = 0; i < jumps.Count; ++i) {
                var jump = jumps[i];
                var fromHr = JumpFromHalfRow(jump);
                var toHr = JumpToHalfRow(jump);
                var minHr = Math.Min(fromHr, toHr);
                var maxHr = Math.Max(fromHr, toHr);
                var isFree = true;
                for (var hr = minHr; hr <= maxHr; ++hr) {
                    if (!freeHalfRows[hr]) {
                        isFree = false;
                        break;
                    }
                }

                if (!isFree) {
                    continue;
                }

                jumps[i] = (jump.From, jump.To, true);
                body[jump.From].LocalJump = (jump.To, currentColumn);

                for (var hr = minHr; hr <= maxHr; ++hr) {
                    freeHalfRows[hr] = false;
                }
            }
            jumps.RemoveAll(jump => jump.Allocated);
            ++currentColumn;
            freeHalfRows.SetAll(true);
        }
    }

    private void AddAnnotations(FunctionInstruction[] body)
    {
        for (var i = 0; i < body.Length; ++i) {
            ref var fInstr = ref body[i];
            if (fInstr.LocalJump.HasValue) {
                continue;
            }

            if (fInstr.MemoryOperand is
                {
                } operand) {
                var operandAddressId = addressIdentifier.Identify(
                    operand.Address, operand.WillExecute ? AddressType.Function : AddressType.All
                );
                fInstr.Annotation = operandAddressId.GetFullName();
            }
        }
    }

    private static (nint Address, bool WillExecute)? GetMemoryOperand(in Instruction instr)
    {
        if (instr.FlowControl is FlowControl.UnconditionalBranch or FlowControl.ConditionalBranch or FlowControl.Call) {
            return (unchecked((nint)instr.NearBranchTarget), true);
        }

        for (var i = 0; i < instr.OpCount; ++i) {
            if (instr.GetOpKind(i) == OpKind.Memory && instr.MemoryBase is Register.None or Register.RIP) {
                return (unchecked((nint)instr.MemoryDisplacement64), false);
            }
        }

        return null;
    }
}
