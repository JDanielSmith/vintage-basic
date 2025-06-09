using System.Text;
using VintageBasic.Syntax; // For ValType, Builtin, BinOp
using System.Collections.Frozen;
using VintageBasic.Runtime;

namespace VintageBasic.Parsing;

using TokenPair = (Tagged<Token>, Tagged<Token>?);

static class Tokenizer
{
	public static IEnumerable<Tagged<Token>> Tokenize(ScannedLine scannedLine)
    {
		Implementation implementation = new(scannedLine);
		return implementation.Tokenize();
    }
}
file sealed class Implementation(ScannedLine scannedLine)
{
	static readonly FrozenDictionary<string, KeywordType> Keywords = new Dictionary<string, KeywordType>() {
		{"LET", KeywordType.LET}, {"PRINT", KeywordType.PRINT}, {"IF", KeywordType.IF}, {"THEN", KeywordType.THEN},
		{"FOR", KeywordType.FOR}, {"TO", KeywordType.TO}, {"STEP", KeywordType.STEP}, {"NEXT", KeywordType.NEXT},
		{"GOTO", KeywordType.GOTO}, {"GOSUB", KeywordType.GOSUB}, {"RETURN", KeywordType.RETURN}, {"END", KeywordType.END},
		{"DATA", KeywordType.DATA}, {"READ", KeywordType.READ}, {"INPUT", KeywordType.INPUT}, {"DIM", KeywordType.DIM},
		{"REM", KeywordType.REM}, {"ON", KeywordType.ON}, {"RESTORE", KeywordType.RESTORE}, {"STOP", KeywordType.STOP},
		{"RANDOMIZE", KeywordType.RANDOMIZE}, {"DEF", KeywordType.DEF}, {"FN", KeywordType.FN},
		{ "OR", KeywordType.OR}, { "AND", KeywordType.AND}, { "NOT", KeywordType.NOT},
		// Note: ELSE is not in Haskell's KeywordTok but might be useful for parser. Added to enum.
	}.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

	static readonly FrozenDictionary<string, Builtin> Builtins = new Dictionary<string, Builtin>() {
		{"ABS", Builtin.Abs}, {"ASC", Builtin.Asc }, {"ATN", Builtin.Atn },
		{"CHR$", Builtin.Chr }, {"COS", Builtin.Cos },
		{"EXP", Builtin.Exp },
		{"INT", Builtin.Int },
		{"LEFT$", Builtin.Left } /* Corresponds to LeftBI */, {"LEN", Builtin.Len }, {"LOG", Builtin.Log },
		{"MID$", Builtin.Mid } /* Corresponds to MidBI */,
		{"RIGHT$", Builtin.Right } /* Corresponds to RightBI */, {"RND", Builtin.Rnd },
		{"SGN", Builtin.Sgn }, {"SIN", Builtin.Sin }, 	{"SPC", Builtin.Spc }, {"SQR", Builtin.Sqr }, {"STR$", Builtin.Str },
		{"TAB", Builtin.Tab }, {"TAN", Builtin.Tan },
		{"VAL", Builtin.Val },
	}.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

	// Note: "=" is handled as EqualsToken separately. AND/OR are keywords for BinOpExpression.
	static readonly FrozenDictionary<string, BinOp> Operators = OpToken.Symbols.ToFrozenDictionary(kvp => kvp.Value, kvp => kvp.Key);

	// For longest match, order operators from longest to shortest.
	static readonly IReadOnlyList<string> OrderedOperatorSymbols = [.. Operators.Keys.OrderByDescending(k => k.Length)];

	readonly string content = scannedLine.Content;
	int currentPositionInContent; // 0-based index into content string
	readonly int lineNumber = scannedLine.LineNumber ?? 0;

	Tagged<Token> CreateRemToken()
	{
		var tokenContent = content[0] == '\'' ? content[1..] : content.Length > 3 ? content[3..].TrimStart() : ""; // Skip "REM " or "'"

		int commentStartColumn = currentPositionInContent;
		currentPositionInContent = content.Length; // Consume rest of line
		return new(new(lineNumber, commentStartColumn + 1), new RemToken(tokenContent)); // 1-based column
	}

	int tokenStartColumn; // 1-based column for SourcePosition

	Tagged<Token> CreateTaggedlToken(Token token)
	{
		return new(new(lineNumber, tokenStartColumn), token);
	}

	Tagged<Token> CreateStringToken()
	{
		int stringStart = currentPositionInContent + 1;
		int stringEnd = stringStart;
		StringBuilder sb = new();
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
		currentPositionInContent = stringEnd; // Even if unterminated, capture what was parsed, as per Haskell example
		return CreateTaggedlToken(new StringToken(sb.ToString()));
	}

	static bool IsExponentChar(char c)
	{
		return c is 'E' or 'e' or  'D' or 'd';
	}
	static bool ContainsExponent(string potentialNumber)
	{
		return potentialNumber.IndexOfAny([ 'E', 'e', 'D', 'd' ]) >= 0;
	}
	static bool IsSignChar(char c)
	{
		return c is '+' or '-';
	}
	static bool IsDecimalChar(char c)
	{
		return Char.IsDigit(c) || c  is '.'; // Decimal point
	}
	bool IsValidNumberChar(int numEnd)
	{
		if (numEnd >= content.Length) return false;

		if (IsDecimalChar(content[numEnd])) return true;
		if (IsExponentChar(content[numEnd]) && numEnd + 1 < content.Length) return true;

		if (!IsSignChar(content[numEnd])) return false;
		if (numEnd > currentPositionInContent && IsExponentChar(content[numEnd - 1])) return true;
		if (numEnd == currentPositionInContent && numEnd + 1 < content.Length && IsDecimalChar(content[numEnd + 1])) return true;

		return false;
	}

	static bool IsVariableSuffix(char c)
	{
		return c is '$' or '%'; // String or Integer suffix
	}

	Tagged<Token>? PreviousToken;
	TokenPair? CreateNumberToken()
	{
		// This requires a "longest match" approach or careful ordering with operators.
		// Let's try to parse a number using a substring and FloatParser.
		// We need to determine the end of the potential number.
		int numEnd = currentPositionInContent;
		while (IsValidNumberChar(numEnd))
		{
			if (IsExponentChar(content[numEnd]) && numEnd + 1 < content.Length && IsSignChar(content[numEnd + 1]))
			{
				numEnd++; // include the sign
			}
			numEnd++;
		}
		if (numEnd <= currentPositionInContent)
			return null;

		string potentialNumber = content[currentPositionInContent..numEnd];
		if (!FloatParser.TryParseFloat(potentialNumber, out var floatVal))
			return null;

		// Check if this number is followed by an identifier char, which means it's part of an identifier (e.g. A1)
		// unless it's an exponent like 1E2 (handled by TryParseFloat)
		bool isPartOfIdentifier = numEnd < content.Length && IsVariableSuffix(content[numEnd]);
		bool looks_like_a_number = !isPartOfIdentifier || ContainsExponent(potentialNumber);
		if (!looks_like_a_number)
			return null;

		// This looks a lot like a number ... but +1 could either be `1 +1` or `A = +1`.  Take a look
		// at the previous token to figure out what do with with a leading '+' or '-'.
		Tagged<Token>? opToken = null;
		if (PreviousToken is not null)
		{
			var prevToken = PreviousToken.Value;
			if (prevToken is FloatToken or VarNameToken or RParenToken)
			{
				var op = potentialNumber[0] == '+' ? BinOp.AddOp : BinOp.SubOp;
				opToken = CreateTaggedlToken(new OpToken(op));
			}
		}
		currentPositionInContent = numEnd;
		return (CreateTaggedlToken(new FloatToken(floatVal)), opToken);
	}

	Tagged<Token>? CreateOpToken()
	{
		foreach (var opSymbol in OrderedOperatorSymbols)
		{
			if (content[currentPositionInContent..].StartsWith(opSymbol, StringComparison.OrdinalIgnoreCase))
			{
				currentPositionInContent += opSymbol.Length;
				return CreateTaggedlToken(new OpToken(Operators[opSymbol]));
			}
		}
		return null;
	}

	char CurrentChar => content[currentPositionInContent];
	Tagged<Token>? CreateSpecialToken()
	{
		Tagged<Token> CreateTaggedlToken_(Token token)
		{
			currentPositionInContent++;
			return CreateTaggedlToken(token);
		}
		return CurrentChar switch
		{
			'(' => CreateTaggedlToken_(new LParenToken()),
			')' => CreateTaggedlToken_(new RParenToken()),
			',' => CreateTaggedlToken_(new CommaToken()),
			';' => CreateTaggedlToken_(new SemicolonToken()),
			'=' => CreateTaggedlToken_(new EqualsToken()),
			'?' => CreateTaggedlToken_(new KeywordToken(KeywordType.PRINT)),
			_ => null
		};
	}

	Tagged<Token> CreateTokenFromLetter()
	{
		int identEnd = currentPositionInContent;
		string identifier = content[currentPositionInContent..identEnd];

		if (!(Builtins.ContainsKey(identifier) || Keywords.ContainsKey(identifier)))
			while (identEnd < content.Length && (Char.IsLetterOrDigit(content[identEnd]) || IsVariableSuffix(content[identEnd])))
			{
				identEnd++;
				identifier = content[currentPositionInContent..identEnd];

				if (Builtins.ContainsKey(identifier)) break;
				if (Keywords.ContainsKey(identifier)) break;
			}

		// Check for DATA keyword specifically, as they consume the rest of the line differently.
		if (identifier.Equals("DATA", StringComparison.OrdinalIgnoreCase))
		{
			return CreateTaggedlToken(new KeywordToken(KeywordType.DATA));
		}
		if (Builtins.TryGetValue(identifier, out var builtinFunc))
		{
			currentPositionInContent = identEnd;
			return CreateTaggedlToken(new BuiltinFuncToken(builtinFunc));
		}
		if (Keywords.TryGetValue(identifier, out var keywordType))
		{
			if (keywordType == KeywordType.REM)
			{
				// Handle REM comments specially
				int commentStart = currentPositionInContent + identifier.Length; // Position after "REM"
				string comment = content[commentStart..].TrimStart(); // Get the rest of the line as comment
				currentPositionInContent = content.Length; // Consume rest of line
				return new(new(lineNumber, tokenStartColumn + 1), new RemToken(comment)); // 1-based column for REM
			}

			currentPositionInContent = identEnd;
			return CreateTaggedlToken(new KeywordToken(keywordType));
		}

		// Variable Name
		var namePart = identifier;
		Val typeSuffix = FloatVal.Empty; // Default
		var suffix = identifier[^1];
		if (IsVariableSuffix(suffix))
		{
			typeSuffix = suffix is '$' ? StringVal.Empty : IntVal.Empty;
			namePart = identifier[..^1];
		}

		currentPositionInContent = identEnd;
		if (String.IsNullOrEmpty(namePart) || !Char.IsLetter(namePart[0]))
		{
			return CreateTaggedlToken(new UnknownToken(identifier));
		}
		return CreateTaggedlToken(new VarNameToken(namePart, typeSuffix));
	}

	Tagged<Token> CreateUnknownToken()
	{
		if (CurrentChar == ':')
		{
			// Tokenize colon as an UnknownToken for now, Parser will handle it.
			// Or define a specific ColonToken. For simplicity:
			currentPositionInContent++;
			return CreateTaggedlToken(new UnknownToken(":"));
		}
		var retval = CreateTaggedlToken(new UnknownToken(CurrentChar.ToString()));
		currentPositionInContent++; // CurrentChar uses currentPositionInContent, so increment after creating token
		return retval;
	}

	TokenPair? GetTokens()
	{
		tokenStartColumn = currentPositionInContent + 1; // 1-based column for SourcePosition

		// 1. Skip Whitespace
		if (Char.IsWhiteSpace(CurrentChar))
		{
			currentPositionInContent++;
			return null;
		}

		// 2. String Literals
		if (CurrentChar is '"')
		{
			return (CreateStringToken(), null);
		}

		// 3. Numbers (Try before operators to handle leading +/- signs correctly)
		if (CreateNumberToken() is TokenPair numberTokens)
		{
			return numberTokens; // Return the FloatToken and potentially an OpToken
		}

		// 4. Operators (longest match first)
		if (CreateOpToken() is Tagged<Token> opToken)
		{
			return (opToken, null);
		}
		// 5. Special Characters
		if (CreateSpecialToken() is Tagged<Token> specialToken)
		{
			return (specialToken, null);
		}

		// 6. Keywords, Builtins, or Variable Names
		if (Char.IsLetter(CurrentChar))
		{
			return (CreateTokenFromLetter(), null);
		}

		// 7. If nothing else, it's an UnknownToken (or statement separator ':')
		return (CreateUnknownToken(), null);
	}
	public IEnumerable<Tagged<Token>> Tokenize()
	{
		// Handle REM at the very start of content (after potential line number)
		if (content.StartsWith("REM", StringComparison.OrdinalIgnoreCase) || ((content.Length > 0) && (content[0] == '\''))) // Some BASICs use ' for REM
		{
			yield return CreateRemToken();
		}

		while (currentPositionInContent < content.Length)
		{
			var theTokens = GetTokens();
			PreviousToken = null;
			if (theTokens is not null)
			{
				if (theTokens.Value.Item2 is not null)
					yield return theTokens.Value.Item2;
				PreviousToken = theTokens.Value.Item1;
				yield return PreviousToken;
			}
		}

		yield return new(new(lineNumber, currentPositionInContent + 1), new EolToken());
	}
}
