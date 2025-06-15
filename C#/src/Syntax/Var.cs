using System.Collections.Frozen;
using VintageBasic.Interpreter;
using VintageBasic.Parsing;
using VintageBasic.Runtime;
namespace VintageBasic.Syntax;

abstract record Var(string Name, Type Type)
{
	public Var(VarName name) : this(name.Name, name.Type) { }

	protected object CoerceToType(object value, Interpreter.Interpreter interpreter)
	{
		var stateManager = interpreter._interpreterContext.StateManager;
		var currentBasicLine = stateManager.CurrentLineNumber;
		return VarName.CoerceToType(Type,value, currentBasicLine, stateManager);
	}
	internal abstract object GetVar(Interpreter.Interpreter interpreter);
	internal abstract void SetVar(Interpreter.Interpreter interpreter, object value);

	static readonly FrozenDictionary<Type, object> defaultValues = new Dictionary<Type, object>() {
		{typeof(int), default(int) }, {typeof(float), default(float) }, {typeof(string), String.Empty } }.ToFrozenDictionary();
	internal static object GetDefaultValue(Type type) => defaultValues.TryGetValue(type, out var value) ? value : throw new ArgumentException($"Unknown object type: {type.Name}");
}

sealed record ScalarVar(VarName VarName) : Var(VarName)
{
	public override string ToString() => $"ScalarVar({VarName})";
	internal override object GetVar(Interpreter.Interpreter interpreter) => interpreter._interpreterContext.VariableManager.GetScalarVar(VarName);
	internal override void SetVar(Interpreter.Interpreter interpreter, object value)
	{
		var coercedValue = CoerceToType(value, interpreter);
		interpreter._interpreterContext.VariableManager.SetScalarVar(VarName, coercedValue);
	}
}

sealed record ArrVar(VarName VarName, IEnumerable<Expression> Dimensions) : Var(VarName)
{
	public override string ToString() => $"ArrVar({VarName}, [{String.Join(", ", Dimensions.Select(d => d.ToString()))}])";
	internal override object GetVar(Interpreter.Interpreter interpreter)
	{
		var indices = interpreter.EvaluateIndices(Dimensions, interpreter.StateManager.CurrentLineNumber);
		return interpreter._interpreterContext.VariableManager.GetArrayVar(VarName, indices);
	}
	internal override void SetVar(Interpreter.Interpreter interpreter, object value)
	{
		var coercedValue = CoerceToType(value, interpreter);

		var stateManager = interpreter._interpreterContext.StateManager;
		var indices = interpreter.EvaluateIndices(Dimensions, stateManager.CurrentLineNumber);
		interpreter._interpreterContext.VariableManager.SetArrayVar(VarName, indices, coercedValue);
	}
}
