namespace VintageBasic.Syntax;

abstract record Literal(ValType Type) { }

sealed record FloatLiteral(float Value) : Literal(ValType.FloatType)
{
    public override string ToString() => $"{nameof(FloatLiteral)}(({Value})";
}

sealed record StringLiteral(string Value) : Literal(ValType.StringType)
{
    public override string ToString() => $"{nameof(StringLiteral)}(\"{Value}\")";
}
