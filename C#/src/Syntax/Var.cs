using System.Collections.Frozen;
using VintageBasic.Parsing;
using VintageBasic.Runtime;
namespace VintageBasic.Syntax;

abstract record Var(string Name, Type Type)
{
	public Var(VarNameToken token) : this(token.Name, token.Type) { }
	public Var(VarName name) : this(name.Name, name.Type) { }

	internal object CoerceToType(object value, int? lineNumber = null, StateManager? stateManager = null)
	{
		return Type.CoerceToType(value, lineNumber, stateManager);
	}
	internal abstract object GetVar(Interpreter.Interpreter interpreter);
	internal abstract void SetVar(Interpreter.Interpreter interpreter, object value);

	static readonly FrozenDictionary<Type, object> defaultValues = new Dictionary<Type, object>() {
		{typeof(int), default(int) }, {typeof(float), default(float) }, {typeof(string), String.Empty } }.ToFrozenDictionary();
	internal static object GetDefaultValue(Type type) => defaultValues.TryGetValue(type, out var value) ? value : throw new ArgumentException($"Unknown object type: {type.Name}");

	internal static bool Equals(VarName lhs, VarName rhs)
	{
		static string GetVarName(VarName varName)
		{
			// Variables are 1) case-insensitive, and 2) unique in only the first two characters.
			return varName.Name[..Math.Min(2, varName.Name.Length)].ToUpperInvariant();
		}
		return GetVarName(lhs) == GetVarName(rhs);
	}
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
