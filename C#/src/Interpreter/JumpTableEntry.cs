namespace VintageBasic.Interpreter;

sealed record JumpTableEntry(int Label, Action ProgramAction, IReadOnlyList<string> Data) { }
