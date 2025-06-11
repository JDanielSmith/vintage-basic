using VintageBasic.Runtime;
namespace VintageBasic.Syntax;

abstract record Var(VarName Name)
{
	internal object CoerceToType(object value, int? lineNumber = null, StateManager? stateManager = null)
	{
		return Name.CoerceToType(value, lineNumber, stateManager);
	}
	internal abstract object GetVar(Interpreter.Interpreter interpreter);
}

sealed record ScalarVar(VarName VarName) : Var(VarName)
{
	public override string ToString() => $"ScalarVar({VarName})";
	internal override object GetVar(Interpreter.Interpreter interpreter) => interpreter.VariableManager.GetScalarVar(VarName);
}

sealed record ArrVar(VarName VarName, IReadOnlyList<Expression> Dimensions) : Var(VarName)
{
	public override string ToString() => $"ArrVar({VarName}, [{String.Join(", ", Dimensions.Select(d => d.ToString()))}])";
	internal override object GetVar(Interpreter.Interpreter interpreter)
	{
		var indices = interpreter.EvaluateIndices(Dimensions, interpreter.StateManager.CurrentLineNumber);
		return interpreter.VariableManager.GetArrayVar(VarName, indices);
	}
}
