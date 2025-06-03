using System;
using System.Collections.Generic;

namespace vintage_basic.Language.VintageBasic;

public enum ValType
{
	FloatType,
	IntType,
	StringType
}

public interface ITypeable
{
	ValType TypeOf();
}

public class Val { }
public class Var { }

// BASIC Line Number
public class Label
{
	public int Value { get; }
	public Label(int value) => Value = value;
}

//public abstract class Literal : ITypeable
//{
//	public abstract ValType TypeOf();
//}

public class VarName : ITypeable
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

public class ScalarVariable : Variable
{
	public VarName Name { get; }
	public ScalarVariable(VarName name) => Name = name;
	public override ValType TypeOf() => Name.TypeOf();
}

public class ArrayVariable : Variable
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
public enum BinaryOperator
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

public class FunctionExpression : Expression
{
	public VarName Name { get; }
	public List<Expression> Arguments { get; }
	public FunctionExpression(VarName name, List<Expression> arguments)
	{
		Name = name;
		Arguments = arguments;
	}
}

public class UnaryExpression : Expression
{
	public Expression Operand { get; }
	public string Operator { get; }
	public UnaryExpression(string op, Expression operand)
	{
		Operator = op;
		Operand = operand;
	}
}

//public class BinaryExpression : Expression
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

public class PrintSeparatorExpression : Expression
{
	public bool IsComma { get; }
	public PrintSeparatorExpression(bool isComma) => IsComma = isComma;
}

public class ParenthesizedExpression : Expression
{
	public Expression Inner { get; }
	public ParenthesizedExpression(Expression inner) => Inner = inner;
}

//// BASIC Statements
//public abstract class Statement { }

//public class LetStatement : Statement
//{
//	public Variable Var { get; }
//	public Expression Expr { get; }
//	public LetStatement(Variable var, Expression expr)
//	{
//		Var = var;
//		Expr = expr;
//	}
//}

//public class PrintStatement : Statement
//{
//	public List<Expression> Expressions { get; }
//	public PrintStatement(List<Expression> expressions)
//	{
//		Expressions = expressions;
//	}
//}

public class InputStatement : Statement
{
	public List<VarName> Variables { get; }
	public InputStatement(List<VarName> variables)
	{
		Variables = variables;
	}

	public string? Prompt { get; set; } // Optional prompt
}

//public class IfStatement : Statement
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

public class GotoStatement : Statement
{
	public int Target { get; }
	public GotoStatement(int target) => Target = target;
}

public class GosubStatement : Statement
{
	public Label Target { get; }
	public GosubStatement(Label target) => Target = target;
}

public class ReturnStatement : Statement { }

public class EndStatement : Statement { }

public class StopStatement : Statement { }

public class RemStatement : Statement
{
	public string Comment { get; }
	public RemStatement(string comment) => Comment = comment;
}

public class DataStatement : Statement
{
	public List<Val> Values { get; }
	public DataStatement(List<Val> values) => Values = values;
}

public class DimStatement : Statement
{
	public List<VarName> Variables { get; }
	public DimStatement(List<VarName> variables) => Variables = variables;

	public string VarName { get; set; } = string.Empty; // Optional name for the variable
}

public class ForStatement : Statement
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

public class NextStatement : Statement
{
	public VarName Variable { get; }
	public NextStatement(VarName variable) => Variable = variable;
}

public class OnGotoStatement : Statement
{
	public Expression Expression { get; }
	public List<Label> Targets { get; }
	public OnGotoStatement(Expression expression, List<Label> targets)
	{
		Expression = expression;
		Targets = targets;
	}
}

public class OnGosubStatement : Statement
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
public class BasicLine
{
	public int LineNumber { get; }
	public List<Statement> Statements { get; }
	public BasicLine(int lineNumber, List<Statement> statements)
	{
		LineNumber = lineNumber;
		Statements = statements;
	}
}
