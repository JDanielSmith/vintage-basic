using VintageBasic.Parsing.Errors;
using VintageBasic.Syntax;

namespace VintageBasic.Parsing;

sealed class Parser
{
    readonly IReadOnlyList<Tagged<Token>> _tokens;
    int _currentTokenIndex;
    readonly int _lineNumber; 
    readonly int _originalSourceLineIndex; 

    private Parser(IReadOnlyList<Tagged<Token>> tokens, int lineNumber, int originalSourceLineIndex)
    {
        _tokens = tokens;
        _currentTokenIndex = 0;
        _lineNumber = lineNumber;
        _originalSourceLineIndex = originalSourceLineIndex;
    }

    public static List<Line> ParseProgram(string programText)
    {
		List<Line> lines = [];
        var scannedLines = LineScanner.ScanLines(programText.Split([ "\r\n", "\r", "\n" ], StringSplitOptions.None));

        foreach (var scannedLine in scannedLines)
        {
            if (!scannedLine.LineNumber.HasValue)
            {
                var tempTokensForUnnumbered = Tokenizer.Tokenize(scannedLine).ToList();
                if (tempTokensForUnnumbered.Count > 0 && tempTokensForUnnumbered.Any(t => t.Value is RemToken) && tempTokensForUnnumbered.All(t => t.Value is RemToken || t.Value is EolToken) )
                {
                     // Optionally handle/store unnumbered REM statements if desired.
                } else if (!String.IsNullOrWhiteSpace(scannedLine.Content) && !tempTokensForUnnumbered.All(t=> t.Value is EolToken) ) {
                    throw new ParseException(
                        $"Line content present without a line number (source line {scannedLine.OriginalLineIndex + 1}).", 
                        new SourcePosition(scannedLine.OriginalLineIndex + 1, 1)
                    );
                }
                continue; 
            }

            var tokensForLine = Tokenizer.Tokenize(scannedLine).ToList();
            
            if (!tokensForLine.Any() || (tokensForLine.Count == 1 && tokensForLine[0].Value is EolToken))
            {
                lines.Add(new(scannedLine.LineNumber.Value, []));
                continue;
            }

			Parser parserInstance = new(tokensForLine, scannedLine.LineNumber.Value, scannedLine.OriginalLineIndex);
            try
            {
                Line parsedLine = parserInstance.ParseSingleLine();
                lines.Add(parsedLine);
            }
            catch (ParseException ex)
            {
                throw new ParseException(
                    $"Error on BASIC line {scannedLine.LineNumber.Value} (source file line {scannedLine.OriginalLineIndex + 1}): {ex.Message}", 
                    ex.InnerException ?? ex, 
                    ex.Position ?? new(scannedLine.LineNumber.Value, 1));
            }
        }
        return lines;
    }

    Line ParseSingleLine()
    {
        List<Tagged<Statement>> statements = [];
        
        while (PeekToken() is not EolToken && PeekToken() is not null)
        {
            var statement = TryParseStatement();
            if (statement is not null)
            {
                statements.Add(statement);
            }
            else
            {
                var currentToken = Peek();
                if (currentToken is not null && currentToken.Value is not EolToken)
                {
                    throw new ParseException($"Unexpected token '{currentToken.Value.Text}' when expecting a statement.", currentToken.Position);
                }
                break; 
            }

            if (TryConsumeSpecificSymbol(":", out _)) 
            {
                if (PeekToken() is EolToken || PeekToken() is null) break; 
            }
            else if (PeekToken() is not EolToken && PeekToken() is not null) 
            {
                var unexpectedToken = Peek();
                throw new ParseException($"Expected ':' to separate statements or end of line, but found '{unexpectedToken?.Value.Text}'.", unexpectedToken?.Position);
            }
        }
        
        if (PeekToken() is EolToken) ConsumeToken<EolToken>();
        else if (PeekToken() is not null) throw new ParseException("Expected End of Line after statements.", CurrentSourcePosition());
         
        return new(_lineNumber, statements);
    }

    // --- Token Helper Methods ---
    Tagged<Token>? Peek(int offset = 0) => (_currentTokenIndex + offset < _tokens.Count) ? _tokens[_currentTokenIndex + offset] : null;
    Token? PeekToken(int offset = 0) => Peek(offset)?.Value;
    SourcePosition CurrentSourcePosition() => Peek()?.Position ?? new SourcePosition(_lineNumber, _tokens.LastOrDefault()?.Position.Column + 1 ?? (_tokens.FirstOrDefault()?.Position.Column ?? 1));

