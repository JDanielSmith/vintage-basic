using VintageBasic.Runtime;
namespace VintageBasic.Syntax;

abstract record Var(VarName Name)
{
	internal object CoerceToType(object value, int? lineNumber = null, StateManager? stateManager = null)
	{
		return Name.CoerceToType(value, lineNumber, stateManager);
	}
	internal abstract object GetVar(Interpreter.Interpreter interpreter);
	internal abstract void SetVar(Interpreter.Interpreter interpreter, object value);
}

sealed record ScalarVar(VarName VarName) : Var(VarName)
{
	public override string ToString() => $"ScalarVar({VarName})";
	internal override object GetVar(Interpreter.Interpreter interpreter) => interpreter._interpreterContext.VariableManager.GetScalarVar(VarName);
	internal override void SetVar(Interpreter.Interpreter interpreter, object value) => interpreter._interpreterContext.VariableManager.SetScalarVar(VarName, value);
}

sealed record ArrVar(VarName VarName, IReadOnlyList<Expression> Dimensions) : Var(VarName)
{
	public override string ToString() => $"ArrVar({VarName}, [{String.Join(", ", Dimensions.Select(d => d.ToString()))}])";
	internal override object GetVar(Interpreter.Interpreter interpreter)
	{
		var indices = interpreter.EvaluateIndices(Dimensions, interpreter.StateManager.CurrentLineNumber);
		return interpreter._interpreterContext.VariableManager.GetArrayVar(VarName, indices);
	}
	internal override void SetVar(Interpreter.Interpreter interpreter, object value)
	{
		var indices = interpreter.EvaluateIndices(Dimensions, interpreter.StateManager.CurrentLineNumber);
		interpreter._interpreterContext.VariableManager.SetArrayVar(VarName, indices, value);
	}
}
