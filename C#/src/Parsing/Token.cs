using System.Collections.Frozen;
using VintageBasic.Runtime;
using VintageBasic.Syntax;

namespace VintageBasic.Parsing;

enum KeywordType
{
    LET, PRINT, IF, THEN, ELSE, // ELSE might be needed by parser, not in Haskell Tokenizer.hs's KeywordTok
    FOR, TO, STEP, NEXT,
    GOTO, GOSUB, RETURN, END,
    DATA, READ, INPUT, DIM, REM,
    ON, RESTORE, STOP, RANDOMIZE,
    DEF, FN,
    AND, OR, NOT,
}

abstract record Token
{
    public virtual string Text => ""; // Default text representation, can be overridden
}

sealed record KeywordToken(KeywordType Keyword) : Token
{
    public override string Text => Keyword.ToString();
}

sealed record BuiltinFuncToken(Builtin FuncName) : Token
{
    public override string Text => FuncName.ToString().ToUpperInvariant(); // Match BASIC naming
}

sealed record VarNameToken(string Name, Val Val) : Token
{
	public override string Text => Name + Val.Suffix;
}

sealed record FloatToken(double Value) : Token
{
    public override string Text => $"{Value}";
}

sealed record StringToken(string Value) : Token
{
    public override string Text => $"\"{Value}\"";
}

sealed record OpToken(BinOp Op) : Token
{
    internal static readonly FrozenDictionary<BinOp, string> Symbols = new Dictionary<BinOp, string>() {
        {BinOp.AddOp, "+"},
        {BinOp.SubOp, "-"},
        {BinOp.MulOp, "*"},
        {BinOp.DivOp, "/"},
        {BinOp.PowOp, "^"},
        {BinOp.NEOp, "<>"},
        {BinOp.LEOp, "<="},
        {BinOp.GEOp, ">="},
        {BinOp.LTOp, "<"},
        {BinOp.GTOp, ">"},
    }.ToFrozenDictionary();
    public override string Text => Symbols.TryGetValue(Op, out var symbol) ? symbol : Op switch
	{
		BinOp.EqOp => "=",
		BinOp.AndOp => "AND",
		BinOp.OrOp => "OR",
		_ => throw new ArgumentOutOfRangeException(nameof(Op), $"Unknown binary operator: {Op}")
	};
}

sealed record LParenToken : Token { public override string Text => "("; }
sealed record RParenToken : Token { public override string Text => ")"; }
sealed record CommaToken : Token { public override string Text => ","; }
sealed record SemicolonToken : Token { public override string Text => ";"; }

// Equals can be an assignment or comparison. Tokenizer might emit generic EqualsTok, parser differentiates.
// For now, OpToken(BinOp.EqOp) covers comparison. Assignment context is handled by parser.
// Let's add a distinct EqualsTok for assignment/equals symbol not part of <>, <=, >=
sealed record EqualsToken : Token { public override string Text => "="; }


sealed record EolToken : Token { public override string Text => "<EOL>"; } // End of Line

// For REM statements, the content is part of the token
sealed record RemToken(string Comment) : Token 
{
    public override string Text => "REM" + Comment;
}

// For DATA statements, stores the raw content after the DATA keyword
sealed record DataContentToken(string RawContent) : Token
{
    public override string Text => RawContent;
}

sealed record UnknownToken(string Content) : Token
{
    public override string Text => Content;
}
