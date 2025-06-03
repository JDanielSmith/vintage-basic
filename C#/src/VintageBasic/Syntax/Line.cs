// src/VintageBasic/Syntax/Line.cs
using System.Collections.Generic;
using System.Linq;

namespace VintageBasic.Syntax;

sealed class Line
{
    public int Label { get; }
    public IReadOnlyList<Tagged<Statement>> Statements { get; }

    public Line(int label, IReadOnlyList<Tagged<Statement>> statements)
    {
        Label = label;
        Statements = statements;
    }

    public override bool Equals(object? obj) => 
        obj is Line other && 
        Label == other.Label && 
        Statements.SequenceEqual(other.Statements);

    public override int GetHashCode() => HashCode.Combine(Label, Statements.Count); // Simplified hash code

    public override string ToString() => $"Line({Label}, [{String.Join("; ", Statements.Select(s => s.ToString()))}])";
}