    Tagged<Token> ConsumeToken()
    {
        if (_currentTokenIndex >= _tokens.Count) throw new ParseException("Unexpected end of tokens.", CurrentSourcePosition());
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
            ConsumeToken(); consumedToken = new Tagged<KeywordToken>(current.Position, kt); return true;
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
            consumedToken = ConsumeToken(); return true;
        }
        consumedToken = null;
        return false;
    }

    // --- Statement Parsers ---
    Tagged<Statement>? TryParseStatement()
    {
        var startToken = Peek() ?? throw new ParseException("Unexpected end of line.", CurrentSourcePosition());
        if (startToken.Value is EolToken) return null;
        SourcePosition startPos = startToken.Position;
        Statement? stmtNode = null;

		switch (startToken.Value)
        {
            case RemToken rt: ConsumeToken(); stmtNode = new RemStatement(rt.Comment); break;
            case KeywordToken kt:
                ConsumeToken(); // Consume keyword
                switch (kt.Keyword)
                {
                    case KeywordType.LET: stmtNode = TryParseLetStatementContents(startPos, true); break;
                    case KeywordType.PRINT: stmtNode = TryParsePrintStatementContents(); break;
                    case KeywordType.GOTO: stmtNode = TryParseGotoStatementContents(); break;
                    case KeywordType.IF: stmtNode = TryParseIfStatementContents(); break;
                    case KeywordType.END: stmtNode = new EndStatement(); break;
                    case KeywordType.DIM: stmtNode = TryParseDimStatementContents(); break;
                    case KeywordType.FOR: stmtNode = TryParseForStatementContents(); break;
                    case KeywordType.NEXT: stmtNode = TryParseNextStatementContents(); break;
                    case KeywordType.GOSUB: stmtNode = TryParseGosubStatementContents(); break;
                    case KeywordType.RETURN: stmtNode = new ReturnStatement(); break;
                    case KeywordType.READ: stmtNode = TryParseReadStatementContents(); break;
                    case KeywordType.DATA: stmtNode = TryParseDataStatementContents(); break;
                    case KeywordType.INPUT: stmtNode = TryParseInputStatementContents(); break;
                    case KeywordType.DEF: stmtNode = TryParseDefFnStatementContents(); break; 
                    case KeywordType.RANDOMIZE: stmtNode = new RandomizeStatement(); break;
                    case KeywordType.RESTORE: stmtNode = TryParseRestoreStatementContents(); break;
                    case KeywordType.STOP: stmtNode = new StopStatement(); break;
                    case KeywordType.REM: stmtNode = new RemStatement( (PeekToken() as StringToken)?.Value ?? (PeekToken() as UnknownToken)?.Content ?? ""); if(PeekToken() is StringToken || PeekToken() is UnknownToken) ConsumeToken(); break;
                    case KeywordType.ON: stmtNode = TryParseOnGotoOrGosubStatementContents(); break;
                    default: throw new ParseException($"Unhandled keyword '{kt.Keyword}' for statement.", startPos);
                }
                break;
            default:
                stmtNode = TryParseImplicitLetOrGotoStatementContents(startPos, false);
                break;
        }
        return new(startPos, stmtNode);
    }

	Statement TryParseImplicitLetOrGotoStatementContents(SourcePosition originalStartPos, bool letKeywordWasConsumed = false)
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

        return TryParseLetStatementContents(originalStartPos, letKeywordWasConsumed);
	}

	LetStatement TryParseLetStatementContents(SourcePosition originalStartPos, bool letKeywordWasConsumed = false)
    {
        if (!letKeywordWasConsumed && PeekToken() is not VarNameToken) { 
             throw new ParseException("Invalid LET statement; expected variable name.", originalStartPos); 
        }
        var variableTagged = TryParseVariableExpression();
        if (variableTagged?.Value is not VarExpression varXNode)
            throw new ParseException("Expected variable for LET statement.", variableTagged?.Position ?? originalStartPos);
        ConsumeToken<EqualsToken>();
        var expressionTagged = TryParseExpression();
        return new(varXNode.Value, expressionTagged.Value);
    }
    
    PrintStatement TryParsePrintStatementContents()
    {
        var expressions = new List<Expression>();
        while (PeekToken() is not EolToken && !(PeekToken() is Token t && t.Text == ":"))
        {
            if (TryConsumeSpecificSymbol(",", out _)) { expressions.Add(new NextZoneExpression()); if (PeekToken() is EolToken || (PeekToken() is Token tc && tc.Text == ":") ) {} } 
            else if (TryConsumeSpecificSymbol(";", out _)) { expressions.Add(new EmptyZoneExpression());}
            else { expressions.Add(TryParseExpression().Value); }
        }
        return new(expressions);
    }
    
    GotoStatement TryParseGotoStatementContents() => new((int)ConsumeToken<FloatToken>().Value.Value);
    GosubStatement TryParseGosubStatementContents() => new((int)ConsumeToken<FloatToken>().Value.Value);

    IfStatement TryParseIfStatementContents()
    {
        var condition = TryParseExpression().Value;
        if (!TryConsumeKeyword(KeywordType.THEN, out _))
            throw new ParseException("Expected THEN after IF condition.", CurrentSourcePosition());
        var thenStatements = new List<Tagged<Statement>>();
        while(PeekToken() is not EolToken && !(PeekToken() is Token t && t.Text == ":"))
        {
            var stmt = TryParseStatement();
            if(stmt is not null) thenStatements.Add(stmt); else break;
            if (TryConsumeSpecificSymbol(":", out _)) { if (PeekToken() is EolToken) break; } else break;
        }
        return new(condition, thenStatements);
    }

    DimStatement TryParseDimStatementContents()
    {
        var declarations = new List<(VarName Name, IReadOnlyList<Expression> Dimensions)>();
        do {
            var varNameToken = ConsumeToken<VarNameToken>();
            VarName varName = new(varNameToken.Value.TypeSuffix, varNameToken.Value.Name);
            ConsumeToken<LParenToken>();
            var dims = new List<Expression>();
            do { dims.Add(TryParseExpression().Value); }
            while (TryConsumeSpecificSymbol(",", out _));
            ConsumeToken<RParenToken>();
            declarations.Add((varName, dims));
        } while (TryConsumeSpecificSymbol(",", out _));
        return new(declarations);
    }

    ForStatement TryParseForStatementContents()
    {
        var loopVarToken = ConsumeToken<VarNameToken>();
        VarName loopVar = new(loopVarToken.Value.TypeSuffix, loopVarToken.Value.Name);
        ConsumeToken<EqualsToken>();
        Expression initial = TryParseExpression().Value;
        if(!TryConsumeKeyword(KeywordType.TO, out _)) throw new ParseException("Expected TO in FOR statement.", CurrentSourcePosition());
        var limit = TryParseExpression().Value;
        Expression step = new LiteralExpression(new FloatLiteral(1.0f)); 
        if(TryConsumeKeyword(KeywordType.STEP, out _)) step = TryParseExpression().Value;
        return new(loopVar, initial, limit, step);
    }

    NextStatement TryParseNextStatementContents()
    {
        List<VarName> vars = [];
        while(PeekToken() is VarNameToken vnt)
        {
            ConsumeToken(); vars.Add(new VarName(vnt.TypeSuffix, vnt.Name));
            if (!TryConsumeSpecificSymbol(",", out _)) break;
        }
        return new(vars.Any() ? vars : null);
    }
    
    ReadStatement TryParseReadStatementContents()
    {
        List<Var> vars = [];
        do { vars.Add((TryParseVariableExpression().Value as VarExpression)?.Value ?? throw new ParseException("Invalid variable in READ.", CurrentSourcePosition())); }
        while(TryConsumeSpecificSymbol(",", out _));
        return new(vars);
    }

    DataStatement TryParseDataStatementContents()
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

    InputStatement TryParseInputStatementContents()
    {
        string? prompt = null;
        if(PeekToken() is StringToken st)
        {
            ConsumeToken();
            prompt = st.Value;
            if(!TryConsumeSpecificSymbol(";", out _) && !TryConsumeSpecificSymbol(",", out _)) 
                throw new ParseException("Expected ; or , after INPUT prompt String.", CurrentSourcePosition());
        }
        List<Var> vars = [];
        do { vars.Add((TryParseVariableExpression().Value as VarExpression)?.Value ?? throw new ParseException("Invalid variable in INPUT.", CurrentSourcePosition()));}
        while(TryConsumeSpecificSymbol(",", out _));
        return new(prompt, vars);
    }

    DefFnStatement TryParseDefFnStatementContents() 
    {
        // DEF was consumed. Now expect FN.
        if(!TryConsumeKeyword(KeywordType.FN, out _)) throw new ParseException("Expected FN after DEF.", CurrentSourcePosition());
        var funcNameToken = ConsumeToken<VarNameToken>();
        VarName funcName = new(funcNameToken.Value.TypeSuffix, funcNameToken.Value.Name);
        ConsumeToken<LParenToken>();
        List<VarName> parameters = [];
        if(PeekToken() is not RParenToken)
        {
            do { 
                var paramToken = ConsumeToken<VarNameToken>();
                parameters.Add(new(paramToken.Value.TypeSuffix, paramToken.Value.Name));
            } while(TryConsumeSpecificSymbol(",", out _));
        }
        ConsumeToken<RParenToken>();
        ConsumeToken<EqualsToken>();
        var body = TryParseExpression().Value;
        return new(funcName, parameters, body);
    }
    RestoreStatement TryParseRestoreStatementContents()
    {
        int? label = null;
        if(PeekToken() is FloatToken ft) { ConsumeToken(); label = (int)ft.Value; } // Optional label
        return new(label);
    }

    Statement TryParseOnGotoOrGosubStatementContents()
    {
        // ON was already consumed. Expect an expression.
        var indexExpr = TryParseExpression().Value;
        
        bool isGosub = false;
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

        var labels = new List<int>();
        do
        {
            var labelToken = ConsumeToken<FloatToken>(); // Line numbers are parsed as FloatTokens by current Tokenizer
            labels.Add((int)labelToken.Value.Value);
        } while (TryConsumeSpecificSymbol(",", out _));

        if (!labels.Any())
        {
            throw new ParseException("Expected at least one label in ON...GOTO/GOSUB statement.", CurrentSourcePosition());
        }

        return isGosub ? new OnGosubStatement(indexExpr, labels) : new OnGotoStatement(indexExpr, labels);
    }


    // --- Expression Parsing (Recursive Descent with Precedence) ---
    Tagged<Expression> TryParseExpression(int minPrecedence = 0) 
    {
        var lhs = TryParseUnaryExpression(); 
        while (true)
        {
            var opToken = PeekToken();
            BinOp? currentOp = null;
            int precedence = -1;

            if (opToken is OpToken ot) { currentOp = ot.Op; precedence = GetOperatorPrecedence(currentOp.Value); }
            else if (opToken is EqualsToken) { currentOp = BinOp.EqOp; precedence = GetOperatorPrecedence(currentOp.Value); }
            else if (opToken is KeywordToken kt) 
            {
                if(kt.Keyword == KeywordType.AND) { currentOp = BinOp.AndOp; precedence = GetOperatorPrecedence(BinOp.AndOp); }
                else if(kt.Keyword == KeywordType.OR) { currentOp = BinOp.OrOp; precedence = GetOperatorPrecedence(BinOp.OrOp); }
            }

            if (currentOp is null || precedence < minPrecedence) break;
            
            ConsumeToken(); 
            int nextMinPrecedence = IsRightAssociative(currentOp.Value) ? precedence : precedence + 1;
            var rhs = TryParseExpression(nextMinPrecedence);
            lhs = new(lhs.Position, new BinOpExpression(currentOp.Value, lhs.Value, rhs.Value));
        }
        return lhs;
    }
    
    Tagged<Expression> TryParseUnaryExpression()
    {
        var tokenTagged = Peek() ?? throw new ParseException("Unexpected end of expression.", CurrentSourcePosition());
        if (tokenTagged.Value is OpToken opTok && opTok.Op == BinOp.SubOp) 
        {
            ConsumeToken();
            var operand = TryParseExpression(GetOperatorPrecedence(BinOp.SubOp, true)); 
            return new(tokenTagged.Position, new MinusExpression(operand.Value));
        }
        if (TryConsumeKeyword(KeywordType.NOT, out var notTokenTagged)) 
        {
            var operand = TryParseExpression(GetOperatorPrecedenceForNot()); 
            return new(notTokenTagged!.Position, new NotExpression(operand.Value));
        }
        return TryParsePowerExpression(); 
    }

    Tagged<Expression> TryParsePowerExpression()
    {
        var lhs = TryParseAtom();
        while(PeekToken() is OpToken op && op.Op == BinOp.PowOp)
        {
            ConsumeToken(); 
            var rhs = TryParseUnaryExpression(); 
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
                ConsumeToken(); var expr = TryParseExpression(); ConsumeToken<RParenToken>(); 
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
        VarName varName = new(varNameTagged.Value.TypeSuffix, varNameTagged.Value.Name);
        if (PeekToken() is LParenToken) 
        {
            ConsumeToken<LParenToken>();
            List<Expression> dimensions = [];
            if (PeekToken() is not RParenToken)
            {
                do { dimensions.Add(TryParseExpression().Value); } 
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
                do { args.Add(TryParseExpression().Value); }
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
        VarName funcName = new(funcNameToken.Value.TypeSuffix, funcNameToken.Value.Name);
        ConsumeToken<LParenToken>();
        List<Expression> args = [];
        if (PeekToken() is not RParenToken)
        {
            do { args.Add(TryParseExpression().Value); }
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
            BinOp.OrOp => 0, BinOp.AndOp => 1,
            BinOp.EqOp => 3, BinOp.NEOp => 3, BinOp.LTOp => 3, BinOp.LEOp => 3, BinOp.GTOp => 3, BinOp.GEOp => 3,
            BinOp.AddOp => 4, BinOp.SubOp => 4,
            BinOp.MulOp => 5, BinOp.DivOp => 5,
            BinOp.PowOp => 6, 
            _ => -1 
        };
    }
    
    static bool IsRightAssociative(BinOp op) => op == BinOp.PowOp;
}
