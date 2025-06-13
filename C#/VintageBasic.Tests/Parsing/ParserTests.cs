// VintageBasic.Tests/Parsing/ParserTests.cs
using VintageBasic.Parsing;
using VintageBasic.Syntax;
using Xunit;

namespace VintageBasic.Tests.Parsing
{
	public class ParserTests
	{
		[Fact]
		public void TestParseLetStatement()
		{
			string programText = "10 LET A = 123";

			// Expected AST
			var expectedVarName = VarName.CreateFloat("A");
			var expectedScalarVar = new ScalarVar(expectedVarName);
			var expectedLiteral = 123.0f;
			var expectedLitX = new LiteralExpression(expectedLiteral);
			// Assuming LET starts at column 1 for its content after line number and space.
			// Actual column positions from Tokenizer would be used in real scenario.
			// For this example, we'll use placeholder positions or focus on structure.
			var expectedLetStmt = new LetStatement(expectedScalarVar, expectedLitX);
			// Placeholder source position for the statement itself.
			var expectedTaggedStmt = new Tagged<Statement>(new SourcePosition(10, 1), expectedLetStmt);
			Line expectedLine = new(10, [expectedTaggedStmt]);

			var parsedLines = Parser.ParseProgram(programText);

			Assert.Single(parsedLines);
			var actualLine = parsedLines.First();
			Assert.Equal(expectedLine.Label, actualLine.Label);
			Assert.Single(actualLine.Statements);

			var actualTaggedStmt = actualLine.Statements.First();
			Assert.IsType<LetStatement>(actualTaggedStmt.Value);
			var actualLetStmt = (LetStatement)actualTaggedStmt.Value;

			Assert.IsType<ScalarVar>(actualLetStmt.Variable);
			var actualScalarVar = (ScalarVar)actualLetStmt.Variable;
			Assert.Equal(expectedScalarVar.VarName.Name, actualScalarVar.VarName.Name);
			Assert.Equal(expectedScalarVar.VarName.Val.GetType(), actualScalarVar.VarName.Val.GetType());

			Assert.IsType<LiteralExpression>(actualLetStmt.Expression);
			var actualLitX = (LiteralExpression)actualLetStmt.Expression;
			Assert.IsType<float>(actualLitX.Value);
			Assert.Equal(expectedLiteral, ((float)actualLitX.Value), 5); // Precision for float comparison
		}

		[Fact]
		public void TestParsePrintStatementWithMultipleExpressions()
		{
			string programText = "20 PRINT \"AGE:\", A; \"!\"";
			var parsedLines = Parser.ParseProgram(programText);

			Assert.Single(parsedLines);
			var line = parsedLines.First();
			Assert.Equal(20, line.Label);
			Assert.Single(line.Statements);
			Assert.IsType<PrintStatement>(line.Statements.First().Value);

			var printStmt = (PrintStatement)line.Statements.First().Value;
			var printStmtExpressions = printStmt.Expressions.ToList();
			Assert.Equal(5, printStmtExpressions.Count); // "AGE:", NextZoneExpression, VarExpression(A), EmptyZoneExpression, "!"

			Assert.IsType<LiteralExpression>(printStmtExpressions[0]);
			Assert.Equal("AGE:", ((string)((LiteralExpression)printStmtExpressions[0]).Value));

			Assert.IsType<NextZoneExpression>(printStmtExpressions[1]); // Comma results in NextZoneExpression

			Assert.IsType<VarExpression>(printStmtExpressions[2]);
			Assert.Equal("A", ((VarName)((VarExpression)printStmtExpressions[2]).Value.Name).Name);

			Assert.IsType<EmptyZoneExpression>(printStmtExpressions[3]); // Semicolon results in EmptyZoneExpression

			Assert.IsType<LiteralExpression>(printStmtExpressions[4]);
			Assert.Equal("!", ((string)((LiteralExpression)printStmtExpressions[4]).Value));
		}
	}
}
