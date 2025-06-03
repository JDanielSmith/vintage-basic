namespace VintageBasic.Syntax;

abstract class Var
{
    public abstract ValType Type { get; }
    public abstract VarName Name { get; } // Added for easier access to the name
}

sealed class ScalarVar : Var
{
    public VarName VarName { get; }

    public ScalarVar(VarName varName)
    {
        VarName = varName;
    }

    public override ValType Type => VarName.Type;
    public override VarName Name => VarName;

    public override bool Equals(object? obj) => obj is ScalarVar other && VarName.Equals(other.VarName);
    public override int GetHashCode() => VarName.GetHashCode();
    public override string ToString() => $"ScalarVar({VarName})";
}

sealed class ArrVar : Var
{
    public VarName VarName { get; }
    public IReadOnlyList<Expr> Dimensions { get; }

    public ArrVar(VarName varName, IReadOnlyList<Expr> dimensions)
    {
        VarName = varName;
        Dimensions = dimensions;
    }

    public override ValType Type => VarName.Type;
    public override VarName Name => VarName;

    public override bool Equals(object? obj) => 
        obj is ArrVar other && 
        VarName.Equals(other.VarName) && 
        Dimensions.SequenceEqual(other.Dimensions);

    public override int GetHashCode()
    {
        int hashCode = VarName.GetHashCode();
        foreach (var dim in Dimensions)
        {
            hashCode = HashCode.Combine(hashCode, dim.GetHashCode()); // Not perfect but better than nothing
        }
        return hashCode;
    }

    public override string ToString() => $"ArrVar({VarName}, [{String.Join(", ", Dimensions.Select(d => d.ToString()))}])";
}
