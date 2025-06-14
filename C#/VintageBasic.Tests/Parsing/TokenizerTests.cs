// VintageBasic.Tests/Parsing/TokenizerTests.cs
using VintageBasic.Parsing;
using VintageBasic.Syntax;
using Xunit;

namespace VintageBasic.Tests.Parsing
{
	public class TokenizerTests
	{
		[Fact]
		public void PrintStringTokenizeTest()
		{
			var scannedLine = new ScannedLine(10, "PRINT \"HELLO\"", 0);
			var expectedTokens = new List<Tagged<Token>>
			{
				new(new(10, 1), new KeywordToken(KeywordType.PRINT)),
				new(new(10, 7), new StringToken("HELLO")), // Assuming column after PRINT and space
                new(new(10, 14), new EolToken()) // Position after "HELLO"
            };

			var actualTokens = Tokenizer.Tokenize(scannedLine).ToList();

			Assert.Equal(expectedTokens.Count, actualTokens.Count);
			for (int i = 0; i < expectedTokens.Count; i++)
			{
				Assert.Equal(expectedTokens[i].Value, actualTokens[i].Value);
				Assert.Equal(expectedTokens[i].Position, actualTokens[i].Position);
			}
		}

		[Fact]
		public void LetStatementTokenizeTest()
		{
			var scannedLine = new ScannedLine(20, "let a = 123.45", 1);
			var tokens = Tokenizer.Tokenize(scannedLine).ToList();

			Assert.Collection(tokens,
				t => { Assert.IsType<KeywordToken>(t.Value); Assert.Equal(KeywordType.LET, ((KeywordToken)t.Value).Keyword); Assert.Equal(new SourcePosition(20, 1), t.Position); },
				t => { Assert.IsType<VarNameToken>(t.Value); Assert.Equal("a", ((VarNameToken)t.Value).Name); Assert.Equal(typeof(float), ((VarNameToken)t.Value).Type); Assert.Equal(new SourcePosition(20, 5), t.Position); },
				t => { Assert.IsType<EqualsToken>(t.Value); Assert.Equal(new SourcePosition(20, 7), t.Position); },
				t => { Assert.IsType<FloatToken>(t.Value); Assert.Equal(123.45, ((FloatToken)t.Value).Value, 2); Assert.Equal(new SourcePosition(20, 9), t.Position); },
				t => { Assert.IsType<EolToken>(t.Value); Assert.Equal(new SourcePosition(20, 15), t.Position); } // Position after 123.45
			);
		}

		[Fact]
		public void RemStatementTokenizeTest()
		{
			var scannedLine = new ScannedLine(30, "REM THIS IS A COMMENT", 2);
			var tokens = Tokenizer.Tokenize(scannedLine).ToList();

			Assert.Collection(tokens,
				t =>
				{
					Assert.IsType<RemToken>(t.Value);
					Assert.Equal("THIS IS A COMMENT", ((RemToken)t.Value).Comment);
					Assert.Equal(new SourcePosition(30, 1), t.Position); // REM starts at col 1
				},
				t => { Assert.IsType<EolToken>(t.Value); Assert.Equal(new SourcePosition(30, 22), t.Position); } // EOL after comment
			);
		}

		[Fact]
		public void RemStatementWithTickTokenizeTest()
		{
			var scannedLine = new ScannedLine(35, "' ALSO A COMMENT", 3);
			var tokens = Tokenizer.Tokenize(scannedLine).ToList();
			Assert.Collection(tokens,
			   t =>
			   {
				   Assert.IsType<RemToken>(t.Value);
				   Assert.Equal(" ALSO A COMMENT", ((RemToken)t.Value).Comment);
				   Assert.Equal(new SourcePosition(35, 1), t.Position);
			   },
			   t => { Assert.IsType<EolToken>(t.Value); Assert.Equal(new SourcePosition(35, 17), t.Position); }
		   );
		}
	}
}
