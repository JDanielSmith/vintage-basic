using System.Text.RegularExpressions;

namespace VintageBasic.Parsing;

readonly record struct ScannedLine(int? LineNumber, string Content, int OriginalLineIndex);

static class LineScanner
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
        string trimmedLine = line.TrimStart(); // Corresponds to dropWhile isSpace

		var match = Regex.Match(trimmedLine, @"^\d+");
		int numPartLength = match.Success ? match.Length : 0;
        if (numPartLength == 0) // No numeric part found at the beginning
        {
            // The Haskell version passes 's' (original line) if no numPart.
            // However, typical BASIC might treat lines without numbers as comments or errors,
            // or if allowed, the content starts immediately.
            // The Haskell 'procLine' passes 's' (the original string, not trimmedLine) to LineScan Nothing s idx.
            // Let's stick to the original line 's' for content if no number.
            return new ScannedLine(null, line, originalLineIndex);
        }

        var numStr = trimmedLine[..numPartLength];
		var statPart = trimmedLine[numPartLength..].TrimStart(); // Corresponds to dropWhile isSpace for statPart
        if (Int32.TryParse(numStr, out var lineNumber))
        {
            return new ScannedLine(lineNumber, statPart, originalLineIndex);
        }

        // This case should ideally not happen if numPart only contains digits.
        // However, if numStr is too large for int, TryParse will fail.
        // Haskell's `read` would throw an error. Here, we might return it as a non-numbered line.
        // Or, depending on strictness, this could be an error condition.
        // For now, treat as content if number parsing fails after finding digits.
        return new ScannedLine(null, line, originalLineIndex); 
    }
}
