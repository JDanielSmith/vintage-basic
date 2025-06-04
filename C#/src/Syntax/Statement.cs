namespace VintageBasic.Syntax;

abstract record Statement { }

sealed record LetStatement(Var Variable, Expr Expression) : Statement
{
    public override string ToString() => $"{nameof(LetStatement)}({Variable}, {Expression})";
}

sealed record DimStatement(IReadOnlyList<(VarName Name, IReadOnlyList<Expr> Dimensions)> Declarations) : Statement
{
    public override string ToString() => $"{nameof(DimStatement)}([{String.Join(", ", Declarations.Select(d => $"{d.Name}({String.Join(", ", d.Dimensions)})"))}])";
}

sealed record GotoStatement(int TargetLabel) : Statement
{
    public override string ToString() => $"{nameof(GotoStatement)}({TargetLabel})";
}

sealed record GosubStatement(int TargetLabel) : Statement
{
    public override string ToString() => $"{nameof(GosubStatement)}({TargetLabel})";
}

sealed record OnGotoStatement(Expr Expression, IReadOnlyList<int> TargetLabels) : Statement
{
    public override string ToString() => $"{nameof(OnGotoStatement)}({Expression}, [{String.Join(", ", TargetLabels)}])";
}

sealed record OnGosubStatement(Expr Expression, IReadOnlyList<int> TargetLabels) : Statement
{
    public override string ToString() => $"{nameof(OnGosubStatement)}({Expression}, [{String.Join(", ", TargetLabels)}])";
}

sealed record ReturnStatement : Statement
{
    public override string ToString() => nameof(ReturnStatement);
}

sealed record IfStatement(Expr Condition, IReadOnlyList<Tagged<Statement>> Statements) : Statement
{
    public override string ToString() => $"{nameof(IfStatement)}({Condition}, [{String.Join("; ", Statements.Select(s => s.ToString()))}])";
}

sealed record ForStatement(VarName LoopVariable, Expr InitialValue, Expr LimitValue, Expr StepValue) : Statement
{
    public override string ToString() => $"{nameof(ForStatement)}({LoopVariable}, {InitialValue}, {LimitValue}, {StepValue})";
}

sealed record NextStatement(IReadOnlyList<VarName>? LoopVariables) : Statement // Nullable for simple NEXT
{
    public override string ToString() => $"{nameof(NextStatement)}([{String.Join(", ", LoopVariables?.Select(v => v.ToString()) ?? new List<string>())}])";
}

sealed record PrintStatement(IReadOnlyList<Expr> Expressions) : Statement
{
    public override string ToString() => $"{nameof(PrintStatement)}([{String.Join(", ", Expressions.Select(e => e.ToString()))}])";
}

sealed record InputStatement(string? Prompt, IReadOnlyList<Var> Variables) : Statement
{
    public override string ToString() => $"{nameof(InputStatement)}(\"{Prompt}\", [{String.Join(", ", Variables.Select(v => v.ToString()))}])";
}

sealed record EndStatement : Statement
{
    public override string ToString() => nameof(EndStatement);
}

sealed record StopStatement : Statement
{
    public override string ToString() => nameof(StopStatement);
}

sealed record RandomizeStatement : Statement
{
    public override string ToString() => nameof(RandomizeStatement);
}

sealed record ReadStatement(IReadOnlyList<Var> Variables) : Statement
{
    public override string ToString() => $"{nameof(ReadStatement)}([{String.Join(", ", Variables.Select(v => v.ToString()))}])";
}

sealed record RestoreStatement(int? TargetLabel) : Statement
{
    public override string ToString() => $"{nameof(RestoreStatement)}({TargetLabel?.ToString() ?? "Start"})";
}

sealed record DataStatement(string Data) : Statement
{
    public override string ToString() => $"{nameof(DataStatement)}(\"{Data}\")";
}

sealed record DefFnStatement(VarName FunctionName, IReadOnlyList<VarName> Parameters, Expr Expression) : Statement
{
    public override string ToString() => $"{nameof(DefFnStatement)}({FunctionName}, [{String.Join(", ", Parameters.Select(p => p.ToString()))}], {Expression})";
}

sealed record RemStatement(string Comment) : Statement
{
    public override string ToString() => $"{nameof(RemStatement)}(\"{Comment}\")";
}
