// src/VintageBasic/Parsing/Token.cs
using VintageBasic.Syntax; // For ValType, Builtin, BinOp

namespace VintageBasic.Parsing;

public enum KeywordType
{
    LET, PRINT, IF, THEN, ELSE, // ELSE might be needed by parser, not in Haskell Tokenizer.hs's KeywordTok
    FOR, TO, STEP, NEXT,
    GOTO, GOSUB, RETURN, END,
    DATA, READ, INPUT, DIM, REM,
    ON, RESTORE, STOP, RANDOMIZE,
    DEF, FN,
    AND, OR, NOT,
}

public abstract record Token
{
    public virtual string Text => ""; // Default text representation, can be overridden
}

public record KeywordToken(KeywordType Keyword) : Token
{
    public override string Text => Keyword.ToString();
}

public record BuiltinFuncToken(Builtin FuncName) : Token
{
    public override string Text => FuncName.ToString().ToUpperInvariant(); // Match BASIC naming
}

public record VarNameToken(string Name, ValType TypeSuffix) : Token
{
    public override string Text => Name + (TypeSuffix == ValType.StringType ? "$" : (TypeSuffix == ValType.IntType ? "%" : ""));
}

public record FloatToken(double Value) : Token
{
    public override string Text => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

public record StringToken(string Value) : Token
{
    public override string Text => $"\"{Value}\"";
}

public record OpToken(BinOp Op) : Token
{
    public override string Text => Op switch {
        BinOp.AddOp => "+",
        BinOp.SubOp => "-",
        BinOp.MulOp => "*",
        BinOp.DivOp => "/",
        BinOp.PowOp => "^",
        BinOp.EqOp => "=", // Note: Can also be EqualsTok
        BinOp.NEOp => "<>",
        BinOp.LTOp => "<",
        BinOp.LEOp => "<=",
        BinOp.GTOp => ">",
        BinOp.GEOp => ">=",
        BinOp.AndOp => "AND", // Assuming keywords for these, parser will handle
        BinOp.OrOp => "OR",   // Assuming keywords for these, parser will handle
        _ => throw new System.ArgumentOutOfRangeException(nameof(Op), $"Unknown binary operator: {Op}")
    };
}

public record LParenToken : Token { public override string Text => "("; }
public record RParenToken : Token { public override string Text => ")"; }
public record CommaToken : Token { public override string Text => ","; }
public record SemicolonToken : Token { public override string Text => ";"; }

// Equals can be an assignment or comparison. Tokenizer might emit generic EqualsTok, parser differentiates.
// For now, OpToken(BinOp.EqOp) covers comparison. Assignment context is handled by parser.
// Let's add a distinct EqualsTok for assignment/equals symbol not part of <>, <=, >=
public record EqualsToken : Token { public override string Text => "="; }


public record EolToken : Token { public override string Text => "<EOL>"; } // End of Line

// For REM statements, the content is part of the token
public record RemToken(string Comment) : Token 
{
    public override string Text => "REM" + Comment;
}

// For DATA statements, stores the raw content after the DATA keyword
public record DataContentToken(string RawContent) : Token
{
    public override string Text => RawContent;
}

public record UnknownToken(string Content) : Token
{
    public override string Text => Content;
}
