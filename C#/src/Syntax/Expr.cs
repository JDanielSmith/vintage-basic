namespace VintageBasic.Syntax;

abstract class Expr
{
    public virtual bool IsPrintSeparator => false;
}

sealed class LitX : Expr
{
    public Literal Value { get; }
    public LitX(Literal value) { Value = value; }
    public override bool Equals(object? obj) => obj is LitX other && Value.Equals(other.Value);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"LitX({Value})";
}

sealed class VarX : Expr
{
    public Var Value { get; }
    public VarX(Var value) { Value = value; }
    public override bool Equals(object? obj) => obj is VarX other && Value.Equals(other.Value);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"VarX({Value})";
}

sealed class FnX : Expr
{
    public VarName FunctionName { get; }
    public IReadOnlyList<Expr> Args { get; }
    public FnX(VarName functionName, IReadOnlyList<Expr> args)
    {
        FunctionName = functionName;
        Args = args;
    }
    public override bool Equals(object? obj) => 
        obj is FnX other && 
        FunctionName.Equals(other.FunctionName) && 
        Args.SequenceEqual(other.Args);
    public override int GetHashCode() => HashCode.Combine(FunctionName, Args.Aggregate(0, (h, arg) => HashCode.Combine(h, arg.GetHashCode())));
    public override string ToString() => $"FnX({FunctionName}, [{String.Join(", ", Args.Select(a => a.ToString()))}])";
}

sealed class MinusX : Expr
{
    public Expr Right { get; }
    public MinusX(Expr right) { Right = right; }
    public override bool Equals(object? obj) => obj is MinusX other && Right.Equals(other.Right);
    public override int GetHashCode() => Right.GetHashCode();
    public override string ToString() => $"MinusX({Right})";
}

sealed class NotX : Expr
{
    public Expr Right { get; }
    public NotX(Expr right) { Right = right; }
    public override bool Equals(object? obj) => obj is NotX other && Right.Equals(other.Right);
    public override int GetHashCode() => Right.GetHashCode();
    public override string ToString() => $"NotX({Right})";
}

sealed class BinX : Expr
{
    public BinOp Op { get; }
    public Expr Left { get; }
    public Expr Right { get; }
    public BinX(BinOp op, Expr left, Expr right)
    {
        Op = op;
        Left = left;
        Right = right;
    }
    public override bool Equals(object? obj) => 
        obj is BinX other && 
        Op == other.Op && 
        Left.Equals(other.Left) && 
        Right.Equals(other.Right);
    public override int GetHashCode() => HashCode.Combine(Op, Left, Right);
    public override string ToString() => $"BinX({Op}, {Left}, {Right})";
}

sealed class BuiltinX : Expr
{
    public Builtin Builtin { get; }
    public IReadOnlyList<Expr> Args { get; }
    public BuiltinX(Builtin builtin, IReadOnlyList<Expr> args)
    {
        Builtin = builtin;
        Args = args;
    }
    public override bool Equals(object? obj) => 
        obj is BuiltinX other && 
        Builtin == other.Builtin && 
        Args.SequenceEqual(other.Args);
    public override int GetHashCode() => HashCode.Combine(Builtin, Args.Aggregate(0, (h, arg) => HashCode.Combine(h, arg.GetHashCode())));
    public override string ToString() => $"BuiltinX({Builtin}, [{String.Join(", ", Args.Select(a => a.ToString()))}])";
}

sealed class NextZoneX : Expr
{
    public override bool IsPrintSeparator => true;
    public override bool Equals(object? obj) => obj is NextZoneX;
    public override int GetHashCode() => typeof(NextZoneX).GetHashCode();
    public override string ToString() => "NextZoneX";
}

sealed class EmptySeparatorX : Expr
{
    public override bool IsPrintSeparator => true;
    public override bool Equals(object? obj) => obj is EmptySeparatorX;
    public override int GetHashCode() => typeof(EmptySeparatorX).GetHashCode();
    public override string ToString() => "EmptySeparatorX";
}

sealed class ParenX : Expr
{
    public Expr Inner { get; }
    public ParenX(Expr inner) { Inner = inner; }
    public override bool Equals(object? obj) => obj is ParenX other && Inner.Equals(other.Inner);
    public override int GetHashCode() => Inner.GetHashCode();
    public override string ToString() => $"ParenX({Inner})";
}
