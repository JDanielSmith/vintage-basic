using System.Text;
using System.Xml.Linq;
using VintageBasic.Runtime;
using VintageBasic.Syntax; // For ValType, Builtin, BinOp

namespace VintageBasic.Parsing;

static class Tokenizer
{
    private static readonly Dictionary<string, KeywordType> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        {"LET", KeywordType.LET}, {"PRINT", KeywordType.PRINT}, {"IF", KeywordType.IF}, {"THEN", KeywordType.THEN},
        {"FOR", KeywordType.FOR}, {"TO", KeywordType.TO}, {"STEP", KeywordType.STEP}, {"NEXT", KeywordType.NEXT},
        {"GOTO", KeywordType.GOTO}, {"GOSUB", KeywordType.GOSUB}, {"RETURN", KeywordType.RETURN}, {"END", KeywordType.END},
        {"DATA", KeywordType.DATA}, {"READ", KeywordType.READ}, {"INPUT", KeywordType.INPUT}, {"DIM", KeywordType.DIM},
        {"REM", KeywordType.REM}, {"ON", KeywordType.ON}, {"RESTORE", KeywordType.RESTORE}, {"STOP", KeywordType.STOP},
        {"RANDOMIZE", KeywordType.RANDOMIZE}, {"DEF", KeywordType.DEF}, {"FN", KeywordType.FN},
        { "OR", KeywordType.OR}, { "AND", KeywordType.AND}, { "NOT", KeywordType.NOT},
		// Note: ELSE is not in Haskell's KeywordTok but might be useful for parser. Added to enum.
	};

    private static readonly Dictionary<string, Builtin> Builtins = new(StringComparer.OrdinalIgnoreCase)
    {
		{"ABS", Builtin.Abs},
        {"ASC", Builtin.Asc },
	    {"ATN", Builtin.Atn },
	    {"CHR$", Builtin.Chr },
	    {"COS", Builtin.Cos },
	    {"EXP", Builtin.Exp },
	    {"INT", Builtin.Int },
	    {"LEFT$", Builtin.Left },  // Corresponds to LeftBI
        {"LEN", Builtin.Len },
	    {"LOG", Builtin.Log },
	    {"MID$", Builtin.Mid },   // Corresponds to MidBI
        {"RIGHT$", Builtin.Right }, // Corresponds to RightBI
        {"RND", Builtin.Rnd },
	    {"SGN", Builtin.Sgn },
	    {"SIN", Builtin.Sin },
	    {"SPC", Builtin.Spc },
	    {"SQR", Builtin.Sqr },
	    {"STR$", Builtin.Str },
	    {"TAB", Builtin.Tab },
	    {"TAN", Builtin.Tan },
	    {"VAL", Builtin.Val },
	};

    private static readonly Dictionary<string, BinOp> Operators = new()
    {
        {"+", BinOp.AddOp}, {"-", BinOp.SubOp}, {"*", BinOp.MulOp}, {"/", BinOp.DivOp},
        {"^", BinOp.PowOp}, {"<>", BinOp.NEOp}, {"<=", BinOp.LEOp}, {">=", BinOp.GEOp},
        {"<", BinOp.LTOp}, {">", BinOp.GTOp}
        // Note: "=" is handled as EqualsToken separately. AND/OR are keywords for BinX.
    };
    
    // For longest match, order operators from longest to shortest.
    private static readonly List<string> OrderedOperatorSymbols = Operators.Keys.OrderByDescending(k => k.Length).ToList();

    public static IEnumerable<Tagged<Token>> Tokenize(ScannedLine scannedLine)
    {
        var tokens = new List<Tagged<Token>>();
        string content = scannedLine.Content;
        int currentPositionInContent = 0; // 0-based index into content string
        int lineNumber = scannedLine.LineNumber ?? 0;

        // Handle REM at the very start of content (after potential line number)
        if (content.StartsWith("REM", StringComparison.OrdinalIgnoreCase) || ((content.Length > 0) && (content[0] == '\''))) // Some BASICs use ' for REM
        {
            string comment;
            int commentStartColumn = currentPositionInContent;
            if (content[0] == '\'')
            {
                comment = content.Substring(1);
                currentPositionInContent = 1; // Position of "'"
            }
            else
            {
                comment = content.Length > 3 ? content.Substring(3).TrimStart() : "";
                currentPositionInContent = 0; // Position of "REM"
            }
            
            var remSourcePos = new SourcePosition(lineNumber, commentStartColumn + 1); // 1-based column
            tokens.Add(new Tagged<Token>(remSourcePos, new RemToken(comment)));
            currentPositionInContent = content.Length; // Consume rest of line
        }

        while (currentPositionInContent < content.Length)
        {
            char currentChar = content[currentPositionInContent];
            int tokenStartColumn = currentPositionInContent + 1; // 1-based column for SourcePosition

            // 1. Skip Whitespace
            if (Char.IsWhiteSpace(currentChar))
            {
                currentPositionInContent++;
                continue;
            }

            // 2. String Literals
            if (currentChar == '"')
            {
                int stringStart = currentPositionInContent + 1;
                int stringEnd = stringStart;
                var sb = new StringBuilder();
                bool foundEndQuote = false;
                while (stringEnd < content.Length)
                {
                    if (content[stringEnd] == '"')
                    {
                        if (stringEnd + 1 < content.Length && content[stringEnd + 1] == '"') // Escaped quote ""
                        {
                            sb.Append('"');
                            stringEnd += 2;
                        }
                        else // End of string
                        {
                            foundEndQuote = true;
                            stringEnd++;
                            break;
                        }
                    }
                    else
                    {
                        sb.Append(content[stringEnd]);
                        stringEnd++;
                    }
                }
                // Even if unterminated, capture what was parsed, as per Haskell example
                tokens.Add(new Tagged<Token>(new SourcePosition(lineNumber, tokenStartColumn), new StringToken(sb.ToString())));
                currentPositionInContent = stringEnd;
                continue;
            }

            // 3. Numbers (Try before operators to handle leading +/- signs correctly)
            // This requires a "longest match" approach or careful ordering with operators.
            // Let's try to parse a number using a substring and FloatParser.
            // We need to determine the end of the potential number.
            int numEnd = currentPositionInContent;
            while (numEnd < content.Length && (Char.IsDigit(content[numEnd]) || content[numEnd] == '.' || 
                   ((content[numEnd] == 'E' || content[numEnd] == 'e' || content[numEnd] == 'D' || content[numEnd] == 'd') && numEnd + 1 < content.Length) ||
                   ((content[numEnd] == '+' || content[numEnd] == '-') && numEnd > currentPositionInContent && (content[numEnd-1] == 'E' || content[numEnd-1] == 'e' || content[numEnd-1] == 'D' || content[numEnd-1] == 'd')) ||
                   ((content[numEnd] == '+' || content[numEnd] == '-') && numEnd == currentPositionInContent && numEnd + 1 < content.Length && (Char.IsDigit(content[numEnd+1]) || content[numEnd+1] == '.'))
                   ))
            {
                // Special check for E/D followed by sign
                if ((content[numEnd] == 'E' || content[numEnd] == 'e' || content[numEnd] == 'D' || content[numEnd] == 'd') &&
                    numEnd + 1 < content.Length && (content[numEnd + 1] == '+' || content[numEnd + 1] == '-'))
                {
                    numEnd++; // include the sign
                }
                numEnd++;
            }

            if (numEnd > currentPositionInContent)
            {
                string potentialNumber = content.Substring(currentPositionInContent, numEnd - currentPositionInContent);
                if (FloatParser.TryParseFloat(potentialNumber, out double floatVal))
                {
                    // Check if this number is followed by an identifier char, which means it's part of an identifier (e.g. A1)
                    // unless it's an exponent like 1E2 (handled by TryParseFloat)
                    bool isPartOfIdentifier = numEnd < content.Length && (content[numEnd] == '$' || content[numEnd] == '%');
                    if (!isPartOfIdentifier || (potentialNumber.ToUpper().Contains('E') || potentialNumber.ToUpper().Contains('D')))
                    {
                        // This looks a lot like a number ... but +1 could either be `1 +1` or `A = +1`.  Take a look
                        // at the previous token to figure out what do with with a leading '+' or '-'.
                        Token? prevToken = null;
                        if ((tokens.Count > 0) && (potentialNumber[0] is '+' or '-'))
                        {
                            // If the previous token is an operator, this is a unary operator, otherwise it's a number
                            prevToken = tokens.Last().Value;
                        }
                        if (prevToken is not null)
                        {
                            if ((prevToken is FloatToken) || (prevToken is VarNameToken) || (prevToken is RParenToken))
							{
                                var op = potentialNumber[0] == '+' ? BinOp.AddOp : BinOp.SubOp;
								tokens.Add(new Tagged<Token>(new SourcePosition(lineNumber, tokenStartColumn), new OpToken(op)));
							}
						}

						tokens.Add(new Tagged<Token>(new SourcePosition(lineNumber, tokenStartColumn), new FloatToken(floatVal)));
                         currentPositionInContent = numEnd;
                         continue;
                    }
                }
            }


            // 4. Operators (longest match first)
            bool operatorFound = false;
            foreach (var opSymbol in OrderedOperatorSymbols)
            {
                if (content.Substring(currentPositionInContent).StartsWith(opSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    tokens.Add(new Tagged<Token>(new SourcePosition(lineNumber, tokenStartColumn), new OpToken(Operators[opSymbol])));
                    currentPositionInContent += opSymbol.Length;
                    operatorFound = true;
                    break;
                }
            }
            if (operatorFound) continue;

            // 5. Special Characters
            switch (currentChar)
            {
                case '(': tokens.Add(new Tagged<Token>(new SourcePosition(lineNumber, tokenStartColumn), new LParenToken())); currentPositionInContent++; continue;
                case ')': tokens.Add(new Tagged<Token>(new SourcePosition(lineNumber, tokenStartColumn), new RParenToken())); currentPositionInContent++; continue;
                case ',': tokens.Add(new Tagged<Token>(new SourcePosition(lineNumber, tokenStartColumn), new CommaToken())); currentPositionInContent++; continue;
                case ';': tokens.Add(new Tagged<Token>(new SourcePosition(lineNumber, tokenStartColumn), new SemicolonToken())); currentPositionInContent++; continue;
                case '=': tokens.Add(new Tagged<Token>(new SourcePosition(lineNumber, tokenStartColumn), new EqualsToken())); currentPositionInContent++; continue;
				case '?': tokens.Add(new Tagged<Token>(new SourcePosition(lineNumber, tokenStartColumn), new KeywordToken(KeywordType.PRINT))); currentPositionInContent++; continue;
			}

			// 6. Keywords, Builtins, or Variable Names
			if (Char.IsLetter(currentChar))
			{
                int identEnd = currentPositionInContent;
				string identifier = content.Substring(currentPositionInContent, identEnd - currentPositionInContent);

                if (!(Builtins.ContainsKey(identifier) || Keywords.ContainsKey(identifier)))
				    while (identEnd < content.Length && (Char.IsLetterOrDigit(content[identEnd]) || content[identEnd] == '$' || content[identEnd] == '%'))
                    {
                        identEnd++;
					    identifier = content.Substring(currentPositionInContent, identEnd - currentPositionInContent);

					    if (Builtins.ContainsKey(identifier)) break;
					    if (Keywords.ContainsKey(identifier)) break;
				    }

                // Check for DATA keyword specifically, as they consume the rest of the line differently.
                if (identifier.Equals("DATA", StringComparison.OrdinalIgnoreCase))
                {
                    int dataContentStart = currentPositionInContent + 4; // Length of "DATA"
                    // Skip exactly one space if present, otherwise content starts immediately after DATA
                    if (dataContentStart < content.Length && content[dataContentStart] == ' ')
                    {
                        dataContentStart++;
                    }
                    else if (dataContentStart < content.Length && !Char.IsWhiteSpace(content[dataContentStart]))
                    {
                        // No space after DATA, content starts immediately
                    }
                    else // Whitespace other than a single space, or end of line
                    {
                        while (dataContentStart < content.Length && Char.IsWhiteSpace(content[dataContentStart]))
                        {
                            dataContentStart++;
                        }
                    }

                    string rawDataContent = "";
                    int colonPos = content.IndexOf(':', dataContentStart);
                    if (colonPos != -1)
                    {
                        rawDataContent = content.Substring(dataContentStart, colonPos - dataContentStart);
                        // currentPositionInContent will be set to colonPos by the outer loop later if needed
                        // For now, we tokenize DATA content, and the colon will be the next token.
                        // This needs careful adjustment of currentPositionInContent after adding DataContentToken.
                        identEnd = colonPos; // The DATA content ends before the colon.
                    }
                    else
                    {
                        rawDataContent = content.Substring(dataContentStart);
                        identEnd = content.Length; // Consumed till end of line
                    }

                    tokens.Add(new Tagged<Token>(new SourcePosition(lineNumber, tokenStartColumn), new KeywordToken(KeywordType.DATA)));
                }
                else if (Builtins.TryGetValue(identifier, out Builtin builtinFunc))
                {
                    tokens.Add(new Tagged<Token>(new SourcePosition(lineNumber, tokenStartColumn), new BuiltinFuncToken(builtinFunc)));
                    currentPositionInContent = identEnd;
                }
                else if (Keywords.TryGetValue(identifier, out KeywordType keywordType))
                {
                    if (keywordType == KeywordType.REM)
                    {
                        // Handle REM comments specially
                        int commentStart = currentPositionInContent + identifier.Length; // Position after "REM"
                        string comment = content.Substring(commentStart).TrimStart(); // Get the rest of the line as comment
                        var remSourcePos = new SourcePosition(lineNumber, tokenStartColumn + 1); // 1-based column for REM
						tokens.Add(new Tagged<Token>(remSourcePos, new RemToken(comment)));
						currentPositionInContent = content.Length; // Consume rest of line
					}
                    else
                    {
						tokens.Add(new Tagged<Token>(new SourcePosition(lineNumber, tokenStartColumn), new KeywordToken(keywordType)));
						currentPositionInContent = identEnd;
					}
				}
                else // Variable Name
                {
                    string namePart = identifier;
                    ValType typeSuffix = ValType.FloatType; // Default
                    if (identifier.EndsWith('$'))
                    {
                        typeSuffix = ValType.StringType;
                        namePart = identifier.Substring(0, identifier.Length - 1);
                    }
                    else if (identifier.EndsWith('%'))
                    {
                        typeSuffix = ValType.IntType;
                        namePart = identifier.Substring(0, identifier.Length - 1);
                    }

                    if (String.IsNullOrEmpty(namePart) || !Char.IsLetter(namePart[0]))
                    {
                        tokens.Add(new Tagged<Token>(new SourcePosition(lineNumber, tokenStartColumn), new UnknownToken(identifier)));
                    }
                    else
                    {
                        tokens.Add(new Tagged<Token>(new SourcePosition(lineNumber, tokenStartColumn), new VarNameToken(namePart, typeSuffix)));
                    }
                    currentPositionInContent = identEnd;
                }
                continue;
            }

            // 7. If nothing else, it's an UnknownToken (or statement separator ':')
            if (currentChar == ':')
            {
                // Tokenize colon as an UnknownToken for now, Parser will handle it.
                // Or define a specific ColonToken. For simplicity:
                tokens.Add(new Tagged<Token>(new SourcePosition(lineNumber, tokenStartColumn), new UnknownToken(":")));
                currentPositionInContent++;
                continue;
            }
            tokens.Add(new Tagged<Token>(new SourcePosition(lineNumber, tokenStartColumn), new UnknownToken(currentChar.ToString())));
            currentPositionInContent++;
        }

        tokens.Add(new Tagged<Token>(new SourcePosition(lineNumber, currentPositionInContent + 1), new EolToken()));
        return tokens;
    }

}
