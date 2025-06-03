// src/VintageBasic/Syntax/VarName.cs
using System;

namespace VintageBasic.Syntax;

sealed class VarName : IEquatable<VarName>
{
    public ValType Type { get; }
    public string Name { get; }

    public VarName(ValType type, string name)
    {
        Type = type;
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public bool Equals(VarName? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Type == other.Type && Name == other.Name;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as VarName);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Name);
    }

    public override string ToString() => $"{Name}{(Type == ValType.StringType ? "$" : (Type == ValType.IntType ? "%" : ""))}"; // Basic-like representation
}
