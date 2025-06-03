using System.Text.RegularExpressions;

namespace vintage_basic.Language.VintageBasic;

sealed record class Tagged<T>(int Position, T Value);

static class LexCommon
{
	// Parses a single whitespace character
	public static bool IsWhiteSpaceChar(char c) => " \v\f\t".Contains(c);

	// Parses a stretch of whitespace
	public static void SkipWhiteSpace(string input, ref int index)
	{
		while (index < input.Length && IsWhiteSpaceChar(input[index]))
			index++;
	}

	// Parses a legal BASIC character
	public static bool IsLegalChar(char c)
	{
		return Char.IsLetterOrDigit(c) || ",:;()$%=<>+-*/^?".Contains(c);
	}

	// Parses a line number
	public static int? ParseLabel(string input)
	{
		var match = Regex.Match(input, @"^\d+");
		return match.Success ? int.Parse(match.Value) : (int?)null;
	}
}
