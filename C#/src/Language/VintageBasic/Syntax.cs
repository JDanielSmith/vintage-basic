using System;
using System.Collections.Generic;

namespace vintage_basic.Language.VintageBasic;

enum ValType
{
	FloatType,
	IntType,
	StringType
}

interface ITypeable
{
	ValType TypeOf();
}

sealed class Val { }
sealed class Var { }

// BASIC Line Number
sealed class Label
{
	public int Value { get; }
	public Label(int value) => Value = value;
}

//public abstract class Literal : ITypeable
//{
//	public abstract ValType TypeOf();
//}

sealed class VarName : ITypeable
{
	public ValType Type { get; }
	public string Name { get; }
	public VarName(ValType type, string name)
	{
		Type = type;
		Name = name;
	}
	public ValType TypeOf() => Type;
}

/*
public abstract class Variable : ITypeable
{
	public abstract ValType TypeOf();
}

sealed class ScalarVariable : Variable
{
	public VarName Name { get; }
	public ScalarVariable(VarName name) => Name = name;
	public override ValType TypeOf() => Name.TypeOf();
}

sealed class ArrayVariable : Variable
{
	public VarName Name { get; }
	public List<Expression> Indexes { get; }
	public ArrayVariable(VarName name, List<Expression> indexes)
	{
		Name = name;
		Indexes = indexes;
	}
	public override ValType TypeOf() => Name.TypeOf();
}
*/

// BASIC binary operators
enum BinaryOperator
{
	Add,
	Subtract,
	Multiply,
	Divide,
	Power,
	Equal,
	NotEqual,
	LessThan,
	LessThanOrEqual,
	GreaterThan,
	GreaterThanOrEqual,
	And,
	Or
}

sealed class FunctionExpression : Expression
{
	public VarName Name { get; }
	public List<Expression> Arguments { get; }
	public FunctionExpression(VarName name, List<Expression> arguments)
	{
		Name = name;
		Arguments = arguments;
	}
}

sealed class UnaryExpression : Expression
{
	public Expression Operand { get; }
	public string Operator { get; }
	public UnaryExpression(string op, Expression operand)
	{
		Operator = op;
		Operand = operand;
	}
}

//sealed class BinaryExpression : Expression
//{
//	public BinaryOperator Operator { get; }
//	public Expression Left { get; }
//	public Expression Right { get; }
//	public BinaryExpression(BinaryOperator op, Expression left, Expression right)
//	{
//		Operator = op;
//		Left = left;
//		Right = right;
//	}
//}

sealed class PrintSeparatorExpression : Expression
{
	public bool IsComma { get; }
	public PrintSeparatorExpression(bool isComma) => IsComma = isComma;
}

sealed class ParenthesizedExpression : Expression
{
	public Expression Inner { get; }
	public ParenthesizedExpression(Expression inner) => Inner = inner;
}

//// BASIC Statements
//public abstract class Statement { }

//sealed class LetStatement : Statement
//{
//	public Variable Var { get; }
//	public Expression Expr { get; }
//	public LetStatement(Variable var, Expression expr)
//	{
//		Var = var;
//		Expr = expr;
//	}
//}

//sealed class PrintStatement : Statement
//{
//	public List<Expression> Expressions { get; }
//	public PrintStatement(List<Expression> expressions)
//	{
//		Expressions = expressions;
//	}
//}

sealed class InputStatement : Statement
{
	public List<VarName> Variables { get; }
	public InputStatement(List<VarName> variables)
	{
		Variables = variables;
	}

	public string? Prompt { get; set; } // Optional prompt
}

//sealed class IfStatement : Statement
//{
//	public Expression Condition { get; }
//	public List<Statement> ThenStatements { get; }
//	public List<Statement> ElseStatements { get; }
//	public IfStatement(Expression condition, List<Statement> thenStatements, List<Statement> elseStatements)
//	{
//		Condition = condition;
//		ThenStatements = thenStatements;
//		ElseStatements = elseStatements;
//	}
//}

sealed class GotoStatement : Statement
{
	public int Target { get; }
	public GotoStatement(int target) => Target = target;
}

sealed class GosubStatement : Statement
{
	public Label Target { get; }
	public GosubStatement(Label target) => Target = target;
}

sealed class ReturnStatement : Statement { }

sealed class EndStatement : Statement { }

sealed class StopStatement : Statement { }

sealed class RemStatement : Statement
{
	public string Comment { get; }
	public RemStatement(string comment) => Comment = comment;
}

sealed class DataStatement : Statement
{
	public List<Val> Values { get; }
	public DataStatement(List<Val> values) => Values = values;
}

sealed class DimStatement : Statement
{
	public List<VarName> Variables { get; }
	public DimStatement(List<VarName> variables) => Variables = variables;

	public string VarName { get; set; } = string.Empty; // Optional name for the variable
}

sealed class ForStatement : Statement
{
	public VarName Variable { get; }
	public Expression Start { get; }
	public Expression End { get; }
	public Expression Step { get; }
	public List<Statement> Body { get; }
	public ForStatement(VarName variable, Expression start, Expression end, Expression step, List<Statement> body)
	{
		Variable = variable;
		Start = start;
		End = end;
		Step = step;
		Body = body;
	}
}

sealed class NextStatement : Statement
{
	public VarName Variable { get; }
	public NextStatement(VarName variable) => Variable = variable;
}

sealed class OnGotoStatement : Statement
{
	public Expression Expression { get; }
	public List<Label> Targets { get; }
	public OnGotoStatement(Expression expression, List<Label> targets)
	{
		Expression = expression;
		Targets = targets;
	}
}

sealed class OnGosubStatement : Statement
{
	public Expression Expression { get; }
	public List<Label> Targets { get; }
	public OnGosubStatement(Expression expression, List<Label> targets)
	{
		Expression = expression;
		Targets = targets;
	}
}






// BASIC program lines
sealed class BasicLine
{
	public int LineNumber { get; }
	public List<Statement> Statements { get; }
	public BasicLine(int lineNumber, List<Statement> statements)
	{
		LineNumber = lineNumber;
		Statements = statements;
	}
}
