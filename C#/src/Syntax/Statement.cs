namespace VintageBasic.Syntax;

abstract class Statement { }

sealed class LetStmt : Statement
{
    public Var Variable { get; }
    public Expr Expression { get; }
    public LetStmt(Var variable, Expr expression)
    {
        Variable = variable;
        Expression = expression;
    }
    public override bool Equals(object? obj) => obj is LetStmt other && Variable.Equals(other.Variable) && Expression.Equals(other.Expression);
    public override int GetHashCode() => HashCode.Combine(Variable, Expression);
    public override string ToString() => $"LetStmt({Variable}, {Expression})";
}

sealed class DimStmt : Statement
{
    public IReadOnlyList<(VarName Name, IReadOnlyList<Expr> Dimensions)> Declarations { get; }
    public DimStmt(IReadOnlyList<(VarName, IReadOnlyList<Expr>)> declarations)
    {
        Declarations = declarations;
    }
    public override bool Equals(object? obj) => 
        obj is DimStmt other && 
        Declarations.SequenceEqual(other.Declarations); // Note: This SequenceEqual might need deeper comparison for lists of Expr
    public override int GetHashCode() => Declarations.Aggregate(0, (h, decl) => HashCode.Combine(h, decl.Name, decl.Dimensions.Count));
    public override string ToString() => $"DimStmt([{String.Join(", ", Declarations.Select(d => $"{d.Name}({String.Join(", ", d.Dimensions)})"))}])";
}

sealed class GotoStmt : Statement
{
    public int TargetLabel { get; }
    public GotoStmt(int targetLabel) { TargetLabel = targetLabel; }
    public override bool Equals(object? obj) => obj is GotoStmt other && TargetLabel == other.TargetLabel;
    public override int GetHashCode() => TargetLabel.GetHashCode();
    public override string ToString() => $"GotoStmt({TargetLabel})";
}

sealed class GosubStmt : Statement
{
    public int TargetLabel { get; }
    public GosubStmt(int targetLabel) { TargetLabel = targetLabel; }
    public override bool Equals(object? obj) => obj is GosubStmt other && TargetLabel == other.TargetLabel;
    public override int GetHashCode() => TargetLabel.GetHashCode();
    public override string ToString() => $"GosubStmt({TargetLabel})";
}

sealed class OnGotoStmt : Statement
{
    public Expr Expression { get; }
    public IReadOnlyList<int> TargetLabels { get; }
    public OnGotoStmt(Expr expression, IReadOnlyList<int> targetLabels)
    {
        Expression = expression;
        TargetLabels = targetLabels;
    }
    public override bool Equals(object? obj) => 
        obj is OnGotoStmt other && 
        Expression.Equals(other.Expression) && 
        TargetLabels.SequenceEqual(other.TargetLabels);
    public override int GetHashCode() => HashCode.Combine(Expression, TargetLabels.Count);
    public override string ToString() => $"OnGotoStmt({Expression}, [{String.Join(", ", TargetLabels)}])";
}

sealed class OnGosubStmt : Statement
{
    public Expr Expression { get; }
    public IReadOnlyList<int> TargetLabels { get; }
    public OnGosubStmt(Expr expression, IReadOnlyList<int> targetLabels)
    {
        Expression = expression;
        TargetLabels = targetLabels;
    }
    public override bool Equals(object? obj) => 
        obj is OnGosubStmt other && 
        Expression.Equals(other.Expression) && 
        TargetLabels.SequenceEqual(other.TargetLabels);
    public override int GetHashCode() => HashCode.Combine(Expression, TargetLabels.Count);
    public override string ToString() => $"OnGosubStmt({Expression}, [{String.Join(", ", TargetLabels)}])";
}

sealed class ReturnStmt : Statement
{
    public override bool Equals(object? obj) => obj is ReturnStmt;
    public override int GetHashCode() => typeof(ReturnStmt).GetHashCode();
    public override string ToString() => "ReturnStmt";
}

sealed class IfStmt : Statement
{
    public Expr Condition { get; }
    public IReadOnlyList<Tagged<Statement>> Statements { get; }
    public IfStmt(Expr condition, IReadOnlyList<Tagged<Statement>> statements)
    {
        Condition = condition;
        Statements = statements;
    }
    public override bool Equals(object? obj) => 
        obj is IfStmt other && 
        Condition.Equals(other.Condition) && 
        Statements.SequenceEqual(other.Statements);
    public override int GetHashCode() => HashCode.Combine(Condition, Statements.Count);
    public override string ToString() => $"IfStmt({Condition}, [{String.Join("; ", Statements.Select(s => s.ToString()))}])";
}

sealed class ForStmt : Statement
{
    public VarName LoopVariable { get; }
    public Expr InitialValue { get; }
    public Expr LimitValue { get; }
    public Expr StepValue { get; }
    public ForStmt(VarName loopVariable, Expr initialValue, Expr limitValue, Expr stepValue)
    {
        LoopVariable = loopVariable;
        InitialValue = initialValue;
        LimitValue = limitValue;
        StepValue = stepValue;
    }
    public override bool Equals(object? obj) => 
        obj is ForStmt other && 
        LoopVariable.Equals(other.LoopVariable) && 
        InitialValue.Equals(other.InitialValue) && 
        LimitValue.Equals(other.LimitValue) && 
        StepValue.Equals(other.StepValue);
    public override int GetHashCode() => HashCode.Combine(LoopVariable, InitialValue, LimitValue, StepValue);
    public override string ToString() => $"ForStmt({LoopVariable}, {InitialValue}, {LimitValue}, {StepValue})";
}

