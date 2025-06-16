using System.Collections.Frozen;
using VintageBasic.Parsing;
using VintageBasic.Runtime;
using VintageBasic.Runtime.Errors;

namespace VintageBasic.Syntax;

/// <summary>
/// A variable name in BASIC can have a suffix explicitly making the type integer ('%') or string ('$')
/// </summary>
sealed class VarName : IEquatable<VarName>
{
	static readonly FrozenDictionary<Type, string> suffixForType = new Dictionary<Type, string>() { { typeof(int), "%" }, { typeof(float), "" }, { typeof(string), "$" } }.ToFrozenDictionary();
	static readonly FrozenDictionary<string, Type> typeForSuffix = suffixForType.ToFrozenDictionary(kvp => kvp.Value, kvp => kvp.Key);

	internal VarName(string name, Type type)
	{
		ArgumentOutOfRangeException.ThrowIfNullOrWhiteSpace(name, nameof(name));
		_ = suffixForType[type]; // validate
		Type = type;
		Name = name;
	}
	public VarName(VarNameToken token) : this(token.Name, token.TypeSuffix) { }
	public VarName(string name, char? suffix) : this(name, GetType(suffix)) { }

	static Type GetType(char? suffix) => typeForSuffix[suffix.HasValue ? suffix.Value.ToString() : ""];

	public string Name { get; init; }
	public Type Type { get; init; }
	public string Suffix => suffixForType[Type];

	internal static bool IsValidSuffix(char c) => c is '$' or '%';

	public static VarName Create<T>(string name)
	{
		return new(name, typeof(T));
	}

	string GetName()
	{
		// Variables are 1) case-insensitive, and 2) unique in only the first two characters.
		return Name[..Math.Min(2, Name.Length)].ToUpperInvariant();
	}

	public override string ToString() => $"{Name}{Suffix}";

	public bool Equals(VarName? other)
	{
		if (other is null) return false;
		if (ReferenceEquals(this, other)) return true;
		return (GetName() == other.GetName()) && (Type == other.Type);
	}
	public override bool Equals(object? obj)
	{
		return Equals(obj as VarName);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(GetName().GetHashCode(StringComparison.OrdinalIgnoreCase), Type.GetHashCode());
	}

	internal object CoerceToType(object value, int? lineNumber = null, StateManager? stateManager = null) 
	{
		return Type.CoerceToType(value, lineNumber, stateManager);
	}
}
