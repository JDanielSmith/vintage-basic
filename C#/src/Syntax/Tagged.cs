namespace VintageBasic.Syntax;

sealed record Tagged<T>(SourcePosition Position, T Value)
{
	public override string ToString() => $"Tagged({Position}, {Value})";
}

sealed record SourcePosition(int Line, int Column)
{
	public override string ToString() => $"({Line},{Column})";
}