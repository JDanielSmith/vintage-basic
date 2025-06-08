using VintageBasic.Runtime;
namespace VintageBasic.Syntax;

abstract record Var(VarName Name)
{
    internal Type GetValType() => Name.GetValType();
}

sealed record ScalarVar(VarName VarName) : Var(VarName)
{
    public override string ToString() => $"ScalarVar({VarName})";
}

sealed record ArrVar(VarName VarName, IReadOnlyList<Expression> Dimensions) : Var(VarName)
{
    public override string ToString() => $"ArrVar({VarName}, [{String.Join(", ", Dimensions.Select(d => d.ToString()))}])";
}
