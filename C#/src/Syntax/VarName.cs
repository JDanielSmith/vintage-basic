using VintageBasic.Parsing;
using VintageBasic.Runtime;

namespace VintageBasic.Syntax;

sealed record VarName(Object Val, string Name)
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
	public static VarName CreateFloat(string name)
	{
		return new(0.0f, name);
	}
	public static VarName CreateInt(string name)
	{
		return new(0, name);
	}
	public static VarName CreateString(string name)
	{
		return new(String.Empty, name);
	}

	internal object GetDefaultValue() => Val switch
	{
		float => 0.0f,
		int => 0,
		string => String.Empty,
		_ => throw new ArgumentException($"Unknown object type: {Val.GetType().Name}")
	};

	internal object CoerceToType(Object value, int? lineNumber = null, StateManager? stateManager = null)
	{
		return Val.CoerceToType(value, lineNumber, stateManager);
	}
	public override string ToString() => $"{Name}{Val.GetSuffix()}";
}
