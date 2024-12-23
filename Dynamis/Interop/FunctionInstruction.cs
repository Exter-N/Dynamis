using Iced.Intel;

namespace Dynamis.Interop;

public record struct FunctionInstruction(
    Instruction Instruction,
    (nint Address, bool WillExecute)? MemoryOperand,
    string? Annotation = null,
    (int Target, int DisplayColumn)? LocalJump = null);
