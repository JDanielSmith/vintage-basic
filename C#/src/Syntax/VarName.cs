using VintageBasic.Parsing;
using VintageBasic.Runtime;

namespace VintageBasic.Syntax;

sealed record VarName(Val Val, string Name)
{
	public VarName(VarNameToken token) : this(token.Val, token.Name) { }

	internal Type GetValType() => Val.GetType();

	internal Val DefaultValue => Val.DefaultValue;
	internal string Suffix => Val.Suffix;

	internal bool IsSameType(Val val)
	{
		return GetValType() == val.GetType();
	}
	internal bool IsSameType(VarName varName)
	{
		return GetValType() == varName.GetValType();
	}

	public static VarName Create<TVal>(string name) where TVal : Val, new()
	{
		TVal val = new();
		return new(val, name);
	}

	internal Val CoerceToType(Val value, int? lineNumber = null, StateManager? stateManager = null)
	{
		return Val.CoerceToType(GetValType(), value, lineNumber, stateManager);
	}

	public override string ToString() => $"{Name}{Suffix}";
}
