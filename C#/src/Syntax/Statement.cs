namespace VintageBasic.Syntax;

abstract record Statement { }

sealed record LetStmt(Var Variable, Expr Expression) : Statement
{
    public override string ToString() => $"LetStmt({Variable}, {Expression})";
}

sealed record DimStmt(IReadOnlyList<(VarName Name, IReadOnlyList<Expr> Dimensions)> Declarations) : Statement
{
    public override string ToString() => $"DimStmt([{String.Join(", ", Declarations.Select(d => $"{d.Name}({String.Join(", ", d.Dimensions)})"))}])";
}

sealed record GotoStmt(int TargetLabel) : Statement
{
    public override string ToString() => $"GotoStmt({TargetLabel})";
}

sealed record GosubStmt(int TargetLabel) : Statement
{
    public override string ToString() => $"GosubStmt({TargetLabel})";
}

sealed record OnGotoStmt(Expr Expression, IReadOnlyList<int> TargetLabels) : Statement
{
    public override string ToString() => $"OnGotoStmt({Expression}, [{String.Join(", ", TargetLabels)}])";
}

sealed record OnGosubStmt(Expr Expression, IReadOnlyList<int> TargetLabels) : Statement
{
    public override string ToString() => $"OnGosubStmt({Expression}, [{String.Join(", ", TargetLabels)}])";
}

sealed record ReturnStmt : Statement
{
    public override string ToString() => "ReturnStmt";
}

sealed record IfStmt(Expr Condition, IReadOnlyList<Tagged<Statement>> Statements) : Statement
{
    public override string ToString() => $"IfStmt({Condition}, [{String.Join("; ", Statements.Select(s => s.ToString()))}])";
}

sealed record ForStmt(VarName LoopVariable, Expr InitialValue, Expr LimitValue, Expr StepValue) : Statement
{
    public override string ToString() => $"ForStmt({LoopVariable}, {InitialValue}, {LimitValue}, {StepValue})";
}

sealed record NextStmt(IReadOnlyList<VarName>? LoopVariables) : Statement // Nullable for simple NEXT
{
    public override string ToString() => $"NextStmt([{String.Join(", ", LoopVariables?.Select(v => v.ToString()) ?? new List<string>())}])";
}

sealed record PrintStmt(IReadOnlyList<Expr> Expressions) : Statement
{
    public override string ToString() => $"PrintStmt([{String.Join(", ", Expressions.Select(e => e.ToString()))}])";
}

sealed record InputStmt(string? Prompt, IReadOnlyList<Var> Variables) : Statement
{
    public override string ToString() => $"InputStmt(\"{Prompt}\", [{String.Join(", ", Variables.Select(v => v.ToString()))}])";
}

sealed record EndStmt : Statement
{
    public override string ToString() => "EndStmt";
}

sealed record StopStmt : Statement
{
    public override string ToString() => "StopStmt";
}

sealed record RandomizeStmt : Statement
{
    public override string ToString() => "RandomizeStmt";
}

sealed record ReadStmt(IReadOnlyList<Var> Variables) : Statement
{
    public override string ToString() => $"ReadStmt([{String.Join(", ", Variables.Select(v => v.ToString()))}])";
}

sealed record RestoreStmt(int? TargetLabel) : Statement
{
    public override string ToString() => $"RestoreStmt({TargetLabel?.ToString() ?? "Start"})";
}

sealed record DataStmt(string Data) : Statement
{
    public override string ToString() => $"DataStmt(\"{Data}\")";
}

sealed record DefFnStmt(VarName FunctionName, IReadOnlyList<VarName> Parameters, Expr Expression) : Statement
{
    public override string ToString() => $"DefFnStmt({FunctionName}, [{String.Join(", ", Parameters.Select(p => p.ToString()))}], {Expression})";
}

sealed record RemStmt(string Comment) : Statement
{
    public override string ToString() => $"RemStmt(\"{Comment}\")";
}
