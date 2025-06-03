/// <summary>
/// A representation of BASIC's builtin functions.
/// </summary>

namespace vintage_basic.Language.VintageBasic;

/// <summary>
/// An enumeration of BASIC's builtin functions.
/// </summary>
record Builtin() : Token;
sealed record AbsBI() : Builtin;
sealed record AscBI() : Builtin;
sealed record AtnBI() : Builtin;
sealed record ChrBI() : Builtin;
sealed record CosBI() : Builtin;
sealed record ExpBI() : Builtin;
sealed record IntBI() : Builtin;
sealed record LeftBI() : Builtin;
sealed record LenBI() : Builtin;
sealed record LogBI() : Builtin;
sealed record MidBI() : Builtin;
sealed record RightBI() : Builtin;
sealed record RndBI() : Builtin;
sealed record SgnBI() : Builtin;
sealed record SinBI() : Builtin;
sealed record SpcBI() : Builtin;
sealed record StrBI() : Builtin;
sealed record SqrBI() : Builtin;
sealed record TabBI() : Builtin;
sealed record TanBI() : Builtin;
sealed record ValBI() : Builtin;

/// <summary>
/// An association list mapping BASIC builtins to their string representation.
/// It is used forwards to print BASIC code, and backwards to parse BASIC code.
/// </summary>
static class Builtins
{
	internal static readonly Dictionary<Builtin, string> builtinToStrAssoc = new()
	{
		{new AbsBI(), "ABS" },
		{new AscBI(), "ASC" },
		{new AtnBI(), "ATN" },
		{new ChrBI(), "CHR$" },
		{new CosBI(), "COS" },
		{new ExpBI(), "EXP" },
		{new IntBI(), "INT" },
		{new LeftBI(), "LEFT$" },
		{new LenBI(), "LEN" },
		{new LogBI(), "LOG" },
		{new MidBI(), "MID$" },
		{new RightBI(), "RIGHT$" },
		{new RndBI(), "RND" },
		{new SgnBI(), "SGN" },
		{new SinBI(), "SIN" },
		{new SpcBI(), "SPC" },
		{new StrBI(), "STR$" },
		{new SqrBI(), "SQR" },
		{new TabBI(), "TAB" },
		{new TanBI(), "TAN" },
		{new ValBI(), "VAL" }
	};
}
