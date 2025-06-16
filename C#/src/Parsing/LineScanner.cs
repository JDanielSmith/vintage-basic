using System.Text.RegularExpressions;

namespace VintageBasic.Parsing;

static partial class LineScanner
{
	/// <summary>
	/// Converts a list of strings into a list of ScannedLines.
	/// The OriginalLineIndex in ScannedLine is the original 0-based line index.
	/// </summary>
	public static IEnumerable<ScannedLine> ScanLines(IEnumerable<string> lines)
	{
		return lines.Select(ProcessLine);
	}
	static ScannedLine ProcessLine(string line, int originalLineIndex)
	{
		var trimmedLine = line.TrimStart(); // Corresponds to dropWhile isSpace

		var match = MyRegex().Match(trimmedLine);
		if (!match.Success) // No numeric part found at the beginning
		{
			// The Haskell version passes 's' (original line) if no numPart.
			// However, typical BASIC might treat lines without numbers as comments or errors,
			// or if allowed, the content starts immediately.
			// The Haskell 'procLine' passes 's' (the original string, not trimmedLine) to LineScan Nothing s idx.
			// Let's stick to the original line 's' for content if no number.
			//return new(null, line, originalLineIndex);
			throw new InvalidOperationException("No line number");
		}

		var numPartLength = match.Length;
		if (Int32.TryParse(trimmedLine[..numPartLength], out var lineNumber))
		{
			var statPart = trimmedLine[numPartLength..].TrimStart(); // Corresponds to dropWhile isSpace for statPart
			return new(lineNumber, statPart, originalLineIndex);
		}

		// This case should ideally not happen if numPart only contains digits.
		// However, if numStr is too large for int, TryParse will fail.
		// Haskell's `read` would throw an error. Here, we might return it as a non-numbered line.
		// Or, depending on strictness, this could be an error condition.
		// For now, treat as content if number parsing fails after finding digits.
		//return new(null, line, originalLineIndex);
		throw new InvalidOperationException("No line number");
	}

	[GeneratedRegex(@"^\d+")]
	private static partial Regex MyRegex();
}

sealed record ScannedLine(int LineNumber, string Content, int OriginalLineIndex);
