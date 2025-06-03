// VintageBasic.Tests/Parsing/ParserTests.cs
using System.Collections.Generic;
using System.Linq;
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
            // Arrange
            string programText = "10 LET A = 123";
            
            // Expected AST
            var expectedVarName = new VarName(ValType.FloatType, "A");
            var expectedScalarVar = new ScalarVar(expectedVarName);
            var expectedLiteral = new FloatLiteral(123.0f);
            var expectedLitX = new LitX(expectedLiteral);
            // Assuming LET starts at column 1 for its content after line number and space.
            // Actual column positions from Tokenizer would be used in real scenario.
            // For this example, we'll use placeholder positions or focus on structure.
            var expectedLetStmt = new LetStmt(expectedScalarVar, expectedLitX);
            // Placeholder source position for the statement itself.
            var expectedTaggedStmt = new Tagged<Statement>(new SourcePosition(10, 1), expectedLetStmt);
            var expectedLine = new Line(10, new List<Tagged<Statement>> { expectedTaggedStmt });

            // Act
            var parsedLines = Parser.ParseProgram(programText);

            // Assert
            Assert.Single(parsedLines);
            var actualLine = parsedLines[0];
            Assert.Equal(expectedLine.Label, actualLine.Label);
            Assert.Single(actualLine.Statements);
            
            var actualTaggedStmt = actualLine.Statements[0];
            Assert.IsType<LetStmt>(actualTaggedStmt.Value);
            var actualLetStmt = (LetStmt)actualTaggedStmt.Value;

            Assert.IsType<ScalarVar>(actualLetStmt.Variable);
            var actualScalarVar = (ScalarVar)actualLetStmt.Variable;
            Assert.Equal(expectedScalarVar.VarName.Name, actualScalarVar.VarName.Name);
            Assert.Equal(expectedScalarVar.VarName.Type, actualScalarVar.VarName.Type);

            Assert.IsType<LitX>(actualLetStmt.Expression);
            var actualLitX = (LitX)actualLetStmt.Expression;
            Assert.IsType<FloatLiteral>(actualLitX.Value);
            Assert.Equal(expectedLiteral.Value, ((FloatLiteral)actualLitX.Value).Value, 5); // Precision for float comparison
        }

        [Fact]
        public void TestParsePrintStatementWithMultipleExpressions()
        {
            string programText = "20 PRINT \"AGE:\", A; \"!\"";
            var parsedLines = Parser.ParseProgram(programText);

            Assert.Single(parsedLines);
            var line = parsedLines[0];
            Assert.Equal(20, line.Label);
            Assert.Single(line.Statements);
            Assert.IsType<PrintStmt>(line.Statements[0].Value);

            var printStmt = (PrintStmt)line.Statements[0].Value;
            Assert.Equal(5, printStmt.Expressions.Count); // "AGE:", NextZoneX, VarX(A), EmptySeparatorX, "!"

            Assert.IsType<LitX>(printStmt.Expressions[0]);
            Assert.Equal("AGE:", ((StringLiteral)((LitX)printStmt.Expressions[0]).Value).Value);
            
            Assert.IsType<NextZoneX>(printStmt.Expressions[1]); // Comma results in NextZoneX

            Assert.IsType<VarX>(printStmt.Expressions[2]);
            Assert.Equal("A", ((VarName)((VarX)printStmt.Expressions[2]).Value.Name).Name);

            Assert.IsType<EmptySeparatorX>(printStmt.Expressions[3]); // Semicolon results in EmptySeparatorX
            
            Assert.IsType<LitX>(printStmt.Expressions[4]);
            Assert.Equal("!", ((StringLiteral)((LitX)printStmt.Expressions[4]).Value).Value);
        }
    }
}
