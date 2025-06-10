using VintageBasic.Runtime;
namespace VintageBasic.Syntax;

abstract record Var(VarName Name)
{
	internal object CoerceToType(object value, int? lineNumber = null, StateManager? stateManager = null)
	{
		return Name.CoerceToType(value, lineNumber, stateManager);
	}
}

sealed record ScalarVar(VarName VarName) : Var(VarName)
{
	public override string ToString() => $"ScalarVar({VarName})";
}

sealed record ArrVar(VarName VarName, IReadOnlyList<Expression> Dimensions) : Var(VarName)
{
	public override string ToString() => $"ArrVar({VarName}, [{String.Join(", ", Dimensions.Select(d => d.ToString()))}])";
}
