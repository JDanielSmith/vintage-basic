using VintageBasic.Syntax;
using VintageBasic.Runtime;

namespace VintageBasic.Interpreter;

sealed class ForLoopContext(VarName loopVariable, Val limitValue, Val stepValue, int loopStartLineIndex)
{
    public VarName LoopVariable { get; } = loopVariable ?? throw new ArgumentNullException(nameof(loopVariable));
	public Val LimitValue { get; } = limitValue ?? throw new ArgumentNullException(nameof(limitValue));
	public Val StepValue { get; } = stepValue ?? throw new ArgumentNullException(nameof(stepValue));
    public int LoopStartLineIndex { get; } = loopStartLineIndex; // Index in _jumpTable of the statement AFTER the FOR statement

    public bool SingleLine { get; set; } // True if this is a single-line FOR loop (e.g., FOR I = 1 TO 10: NEXT I)
}
