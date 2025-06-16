using VintageBasic.Runtime;
using VintageBasic.Runtime.Errors;
using VintageBasic.Parsing;

namespace VintageBasic.Syntax;

abstract record Expression
{
	public virtual bool IsPrintSeparator => false;
	internal abstract object Evaluate(Interpreter.Interpreter interpreter, int currentBasicLine);

	// Coerces int to float for expression evaluation if needed, otherwise returns original value.
	internal static object CoerceToType(object value, int? lineNumber = null, StateManager? stateManager = null)
	{
		var targetType = value is int ? typeof(float) : value.GetType(); // Coerce int to float for expression evaluation
		return targetType.CoerceToType(value, lineNumber, stateManager);
	}
}

sealed record LiteralExpression(object Value) : Expression
{
	public override string ToString() => $"{nameof(LiteralExpression)}({Value})";
	internal override object Evaluate(Interpreter.Interpreter interpreter, int currentBasicLine) => Value switch
	{
		float or string => Value,
		_ => throw new NotSupportedException(),
	};
}

sealed record VarExpression(Var Value) : Expression
{
	public override string ToString() => $"{nameof(VarExpression)}({Value})";
	internal override object Evaluate(Interpreter.Interpreter interpreter, int currentBasicLine) =>
		CoerceToType(Value.GetValue(interpreter), currentBasicLine, interpreter.StateManager);
}

sealed record MinusExpression(Expression Right) : Expression
{
	public override string ToString() => $"{nameof(MinusExpression)}({Right})";
	internal override object Evaluate(Interpreter.Interpreter interpreter, int currentBasicLine)
	{
		var op = interpreter.EvaluateExpression(Right, currentBasicLine);
		if (CoerceToType(op, currentBasicLine, interpreter.StateManager) is float fv)
			return -fv;
		throw new TypeMismatchError("Numeric operand for unary minus.", currentBasicLine);
	}
}

sealed record NotExpression(Expression Right) : Expression
{
	public override string ToString() => $"{nameof(NotExpression)}({Right})";
	internal override object Evaluate(Interpreter.Interpreter interpreter, int currentBasicLine)
	{
		var notOp = interpreter.EvaluateExpression(Right, currentBasicLine);
		if (CoerceToType(notOp, currentBasicLine, interpreter.StateManager) is float fvN)
			return fvN == 0.0f ? -1.0f : 0.0f;
		throw new TypeMismatchError("Numeric operand for NOT.", currentBasicLine);
	}
}

sealed record BinOpExpression(BinOp Op, Expression Left, Expression Right) : Expression
{
	public override string ToString() => $"{nameof(BinOpExpression)}({Op}, {Left}, {Right})";
	internal override object Evaluate(Interpreter.Interpreter interpreter, int currentBasicLine)
	{
		var lhs = interpreter.EvaluateExpression(Left, currentBasicLine);
		var rhs = interpreter.EvaluateExpression(Right, currentBasicLine);
		return interpreter.EvaluateBinOp(Op, lhs, rhs, currentBasicLine);
	}
}

sealed record BuiltinExpression(Builtin Builtin, IEnumerable<Expression> Args) : Expression
{
	public override string ToString() => $"{nameof(BuiltinExpression)}({Builtin}, [{String.Join(", ", Args.Select(a => a.ToString()))}])";
	internal override object Evaluate(Interpreter.Interpreter interpreter, int currentBasicLine) => interpreter.EvaluateBuiltin(Builtin, Args, currentBasicLine);
}

sealed record NextZoneExpression : Expression
{
	public override bool IsPrintSeparator => true;
	public override string ToString() => nameof(NextZoneExpression);

	internal const string Value = "<Special:NextZone>";
	internal override object Evaluate(Interpreter.Interpreter interpreter, int currentBasicLine) => Value;
}

sealed record EmptyZoneExpression : Expression
{
	public override bool IsPrintSeparator => true;
	public override string ToString() => nameof(EmptyZoneExpression);

	internal const string Value = "<Special:EmptySeparator>";
	internal override object Evaluate(Interpreter.Interpreter interpreter, int currentBasicLine) => Value;
}

sealed record ParenExpression(Expression Inner) : Expression
{
	public override string ToString() => $"{nameof(ParenExpression)}({Inner})";
	internal override object Evaluate(Interpreter.Interpreter interpreter, int currentBasicLine) => interpreter.EvaluateExpression(Inner, currentBasicLine);
}
