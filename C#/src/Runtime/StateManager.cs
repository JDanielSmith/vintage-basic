namespace VintageBasic.Runtime;

sealed class StateManager(BasicState state)
{
    readonly BasicState _state = state;

	public int CurrentLineNumber => _state.CurrentLineNumber;

	public void SetCurrentLineNumber(int lineNumber)
    {
        // In BASIC, line numbers are positive.
        // Consider adding validation if necessary, though the parser/lexer should ensure valid line numbers.
        _state.CurrentLineNumber = lineNumber;
    }
}
