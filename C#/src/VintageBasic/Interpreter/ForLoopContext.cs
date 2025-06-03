// src/VintageBasic/Interpreter/ForLoopContext.cs
using VintageBasic.Syntax;
using VintageBasic.Runtime;

namespace VintageBasic.Interpreter;

sealed class ForLoopContext
{
    public VarName LoopVariable { get; }
    public Val LimitValue { get; }
    public Val StepValue { get; }
    public int LoopStartLineIndex { get; } // Index in _jumpTable of the statement AFTER the FOR statement

    public bool SingleLine { get; set; } // True if this is a single-line FOR loop (e.g., FOR I = 1 TO 10: NEXT I)

	public ForLoopContext(VarName loopVariable, Val limitValue, Val stepValue, int loopStartLineIndex)
    {
        LoopVariable = loopVariable;
        LimitValue = limitValue;
        StepValue = stepValue;
        LoopStartLineIndex = loopStartLineIndex;
    }
}
