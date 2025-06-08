using VintageBasic.Parsing;
using VintageBasic.Runtime;

namespace VintageBasic.Syntax;

sealed record VarName(Val Val, string Name)
{
	public VarName(VarNameToken token) : this(token.Val, token.Name) { }

	internal bool EqualsName(VarName other)
	{
		static string GetVarName(VarName varName)
		{
			// Variables are 1) case-insensitive, and 2) unique in only the first two characters.
			return varName.Name[..Math.Min(2, varName.Name.Length)].ToUpperInvariant();
		}
		return GetVarName(this) == GetVarName(other);
	}

	public static VarName Create<TVal>(string name) where TVal : Val, new()
	{
		TVal val = new();
		return new(val, name);
	}

	internal Val CoerceToType(Val value, int? lineNumber = null, StateManager? stateManager = null)
	{
		return Val.CoerceToType(value, lineNumber, stateManager);
	}
	public override string ToString() => $"{Name}{Val.Suffix}";
}
