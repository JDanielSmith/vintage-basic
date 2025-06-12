using System.Collections.Generic;
using VintageBasic.Syntax;

namespace VintageBasic.Parsing;

sealed class Parser
{
	readonly IReadOnlyList<Tagged<Token>> _tokens;
	int _currentTokenIndex;
	readonly int _lineNumber;

	private Parser(IReadOnlyList<Tagged<Token>> tokens, int lineNumber)
	{
		_tokens = tokens;
		_lineNumber = lineNumber;
	}

	public static List<Line> ParseProgram(string programText)
	{
		List<Line> lines = [];
		var scannedLines = LineScanner.ScanLines(programText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None));

		foreach (var scannedLine in scannedLines)
		{
			var lineNumber = scannedLine.LineNumber;

			var tokensForLine = Tokenizer.Tokenize(scannedLine).ToList();
			if ((tokensForLine.Count <= 0) || (tokensForLine.Count == 1 && tokensForLine[0].Value is EolToken))
			{
				lines.Add(new(lineNumber, []));
				continue;
			}

			Parser parserInstance = new(tokensForLine, lineNumber);
			try
			{
				var parsedLine = parserInstance.ParseSingleLine();
				lines.Add(parsedLine);
			}
			catch (ParseException ex)
			{
				throw new ParseException(
					$"Error on BASIC line {lineNumber} (source file line {scannedLine.OriginalLineIndex + 1}): {ex.Message}",
					ex.InnerException ?? ex,
					ex.Position ?? new(lineNumber, 1));
			}
		}
		return lines;
	}

	Line ParseSingleLine()
	{
		IEnumerable<Tagged<Statement>> ParseSingleLine_()
		{
			while (!EndOfLine)
			{
				var statement = TryParseStatement();
				if (statement is not null)
				{
					yield return statement;
				}
				else
				{
					var currentToken = Peek();
					if (currentToken is not null && currentToken.Value is not EolToken)
					{
						throw new ParseException($"Unexpected token '{currentToken.Value.Text}' when expecting a statement.", currentToken.Position);
					}
					yield break;
				}

				if (TryConsumeSpecificSymbol(":", out _))
				{
					if (EndOfLine) yield break;
				}
				else if (!EndOfLine)
				{
					var unexpectedToken = Peek();
					throw new ParseException($"Expected ':' to separate statements or end of line, but found '{unexpectedToken?.Value.Text}'.", unexpectedToken?.Position);
				}
			}
		}
		List<Tagged<Statement>> statements = [.. ParseSingleLine_()];

		if (PeekToken() is EolToken)
			ConsumeToken<EolToken>();
		else if (PeekToken() is not null)
			throw new ParseException("Expected End of Line after statements.", CurrentSourcePosition());
		return new(_lineNumber, statements);
	}

	static T FirstOrDefault<T>(IReadOnlyList<T> list) => list.Count > 0 ? list[0] : default!;
	static T LastOrDefault<T>(IReadOnlyList<T> list) => list.Count > 0 ? list[^1] : default!;

	// --- Token Helper Methods ---
	Tagged<Token>? Peek(int offset = 0) => (_currentTokenIndex + offset < _tokens.Count) ? _tokens[_currentTokenIndex + offset] : null;
	Token? PeekToken(int offset = 0) => Peek(offset)?.Value;
	SourcePosition CurrentSourcePosition() => Peek()?.Position ?? new(_lineNumber, LastOrDefault(_tokens)?.Position.Column + 1 ?? FirstOrDefault(_tokens)?.Position.Column ?? 1);

	bool EndOfLine => PeekToken() is EolToken || PeekToken() is null;

	bool EndOfStatement => EndOfLine || (PeekToken() is Token t && t.Text is ":");

	Tagged<Token> ConsumeToken()
	{
		if (_currentTokenIndex >= _tokens.Count)
			throw new ParseException("Unexpected end of tokens.", CurrentSourcePosition());
		var token = _tokens[_currentTokenIndex];
		_currentTokenIndex++;
		return token;
	}

	Tagged<T> ConsumeToken<T>(string? expectedText = null) where T : Token
	{
		var taggedToken = ConsumeToken();
		if (taggedToken.Value is T specificToken)
		{
			if (expectedText is not null && !specificToken.Text.Equals(expectedText, StringComparison.OrdinalIgnoreCase))
				throw new ParseException($"Expected token '{expectedText}' but got '{specificToken.Text}'.", taggedToken.Position);
			return new(taggedToken.Position, specificToken);
		}
		throw new ParseException($"Expected token type {typeof(T).Name} but got {taggedToken.Value.GetType().Name} ('{taggedToken.Value.Text}').", taggedToken.Position);
	}

	bool TryConsumeKeyword(KeywordType kw, out Tagged<KeywordToken>? consumedToken)
	{
		var current = Peek();
		if (current?.Value is KeywordToken kt && kt.Keyword == kw)
		{
			ConsumeToken();
			consumedToken = new(current.Position, kt);
			return true;
		}
		consumedToken = null;
		return false;
	}

	bool TryConsumeSpecificSymbol(string symbol, out Tagged<Token>? consumedToken)
	{
		var current = Peek();
		// Check against Text property for symbols like ':', '=', etc.
		if (current is not null && current.Value.Text.Equals(symbol, StringComparison.OrdinalIgnoreCase))
		{
			consumedToken = ConsumeToken();
			return true;
		}
		consumedToken = null;
		return false;
	}

	// --- Statement Parsers ---
	Statement ParseKeywordStatement(KeywordType keywordType, SourcePosition startPos)
	{
		RemStatement CreateRemStatement()
		{
			var token = PeekToken();
			if (token is StringToken or UnknownToken)
				ConsumeToken();
			return new((token as StringToken)?.Value ?? (token as UnknownToken)?.Content ?? "");
		}

		ConsumeToken(); // Consume keyword
		return keywordType switch
		{
			KeywordType.LET => ParseLetStatementContents(startPos, true),
			KeywordType.PRINT => ParsePrintStatementContents(),
			KeywordType.GOTO => ParseGotoStatementContents(),
			KeywordType.IF => ParseIfStatementContents(),
			KeywordType.END => new EndStatement(),
			KeywordType.DIM => ParseDimStatementContents(),
			KeywordType.FOR => ParseForStatementContents(),
			KeywordType.NEXT => ParseNextStatementContents(),
			KeywordType.GOSUB => ParseGosubStatementContents(),
			KeywordType.RETURN => new ReturnStatement(),
			KeywordType.READ => ParseReadStatementContents(),
			KeywordType.DATA => ParseDataStatementContents(),
			KeywordType.INPUT => ParseInputStatementContents(),
			KeywordType.DEF => ParseDefFnStatementContents(),
			KeywordType.RANDOMIZE => new RandomizeStatement(),
			KeywordType.RESTORE => ParseRestoreStatementContents(),
			KeywordType.STOP => new StopStatement(),
			KeywordType.REM => CreateRemStatement(),
			KeywordType.ON => ParseOnGotoOrGosubStatementContents(),
			_ => throw new ParseException($"Unhandled keyword '{keywordType}' for statement.", startPos)
		};
	}

	Tagged<Statement>? TryParseStatement()
	{
		var startToken = Peek() ?? throw new ParseException("Unexpected end of line.", CurrentSourcePosition());
		if (startToken.Value is EolToken)
			return null;
		var startPos = startToken.Position;

		RemStatement CreateRemStatement(RemToken rt)
		{
			ConsumeToken();
			return new(rt.Comment);
		}

		var stmtNode = startToken.Value switch
		{
			RemToken rt => CreateRemStatement(rt),
			KeywordToken kt => ParseKeywordStatement(kt.Keyword, startPos),
			_ => ParseImplicitLetOrGotoStatementContents(startPos, false)
		};
		return new(startPos, stmtNode);
	}

	Statement ParseImplicitLetOrGotoStatementContents(SourcePosition originalStartPos, bool letKeywordWasConsumed = false)
	{
		var token = PeekToken();
		if (!letKeywordWasConsumed && token is not VarNameToken)
		{
			if (token is FloatToken ft)
			{
				ConsumeToken(); // Consume the FloatToken
				return new GotoStatement((int)ft.Value); // Implicit GOTO with line number
			}
			throw new ParseException("Invalid LET statement; expected variable name.", originalStartPos);
		}
		return ParseLetStatementContents(originalStartPos, letKeywordWasConsumed);
	}

	LetStatement ParseLetStatementContents(SourcePosition originalStartPos, bool letKeywordWasConsumed = false)
	{
		if (!letKeywordWasConsumed && PeekToken() is not VarNameToken)
		{
			throw new ParseException("Invalid LET statement; expected variable name.", originalStartPos);
		}
		var variableTagged = TryParseVariableExpression();
		if (variableTagged?.Value is not VarExpression varXNode)
			throw new ParseException("Expected variable for LET statement.", variableTagged?.Position ?? originalStartPos);
		ConsumeToken<EqualsToken>();
		var expressionTagged = ParseExpression();
		return new(varXNode.Value, expressionTagged.Value);
	}

	PrintStatement ParsePrintStatementContents()
	{
		IEnumerable<Expression> GetPrintStatementContents()
		{
			while (!EndOfStatement)
			{
				if (TryConsumeSpecificSymbol(",", out _)) yield return new NextZoneExpression();
				else if (TryConsumeSpecificSymbol(";", out _)) yield return new EmptyZoneExpression();
				else yield return ParseExpression().Value;
			}
		}
		return new([.. GetPrintStatementContents()]); // Enumerate now so that results are fixed.
	}

	GotoStatement ParseGotoStatementContents() => new((int)ConsumeToken<FloatToken>().Value.Value);
	GosubStatement ParseGosubStatementContents() => new((int)ConsumeToken<FloatToken>().Value.Value);

	IfStatement ParseIfStatementContents()
	{
		var condition = ParseExpression().Value;
		if (!TryConsumeKeyword(KeywordType.THEN, out _))
			throw new ParseException("Expected THEN after IF condition.", CurrentSourcePosition());

		IEnumerable<Tagged<Statement>> GetIfStatementContents()
		{
			while (!EndOfStatement)
			{
				var stmt = TryParseStatement();
				if (stmt is not null)
					yield return stmt;
			}
		}
		return new(condition, [.. GetIfStatementContents()]); // Enumerate now so that results are fixed.
	}

	DimStatement ParseDimStatementContents()
	{
		List<(VarName Name, IReadOnlyList<Expression> Dimensions)> declarations = [];
		do
		{
			var varNameToken = ConsumeToken<VarNameToken>();
			VarName varName = new(varNameToken.Value);
			ConsumeToken<LParenToken>();
			List<Expression> dims = [];
			do { dims.Add(ParseExpression().Value); }
			while (TryConsumeSpecificSymbol(",", out _));
			ConsumeToken<RParenToken>();
			declarations.Add((varName, dims));
		} while (TryConsumeSpecificSymbol(",", out _));
		return new(declarations);
	}

	ForStatement ParseForStatementContents()
	{
		var loopVarToken = ConsumeToken<VarNameToken>();
		VarName loopVar = new(loopVarToken.Value);
		ConsumeToken<EqualsToken>();
		var initial = ParseExpression().Value;
		if (!TryConsumeKeyword(KeywordType.TO, out _))
			throw new ParseException("Expected TO in FOR statement.", CurrentSourcePosition());
		var limit = ParseExpression().Value;
		Expression step = new LiteralExpression(new FloatLiteral(1.0f));
		if (TryConsumeKeyword(KeywordType.STEP, out _))
			step = ParseExpression().Value;
		return new(loopVar, initial, limit, step);
	}

	NextStatement ParseNextStatementContents()
	{
		List<VarName> vars = [];
		while (PeekToken() is VarNameToken vnt)
		{
			ConsumeToken(); vars.Add(new(vnt));
			if (!TryConsumeSpecificSymbol(",", out _)) break;
		}
		return new(vars.Count > 0 ? vars : null);
	}

	ReadStatement ParseReadStatementContents()
	{
		List<Var> vars = [];
		do { vars.Add((TryParseVariableExpression().Value as VarExpression)?.Value ?? throw new ParseException("Invalid variable in READ.", CurrentSourcePosition())); }
		while (TryConsumeSpecificSymbol(",", out _));
		return new(vars);
	}

	DataStatement ParseDataStatementContents()
	{
		// After "DATA" keyword, the Tokenizer should provide a DataContentToken
		// which contains the raw string content for the DATA statement.
		var dataContentToken = PeekToken();
		if (dataContentToken is DataContentToken dct)
		{
			ConsumeToken(); // Consume the DataContentToken
			return new(dct.RawContent);
		}
		// If no DataContentToken follows DATA, it means DATA statement is empty or there's a tokenizer issue.
		// An empty DATA statement is valid (e.g., "10 DATA").
		// The raw content would be empty.
		// If the next token is EOL or ':', it's an empty DATA statement.
		if (dataContentToken is EolToken || (dataContentToken is not null && dataContentToken.Text == ":"))
		{
			return new(""); // Empty DATA statement
		}
		throw new ParseException($"Expected data content after DATA keyword, but found {dataContentToken?.Text}", CurrentSourcePosition());
	}

	InputStatement ParseInputStatementContents()
	{
		string? prompt = null;
		if (PeekToken() is StringToken st)
		{
			ConsumeToken();
			prompt = st.Value;
			if (!TryConsumeSpecificSymbol(";", out _) && !TryConsumeSpecificSymbol(",", out _))
				throw new ParseException("Expected ; or , after INPUT prompt String.", CurrentSourcePosition());
		}
		List<Var> vars = [];
		do { vars.Add((TryParseVariableExpression().Value as VarExpression)?.Value ?? throw new ParseException("Invalid variable in INPUT.", CurrentSourcePosition())); }
		while (TryConsumeSpecificSymbol(",", out _));
		return new(prompt, vars);
	}

	DefFnStatement ParseDefFnStatementContents()
	{
		// DEF was consumed. Now expect FN.
		if (!TryConsumeKeyword(KeywordType.FN, out _)) throw new ParseException("Expected FN after DEF.", CurrentSourcePosition());
		var funcNameToken = ConsumeToken<VarNameToken>();
		VarName funcName = new(funcNameToken.Value);
		ConsumeToken<LParenToken>();
		List<VarName> parameters = [];
		if (PeekToken() is not RParenToken)
		{
			do
			{
				var paramToken = ConsumeToken<VarNameToken>();
				parameters.Add(new(paramToken.Value));
			} while (TryConsumeSpecificSymbol(",", out _));
		}
		ConsumeToken<RParenToken>();
		ConsumeToken<EqualsToken>();
		var body = ParseExpression().Value;
		return new(funcName, parameters, body);
	}
	RestoreStatement ParseRestoreStatementContents()
	{
		int? label = null;
		if (PeekToken() is FloatToken ft) // Optional label
		{
			ConsumeToken();
			label = (int)ft.Value;
		} 
		return new(label);
	}

	Statement ParseOnGotoOrGosubStatementContents()
	{
		// ON was already consumed. Expect an expression.
		var indexExpr = ParseExpression().Value;

		bool isGosub;
		if (TryConsumeKeyword(KeywordType.GOTO, out _))
		{
			isGosub = false;
		}
		else if (TryConsumeKeyword(KeywordType.GOSUB, out _))
		{
			isGosub = true;
		}
		else
		{
			throw new ParseException("Expected GOTO or GOSUB after expression in ON statement.", CurrentSourcePosition());
		}
		IEnumerable<int> GetLabels()
		{
			do
			{
				var labelToken = ConsumeToken<FloatToken>(); // Line numbers are parsed as FloatTokens by current Tokenizer
				yield return (int)labelToken.Value.Value;
			} while (TryConsumeSpecificSymbol(",", out _));
		}
		var labels = GetLabels();
		if (!labels.Any())
		{
			throw new ParseException("Expected at least one label in ON...GOTO/GOSUB statement.", CurrentSourcePosition());
		}
		var labels_ = labels.ToList();
		return isGosub ? new OnGosubStatement(indexExpr, labels_) : new OnGotoStatement(indexExpr, labels_);
	}

	// --- Expression Parsing (Recursive Descent with Precedence) ---
	Tagged<Expression> ParseExpression(int minPrecedence = 0)
	{
		var lhs = ParseUnaryExpression();
		while (true)
		{
			var opToken = PeekToken();
			BinOp? currentOp = null;
			int precedence = -1;

			if (opToken is OpToken ot)
			{
				currentOp = ot.Op;
				precedence = GetOperatorPrecedence(currentOp.Value);
			}
			else if (opToken is EqualsToken)
			{
				currentOp = BinOp.EqOp;
				precedence = GetOperatorPrecedence(currentOp.Value);
			}
			else if (opToken is KeywordToken kt)
			{
				if (kt.Keyword == KeywordType.AND)
				{
					currentOp = BinOp.AndOp;
					precedence = GetOperatorPrecedence(BinOp.AndOp);
				}
				else if (kt.Keyword == KeywordType.OR)
				{
					currentOp = BinOp.OrOp;
					precedence = GetOperatorPrecedence(BinOp.OrOp);
				}
			}
			if (currentOp is null || precedence < minPrecedence) break;

			ConsumeToken();
			int nextMinPrecedence = IsRightAssociative(currentOp.Value) ? precedence : precedence + 1;
			var rhs = ParseExpression(nextMinPrecedence);
			lhs = new(lhs.Position, new BinOpExpression(currentOp.Value, lhs.Value, rhs.Value));
		}
		return lhs;
	}

	Tagged<Expression> ParseUnaryExpression()
	{
		var tokenTagged = Peek() ?? throw new ParseException("Unexpected end of expression.", CurrentSourcePosition());
		if (tokenTagged.Value is OpToken opTok && opTok.Op == BinOp.SubOp)
		{
			ConsumeToken();
			var operand = ParseExpression(GetOperatorPrecedence(BinOp.SubOp, true));
			return new(tokenTagged.Position, new MinusExpression(operand.Value));
		}
		if (TryConsumeKeyword(KeywordType.NOT, out var notTokenTagged))
		{
			var operand = ParseExpression(GetOperatorPrecedenceForNot());
			return new(notTokenTagged!.Position, new NotExpression(operand.Value));
		}
		return ParsePowerExpression();
	}

	Tagged<Expression> ParsePowerExpression()
	{
		var lhs = TryParseAtom();
		while (PeekToken() is OpToken op && op.Op == BinOp.PowOp)
		{
			ConsumeToken();
			var rhs = ParseUnaryExpression();
			lhs = new(lhs.Position, new BinOpExpression(BinOp.PowOp, lhs.Value, rhs.Value));
		}
		return lhs;
	}

	Tagged<Expression> TryParseAtom()
	{
		var taggedToken = Peek() ?? throw new ParseException("Unexpected end of expression, expected atom.", CurrentSourcePosition());
		var token = taggedToken.Value;
		var pos = taggedToken.Position;

		switch (token)
		{
			case FloatToken ft: ConsumeToken(); return new(pos, new LiteralExpression(new FloatLiteral((float)ft.Value)));
			case StringToken st: ConsumeToken(); return new(pos, new LiteralExpression(new StringLiteral(st.Value)));
			case LParenToken:
				ConsumeToken(); var expr = ParseExpression(); ConsumeToken<RParenToken>();
				return new(pos, new ParenExpression(expr.Value));
			case VarNameToken: return TryParseVariableExpression();
			case BuiltinFuncToken: return TryParseBuiltinFunctionCall();
			case KeywordToken kt when kt.Keyword == KeywordType.FN: return TryParseUserFunctionCall();
			default: throw new ParseException($"Unexpected token '{token.Text}' in expression atom.", pos);
		}
	}

	Tagged<Expression> TryParseVariableExpression()
	{
		var varNameTagged = ConsumeToken<VarNameToken>();
		VarName varName = new(varNameTagged.Value);
		if (PeekToken() is LParenToken)
		{
			ConsumeToken<LParenToken>();
			List<Expression> dimensions = [];
			if (PeekToken() is not RParenToken)
			{
				do { dimensions.Add(ParseExpression().Value); }
				while (TryConsumeSpecificSymbol(",", out _));
			}
			ConsumeToken<RParenToken>();
			return new(varNameTagged.Position, new VarExpression(new ArrVar(varName, dimensions)));
		}
		return new(varNameTagged.Position, new VarExpression(new ScalarVar(varName)));
	}

	Tagged<Expression> TryParseBuiltinFunctionCall()
	{
		var builtinTokenTagged = ConsumeToken<BuiltinFuncToken>();
		var builtin = builtinTokenTagged.Value.FuncName;
		List<Expression> args = [];
		if (PeekToken() is LParenToken)
		{
			ConsumeToken<LParenToken>();
			if (PeekToken() is not RParenToken)
			{
				do { args.Add(ParseExpression().Value); }
				while (TryConsumeSpecificSymbol(",", out _));
			}
			ConsumeToken<RParenToken>();
		}
		return new(builtinTokenTagged.Position, new BuiltinExpression(builtin, args));
	}

	Tagged<Expression> TryParseUserFunctionCall()
	{
		ConsumeToken<KeywordToken>("FN");
		var funcNameToken = ConsumeToken<VarNameToken>();
		VarName funcName = new(funcNameToken.Value);
		ConsumeToken<LParenToken>();
		List<Expression> args = [];
		if (PeekToken() is not RParenToken)
		{
			do { args.Add(ParseExpression().Value); }
			while (TryConsumeSpecificSymbol(",", out _));
		}
		ConsumeToken<RParenToken>();
		return new(funcNameToken.Position, new FnExpression(funcName, args));
	}

	static int GetOperatorPrecedenceForNot() => 2;

	static int GetOperatorPrecedence(BinOp op, bool isUnary = false)
	{
		if (isUnary && op == BinOp.SubOp) return 7;
		return op switch
		{
			BinOp.OrOp => 0,
			BinOp.AndOp => 1,
			BinOp.EqOp => 3,
			BinOp.NEOp => 3,
			BinOp.LTOp => 3,
			BinOp.LEOp => 3,
			BinOp.GTOp => 3,
			BinOp.GEOp => 3,
			BinOp.AddOp => 4,
			BinOp.SubOp => 4,
			BinOp.MulOp => 5,
			BinOp.DivOp => 5,
			BinOp.PowOp => 6,
			_ => -1
		};
	}

	static bool IsRightAssociative(BinOp op) => op == BinOp.PowOp;
}