sealed class NextStmt : Statement
{
    public IReadOnlyList<VarName>? LoopVariables { get; } // Nullable for simple NEXT
    public NextStmt(IReadOnlyList<VarName>? loopVariables)
    {
        LoopVariables = loopVariables;
    }
    public override bool Equals(object? obj) => 
        obj is NextStmt other && 
        ((LoopVariables is null && other.LoopVariables is null) || 
         (LoopVariables is not null && other.LoopVariables is not null && LoopVariables.SequenceEqual(other.LoopVariables)));
    public override int GetHashCode() => LoopVariables?.Count ?? 0;
    public override string ToString() => $"NextStmt([{String.Join(", ", LoopVariables?.Select(v => v.ToString()) ?? new List<string>())}])";
}

sealed class PrintStmt : Statement
{
    public IReadOnlyList<Expr> Expressions { get; }
    public PrintStmt(IReadOnlyList<Expr> expressions)
    {
        Expressions = expressions;
    }
    public override bool Equals(object? obj) => 
        obj is PrintStmt other && 
        Expressions.SequenceEqual(other.Expressions);
    public override int GetHashCode() => Expressions.Count;
    public override string ToString() => $"PrintStmt([{String.Join(", ", Expressions.Select(e => e.ToString()))}])";
}

sealed class InputStmt : Statement
{
    public string? Prompt { get; } // Nullable for no prompt
    public IReadOnlyList<Var> Variables { get; }
    public InputStmt(string? prompt, IReadOnlyList<Var> variables)
    {
        Prompt = prompt;
        Variables = variables;
    }
    public override bool Equals(object? obj) => 
        obj is InputStmt other && 
        Prompt == other.Prompt && 
        Variables.SequenceEqual(other.Variables);
    public override int GetHashCode() => HashCode.Combine(Prompt, Variables.Count);
    public override string ToString() => $"InputStmt(\"{Prompt}\", [{String.Join(", ", Variables.Select(v => v.ToString()))}])";
}

sealed class EndStmt : Statement
{
    public override bool Equals(object? obj) => obj is EndStmt;
    public override int GetHashCode() => typeof(EndStmt).GetHashCode();
    public override string ToString() => "EndStmt";
}

sealed class StopStmt : Statement
{
    public override bool Equals(object? obj) => obj is StopStmt;
    public override int GetHashCode() => typeof(StopStmt).GetHashCode();
    public override string ToString() => "StopStmt";
}

sealed class RandomizeStmt : Statement
{
    public override bool Equals(object? obj) => obj is RandomizeStmt;
    public override int GetHashCode() => typeof(RandomizeStmt).GetHashCode();
    public override string ToString() => "RandomizeStmt";
}

sealed class ReadStmt : Statement
{
    public IReadOnlyList<Var> Variables { get; }
    public ReadStmt(IReadOnlyList<Var> variables)
    {
        Variables = variables;
    }
    public override bool Equals(object? obj) => 
        obj is ReadStmt other && 
        Variables.SequenceEqual(other.Variables);
    public override int GetHashCode() => Variables.Count;
    public override string ToString() => $"ReadStmt([{String.Join(", ", Variables.Select(v => v.ToString()))}])";
}

sealed class RestoreStmt : Statement
{
    public int? TargetLabel { get; } // Nullable for restoring to the beginning
    public RestoreStmt(int? targetLabel)
    {
        TargetLabel = targetLabel;
    }
    public override bool Equals(object? obj) => obj is RestoreStmt other && TargetLabel == other.TargetLabel;
    public override int GetHashCode() => TargetLabel.GetHashCode();
    public override string ToString() => $"RestoreStmt({TargetLabel?.ToString() ?? "Start"})";
}

sealed class DataStmt : Statement
{
    public string Data { get; }
    public DataStmt(string data) { Data = data; }
    public override bool Equals(object? obj) => obj is DataStmt other && Data == other.Data;
    public override int GetHashCode() => Data.GetHashCode();
    public override string ToString() => $"DataStmt(\"{Data}\")";
}

sealed class DefFnStmt : Statement
{
    public VarName FunctionName { get; }
    public IReadOnlyList<VarName> Parameters { get; }
    public Expr Expression { get; }
    public DefFnStmt(VarName functionName, IReadOnlyList<VarName> parameters, Expr expression)
    {
        FunctionName = functionName;
        Parameters = parameters;
        Expression = expression;
    }
    public override bool Equals(object? obj) => 
        obj is DefFnStmt other && 
        FunctionName.Equals(other.FunctionName) && 
        Parameters.SequenceEqual(other.Parameters) && 
        Expression.Equals(other.Expression);
    public override int GetHashCode() => HashCode.Combine(FunctionName, Parameters.Count, Expression);
    public override string ToString() => $"DefFnStmt({FunctionName}, [{String.Join(", ", Parameters.Select(p => p.ToString()))}], {Expression})";
}

sealed class RemStmt : Statement
{
    public string Comment { get; }
    public RemStmt(string comment) { Comment = comment; }
    public override bool Equals(object? obj) => obj is RemStmt other && Comment == other.Comment;
    public override int GetHashCode() => Comment.GetHashCode();
    public override string ToString() => $"RemStmt(\"{Comment}\")";
}
