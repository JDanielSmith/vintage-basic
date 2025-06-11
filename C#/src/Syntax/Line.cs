namespace VintageBasic.Syntax;

sealed record Line(int Label, IReadOnlyList<Tagged<Statement>> Statements)
{
	public override string ToString() => $"Line({Label}, [{String.Join("; ", Statements.Select(s => s.ToString()))}])";
}
