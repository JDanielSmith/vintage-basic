// src/VintageBasic/Runtime/StateManager.cs
using System;

namespace VintageBasic.Runtime;

public class StateManager
{
    private readonly BasicState _state;

    public StateManager(BasicState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public int GetCurrentLineNumber()
    {
        return _state.CurrentLineNumber;
    }

    public void SetCurrentLineNumber(int lineNumber)
    {
        // In BASIC, line numbers are positive.
        // Consider adding validation if necessary, though the parser/lexer should ensure valid line numbers.
        _state.CurrentLineNumber = lineNumber;
    }
}
