namespace VintageBasic.Syntax;

sealed record SourcePosition(int Line, int Column)
{
    public override string ToString() => $"({Line},{Column})";
}

sealed record Tagged<T>(SourcePosition Position, T Value)
{
    public override string ToString() => $"Tagged({Position}, {Value})";
}
