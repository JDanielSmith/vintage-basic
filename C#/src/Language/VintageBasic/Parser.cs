using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace vintage_basic.Language.VintageBasic;

public class BasicParser
{
	//private Queue<TaggedToken> tokens;

	//public BasicParser(List<TaggedToken> tokenStream)
	//{
	//	tokens = new Queue<TaggedToken>(tokenStream);
	//}

	//private void SkipSpace()
	//{
	//	while (tokens.Count > 0 && tokens.Peek().Token.Type == TokenType.SpaceTok)
	//	{
	//		tokens.Dequeue();
	//	}
	//}

	//public int ParseLineNumber()
	//{
	//	var digits = new List<char>();
	//	while (tokens.Count > 0 && char.IsDigit(tokens.Peek().Token.Value[0]))
	//	{
	//		digits.Add(tokens.Dequeue().Token.Value[0]);
	//	}
	//	SkipSpace();
	//	return digits.Count > 0 ? int.Parse(new string(digits.ToArray())) : throw new Exception("Invalid line number");
	//}

	//public Literal ParseLiteral()
	//{
	//	if (tokens.Count == 0) throw new Exception("Unexpected end of input");

	//	var token = tokens.Dequeue();
	//	if (token.Token.Type == TokenType.StringTok)
	//	{
	//		return new StringLiteral(token.Token.Value);
	//	}
	//	if (float.TryParse(token.Token.Value, out float floatValue))
	//	{
	//		return new FloatLiteral(floatValue);
	//	}

	//	throw new Exception("Invalid literal");
	//}

	//public Variable ParseVariable()
	//{
	//	if (tokens.Count == 0) throw new Exception("Unexpected end of input");

	//	var token = tokens.Dequeue();
	//	string name = token.Token.Value;
	//	if (name.Length > 2) name = name.Substring(0, 2); // Restrict significant letters

	//	if (tokens.Count > 0 && tokens.Peek().Token.Type == TokenType.PercentTok)
	//	{
	//		tokens.Dequeue();
	//		return new IntVariable(name);
	//	}
	//	if (tokens.Count > 0 && tokens.Peek().Token.Type == TokenType.DollarTok)
	//	{
	//		tokens.Dequeue();
	//		return new StringVariable(name);
	//	}

	//	return new FloatVariable(name);
	//}

	//public Expression ParseExpression()
	//{
	//	var left = ParsePrimaryExpression();
	//	while (tokens.Count > 0 && IsOperator(tokens.Peek().Token.Value))
	//	{
	//		string op = tokens.Dequeue().Token.Value;
	//		var right = ParsePrimaryExpression();
	//		left = new BinaryExpression(left, right, op);
	//	}
	//	return left;
	//}

	//private Expression ParsePrimaryExpression()
	//{
	//	if (tokens.Count == 0) throw new Exception("Unexpected end of input");

	//	var token = tokens.Dequeue();
	//	if (float.TryParse(token.Token.Value, out float num))
	//		return new LiteralExpression(new FloatLiteral(num));
	//	if (Regex.IsMatch(token.Token.Value, @"^[a-zA-Z]+\d*$"))
	//		return new VariableExpression(new FloatVariable(token.Token.Value));

	//	throw new Exception("Unexpected token: " + token.Token.Value);
	//}

	//private bool IsOperator(string token) => "+-*/".Contains(token);

	//public Statement ParseStatement()
	//{
	//	if (tokens.Count == 0) throw new Exception("Unexpected end of input");

	//	var token = tokens.Peek();

	//	switch (token.Token.Type)
	//	{
	//		case TokenType.LetTok:
	//			return ParseLetStatement();
	//		case TokenType.GoTok:
	//			return ParseGotoStatement();
	//		case TokenType.IfTok:
	//			return ParseIfStatement();
	//		case TokenType.PrintTok:
	//			return ParsePrintStatement();
	//		default:
	//			throw new Exception($"Unexpected statement token: {token.Token.Type}");
	//	}
	//}

