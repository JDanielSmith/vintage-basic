// src/VintageBasic/Syntax/Literal.cs
namespace VintageBasic.Syntax;

abstract class Literal
{
    public abstract ValType Type { get; }
}

sealed class FloatLiteral : Literal
{
    public float Value { get; }

    public FloatLiteral(float value)
    {
        Value = value;
    }

    public override ValType Type => ValType.FloatType;

    public override bool Equals(object? obj) => obj is FloatLiteral other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"FloatLit({Value})";

}

sealed class StringLiteral : Literal
{
    public string Value { get; }

    public StringLiteral(string value)
    {
        Value = value;
    }

    public override ValType Type => ValType.StringType;

    public override bool Equals(object? obj) => obj is StringLiteral other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"StringLit(\"{Value}\")";
}
