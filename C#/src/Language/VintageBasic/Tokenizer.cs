using System.Linq;

namespace vintage_basic.Language.VintageBasic;

/// <summary
/// Finds and tokenizes BASIC keywords, in preparation for parsing. This allows
/// keywords to be read even if there are no spaces around them. Even though
/// the standard disallows it, many BASIC implementations allowed this to save
/// memory or screen real estate. The downside is that longer variable names
/// are not practical, since they might contain keywords.
/// </summary>

abstract record Token;
sealed record StringTok(string Value) : Token;
sealed record RemTok(string Value) : Token;
sealed record DataTok(string Value) : Token;
sealed record CharTok(char Value) : Token;
sealed record BuiltinTok(Builtin Builtin) : Token;
sealed record EqTok() : Token;
sealed record NETok() : Token;
sealed record LETok() : Token;
sealed record LTTok() : Token;
sealed record GETok() : Token;
sealed record GTTok() : Token;
sealed record PlusTok() : Token;
sealed record MinusTok() : Token;
sealed record MulTok() : Token;
sealed record DivTok() : Token;
sealed record PowTok() : Token;
sealed record AndTok() : Token;
sealed record OrTok() : Token;
sealed record NotTok() : Token;
sealed record LetTok() : Token;
sealed record DimTok() : Token;
sealed record OnTok() : Token;
sealed record GoTok() : Token;
sealed record SubTok() : Token;
sealed record ReturnTok() : Token;
sealed record IfTok() : Token;
sealed record ThenTok() : Token;
sealed record ForTok() : Token;
sealed record ToTok() : Token;
sealed record StepTok() : Token;
sealed record NextTok() : Token;
sealed record PrintTok() : Token;
sealed record InputTok() : Token;
sealed record RandomizeTok() : Token;
sealed record ReadTok() : Token;
sealed record RestoreTok() : Token;
sealed record DefTok() : Token;
sealed record FnTok() : Token;
sealed record EndTok() : Token;
sealed record StopTok() : Token;
sealed record SpaceTok() : Token;
sealed record DotTok() : Token;
sealed record CommaTok() : Token;
sealed record ColonTok() : Token;
sealed record SemiTok() : Token;
sealed record LParenTok() : Token;
sealed record RParenTok() : Token;
sealed record DollarTok() : Token;
sealed record PercentTok() : Token;

class Tokenizer
{
	public static List<Tagged<Token>> Tokenize(string input)
	{
		var tokens = new List<Tagged<Token>>();
		Tokenizer tokenizer = new();
		foreach (var token in tokenizer.TokenizeString(input))
		{
			tokens.Add(token);
		}
		return tokens;
	}

	int Index;
	string Input = String.Empty;

	public static bool IsStringToken(Token _) => _ is StringTok;

	public static bool IsRemToken(Token _) => _ is RemTok;

	public static bool IsDataToken(Token _) => _ is DataTok;

	Token? Lex()
	{
		char c = Input[Index];

		if (LexCommon.IsWhiteSpaceChar(c))
		{
			LexCommon.SkipWhiteSpace(Input, ref Index);
			return new SpaceTok();
		}

		if (c == '"')
		{
			int start = Index;
			Index++;
			while (Index < Input.Length && Input[Index] != '"')
			{
				// no special escape chars allowed
				Index++;
			}
			if (Index >= Input.Length) throw new ArgumentException("Unterminated string literal.");
			return new StringTok(Input[start..Index]);
		}

		string line = Input[Index..];
		if (line.StartsWith("REM", StringComparison.OrdinalIgnoreCase))
		{
			Index = Input.Length; // consume the rest of the line
			return new RemTok(line);
		}

		if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
		{
			Index = Input.Length; // consume the rest of the line
			return new DataTok(line);
		}

		foreach (var key in strToTokAssoc.Keys)
		{
			if (line.StartsWith(key, StringComparison.OrdinalIgnoreCase))
			{
				Index += key.Length - 1;
				return strToTokAssoc[key];
			}
		}

		if (LexCommon.IsLegalChar(c))
			return new CharTok(Char.ToUpper(c));

		return null;
	}