	//private Statement ParseLetStatement()
	//{
	//	tokens.Dequeue(); // Consume LET token
	//	var variable = ParseVariable();
	//	var eqToken = tokens.Dequeue();
	//	if (eqToken.Token.Type != TokenType.EqTok)
	//		throw new Exception("Expected '=' in LET statement");
	//	var expression = ParseExpression();
	//	return new LetStatement(variable, expression);
	//}

	//private Statement ParseGotoStatement()
	//{
	//	tokens.Dequeue(); // Consume GO token

	//	var toToken = tokens.Dequeue();
	//	if (toToken.Token.Type != TokenType.ToTok)
	//		throw new Exception("Expected 'TO' after 'GO' in GOTO statement");

	//	var lineNumber = ParseLineNumber();
	//	return new GotoStatement(lineNumber);
	//}

	//private Statement ParseIfStatement()
	//{
	//	tokens.Dequeue(); // Consume IF token
	//	var condition = ParseExpression();
	//	var thenToken = tokens.Dequeue();
	//	if (thenToken.Token.Type != TokenType.ThenTok)
	//		throw new Exception("Expected THEN after IF condition");
	//	var statements = new List<Statement>();
	//	while (tokens.Count > 0)
	//	{
	//		statements.Add(ParseStatement());
	//	}
	//	return new IfStatement(condition, statements);
	//}

	//private Statement ParsePrintStatement()
	//{
	//	tokens.Dequeue(); // Consume PRINT token
	//	var expressions = new List<Expression>();
	//	while (tokens.Count > 0)
	//	{
	//		expressions.Add(ParseExpression());
	//		if (tokens.Count > 0 && tokens.Peek().Token.Type == TokenType.CommaTok)
	//			tokens.Dequeue(); // Consume comma
	//	}
	//	return new PrintStatement(expressions);
	//}
}



// Supporting Classes
public abstract class Literal { }
public class FloatLiteral : Literal { public float Value { get; } public FloatLiteral(float value) => Value = value; }
public class StringLiteral : Literal { public string Value { get; } public StringLiteral(string value) => Value = value; }

public abstract class Variable { }
public class FloatVariable : Variable { public string Name { get; } public FloatVariable(string name) => Name = name; }
public class IntVariable : Variable { public string Name { get; } public IntVariable(string name) => Name = name; }
public class StringVariable : Variable { public string Name { get; } public StringVariable(string name) => Name = name; }

public abstract class Expression { }
public class LiteralExpression : Expression { public Literal Value { get; } public LiteralExpression(Literal value) => Value = value; }
public class VariableExpression : Expression { public Variable Value { get; } public VariableExpression(Variable value) => Value = value; }
public class BinaryExpression : Expression { public Expression Left { get; } public Expression Right { get; } public string Operator { get; } public BinaryExpression(Expression left, Expression right, string op) { Left = left; Right = right; Operator = op; } }

public abstract class Statement { }

public class LetStatement : Statement
{
	public Variable Var { get; }
	public Expression Expr { get; }
	public LetStatement(Variable var, Expression expr)
	{
		Var = var;
		Expr = expr;
	}
}

//public class GotoStatement : Statement
//{
//	public int LineNumber { get; }
//	public GotoStatement(int lineNumber) => LineNumber = lineNumber;
//}

public class IfStatement : Statement
{
	public Expression Condition { get; }
	public List<Statement> Statements { get; }
	public IfStatement(Expression condition, List<Statement> statements)
	{
		Condition = condition;
		Statements = statements;
	}
}

public class PrintStatement : Statement
{
	public List<Expression> Expressions { get; }
	public PrintStatement(List<Expression> expressions) => Expressions = expressions;
}



//public class BasicLine
//{
//	public int LineNumber { get; }
//	public List<Statement> Statements { get; }
//	public BasicLine(int lineNumber, List<Statement> statements)
//	{
//		LineNumber = lineNumber;
//		Statements = statements;
//	}
//}
class ParsedLine
{
	public int Label { get; set; }
	public List<Tagged<Statement>> Statements { get; set; } = new List<Tagged<Statement>>();
}
