namespace VintageBasic.Syntax;

abstract record Literal { }

sealed record FloatLiteral(float Value) : Literal
{
    public override string ToString() => $"{nameof(FloatLiteral)}(({Value})";
}

sealed record StringLiteral(string Value) : Literal
{
    public override string ToString() => $"{nameof(StringLiteral)}(\"{Value}\")";
}
