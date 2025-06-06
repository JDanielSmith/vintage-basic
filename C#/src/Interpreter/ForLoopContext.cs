using VintageBasic.Syntax;
using VintageBasic.Runtime;

namespace VintageBasic.Interpreter;

sealed record ForLoopContext(VarName LoopVariable, Val LimitValue, Val StepValue, int LoopStartLineIndex)
{
    public bool SingleLine { get; set; } // True if this is a single-line FOR loop (e.g., FOR I = 1 TO 10: NEXT I)
}
