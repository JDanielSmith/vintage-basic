namespace VintageBasic.Syntax;

abstract record Expression
{
    public virtual bool IsPrintSeparator => false;
}

sealed record LiteralExpression(Literal Value) : Expression
{
    public override string ToString() => $"{nameof(LiteralExpression)}({Value})";
}

sealed record VarExpression(Var Value) : Expression
{
    public override string ToString() => $"{nameof(VarExpression)}({Value})";
}

sealed record FnExpression(VarName FunctionName, IReadOnlyList<Expression> Args) : Expression
{
    public override string ToString() => $"{nameof(FnExpression)}({FunctionName}, [{String.Join(", ", Args.Select(a => a.ToString()))}])";
}

sealed record MinusExpression(Expression Right) : Expression
{
    public override string ToString() => $"{nameof(MinusExpression)}({Right})";
}

sealed record NotExpression(Expression Right) : Expression
{
    public override string ToString() => $"{nameof(NotExpression)}({Right})";
}

sealed record BinOpExpression(BinOp Op, Expression Left, Expression Right) : Expression
{
    public override string ToString() => $"{nameof(BinOpExpression)}({Op}, {Left}, {Right})";
}

sealed record BuiltinExpression(Builtin Builtin, IReadOnlyList<Expression> Args) : Expression
{
    public override string ToString() => $"{nameof(BuiltinExpression)}({Builtin}, [{String.Join(", ", Args.Select(a => a.ToString()))}])";
}

sealed record NextZoneExpression : Expression
{
    public override bool IsPrintSeparator => true;
    public override string ToString() => nameof(NextZoneExpression);
}

sealed record EmptyZoneExpression : Expression
{
    public override bool IsPrintSeparator => true;
    public override string ToString() => nameof(EmptyZoneExpression);
}

sealed record ParenExpression(Expression Inner) : Expression
{
    public override string ToString() => $"{nameof(ParenExpression)}({Inner})";
}
