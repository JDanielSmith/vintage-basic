namespace VintageBasic.Syntax;

abstract record Var(ValType Type, VarName Name) { }

sealed record ScalarVar(VarName VarName) : Var(VarName.Type, VarName)
{
    public override string ToString() => $"ScalarVar({VarName})";
}

sealed record ArrVar(VarName VarName, IReadOnlyList<Expression> Dimensions) : Var(VarName.Type, VarName)
{
    public override string ToString() => $"ArrVar({VarName}, [{String.Join(", ", Dimensions.Select(d => d.ToString()))}])";
}