	IEnumerable<Tagged<Token>> TokenizeString(string input)
	{
		Input = input;
		for (Index = 0; Index < Input.Length; Index++)
		{
			int position = Index;
			Token? token = Lex();
			if (token is not null)
			{
				yield return new Tagged<Token>(position, token);
				continue;
			}

			if (Index >= Input.Length) break;
			throw new ArgumentException($"Illegal character '{Input[Index]}' at position {position}.");
		}
	}

	static readonly Dictionary<string, Token> strToTokAssoc_ = new()
	{
		{ ",", new CommaTok() },
		{ ":", new ColonTok() },
		{ ";", new SemiTok() },
		{ "(", new LParenTok() },
		{ ")", new RParenTok() },
		{ "$", new DollarTok() },
		{ "%", new PercentTok() },
		{ "=", new EqTok() },
		{ "<>", new NETok() },
		{ "<=", new LETok() },
		{ "<", new LTTok() },
		{ ">=", new GETok() },
		{ ">", new GTTok() },
		{ "+", new PlusTok() },
		{ "-", new MinusTok() },
		{ "*", new MulTok() },
		{ "/", new DivTok() },
		{ "^", new PowTok() },
		{ ".", new DotTok() },
		{ "AND", new AndTok() },
		{ "OR", new OrTok() },
		{ "NOT", new NotTok() },
		{ "LET", new LetTok() },
		{ "DIM", new DimTok() },
		{ "ON", new OnTok() },
		{ "GO", new GoTok() },
		{ "SUB", new SubTok() },
		{ "RETURN", new ReturnTok() },
		{ "IF", new IfTok() },
		{ "THEN", new ThenTok() },
		{ "FOR", new ForTok() },
		{ "TO", new ToTok() },
		{ "STEP", new StepTok() },
		{ "NEXT", new NextTok() },
		{ "PRINT", new PrintTok() },
		{ "?", new PrintTok() },
		{ "INPUT", new InputTok() },
		{ "RANDOMIZE", new RandomizeTok() },
		{ "READ", new ReadTok() },
		{ "RESTORE", new RestoreTok() },
		{ "DEF", new DefTok() },
		{ "FN", new FnTok() },
		{ "END", new EndTok() },
		{ "STOP", new StopTok() }
	};
	static Dictionary<string, Token> makeStrToTokAssoc()
	{
		var assoc = new Dictionary<string, Token>();
		foreach (var pair in strToTokAssoc_)
		{
			assoc[pair.Key] = pair.Value;
		}
		foreach (var pair in Builtins.builtinToStrAssoc)
		{
			assoc[pair.Value] = new BuiltinTok(pair.Key);
		}
		return assoc;
	}
	public static readonly Dictionary<string, Token> strToTokAssoc = makeStrToTokAssoc();

	static Dictionary<Token, string> makeTokToStrAssoc()
	{
		var assoc = new Dictionary<Token, string>();
		foreach (var pair in strToTokAssoc)
		{
			if (!assoc.ContainsKey(pair.Value))
				assoc[pair.Value] = pair.Key;
		}
		return assoc;
	}
	public static readonly Dictionary<Token, string> tokToStrAssoc = makeTokToStrAssoc();

	public static bool IsBuiltinToken(Token _) => _ is BuiltinTok;

	/// <summary>
	/// Prettyprint a token for error reporting or debugging.
	/// </summary>
	public static string PrintToken(Token token)
	{
		if (tokToStrAssoc.TryGetValue(token, out string? str))
		{
			return str;
		}
		return token switch
		{
			CharTok charTok => $"[{charTok.Value}]",
			DataTok dataTok => $"DATA {dataTok.Value}",
			RemTok remTok => $"REM {remTok.Value}",
			SpaceTok => " ",
			StringTok stringTok => $"\"{stringTok.Value}\"",
			_ => throw new InvalidOperationException("unrecognized token.")
		};
	}
}