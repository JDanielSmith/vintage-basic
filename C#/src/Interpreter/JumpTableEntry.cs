namespace VintageBasic.Interpreter;

sealed class JumpTableEntry(int label, Action programAction, IReadOnlyList<string> data)
{
    public int Label { get; } = label;
    public Action ProgramAction { get; } = programAction;
	public IReadOnlyList<string> Data { get; } = data; // Data strings from DATA statements on this line
}
