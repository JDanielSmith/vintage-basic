using System;
using System.Collections.Frozen;
using VintageBasic.Parsing;
using VintageBasic.Runtime;

namespace VintageBasic.Syntax;

sealed record VarName(string Name, Type Type)
{
	public VarName(VarNameToken token) : this(token.Name, token.Type) { }

	public string Suffix => GetSuffix(Type);

	public static VarName Create<T>(string name)
	{
		return new(name, typeof(T));	
	}

	static readonly FrozenDictionary<Type, string> suffixForType = new Dictionary<Type, string>() { { typeof(int), "%" }, { typeof(float), "" }, { typeof(string), "$" } }.ToFrozenDictionary();
	public static string GetSuffix(Type type) => suffixForType.TryGetValue(type, out var suffix) ? suffix : throw new ArgumentException($"Unknown object type: {type.Name}");

	public override string ToString() => $"{Name}{Suffix}";
}
