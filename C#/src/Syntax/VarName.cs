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

	VarName(string name, Type type)
	{
		ArgumentOutOfRangeException.ThrowIfNullOrWhiteSpace(name, nameof(name));
		var _ = suffixForType[type]; // validate
		Type = type;
		Name = name;
	}
	public VarName(VarNameToken token) : this(token.Name, token.TypeSuffix) { }
	public VarName(string name, char? suffix) : this(name, GetType(suffix)) { }

	static Type GetType(char? suffix) => typeForSuffix[suffix.HasValue ? suffix.Value.ToString() : ""];

	public string Name { get; init; }
	public Type Type { get; init; }
	public string Suffix => suffixForType[Type];

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

	internal object CoerceToType(object value, int? lineNumber = null, StateManager? stateManager = null) =>
		CoerceToType(Type, value, lineNumber, stateManager);
	internal static object CoerceToType(Type targetType, object value, int? lineNumber = null, StateManager? stateManager = null)
	{
		if (stateManager is not null && lineNumber.HasValue)
			stateManager.SetCurrentLineNumber(lineNumber.Value);
		if (targetType == value.GetType()) return value;
		if (targetType == typeof(object)) return value; // Allow object as a generic target type
		if (targetType == typeof(float)) return value.AsFloat(lineNumber);
		if (targetType == typeof(int)) return value.AsInt(lineNumber);
		if (targetType == typeof(string)) return value;
		throw new TypeMismatchError($"Cannot coerce {value.GetTypeName()} to {targetType}", lineNumber ?? stateManager?.CurrentLineNumber);
	}
}
