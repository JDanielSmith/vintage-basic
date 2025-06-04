namespace VintageBasic.Interpreter;

sealed class JumpTableEntry(int label, Action programAction, IReadOnlyList<string> data)
{
    public int Label { get; } = label;
    public Action ProgramAction { get; } = programAction ?? throw new ArgumentNullException(nameof(programAction));
	public IReadOnlyList<string> Data { get; } = data ?? throw new ArgumentNullException(nameof(data)); // Data strings from DATA statements on this line
}
