// src/VintageBasic/Syntax/Tagged.cs
namespace VintageBasic.Syntax;

readonly struct SourcePosition
{
    public int Line { get; }
    public int Column { get; }

    public SourcePosition(int line, int column)
    {
        Line = line;
        Column = column;
    }

    public override bool Equals(object? obj) => 
        obj is SourcePosition other && 
        Line == other.Line && 
        Column == other.Column;

    public override int GetHashCode() => HashCode.Combine(Line, Column);
    public override string ToString() => $"({Line},{Column})";
}

sealed class Tagged<T>
{
    public SourcePosition Position { get; }
    public T Value { get; }

    public Tagged(SourcePosition position, T value)
    {
        Position = position;
        Value = value;
    }

    public override bool Equals(object? obj) => 
        obj is Tagged<T> other && 
        Position.Equals(other.Position) && 
        (Value?.Equals(other.Value) ?? other.Value == null);

    public override int GetHashCode() => HashCode.Combine(Position, Value);

    public override string ToString() => $"Tagged({Position}, {Value})";
}
