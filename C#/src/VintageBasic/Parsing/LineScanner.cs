// src/VintageBasic/Parsing/LineScanner.cs
using System.Collections.Generic;
using System.Linq;

namespace VintageBasic.Parsing;

public record ScannedLine(int? LineNumber, string Content, int OriginalLineIndex);

public static class LineScanner
{
    /// <summary>
    /// Converts a list of strings into a list of ScannedLines.
    /// The OriginalLineIndex in ScannedLine is the original 0-based line index.
    /// </summary>
    public static IEnumerable<ScannedLine> ScanLines(IEnumerable<string> lines)
    {
        if (lines == null)
        {
            return Enumerable.Empty<ScannedLine>();
        }

        return lines.Select((line, index) => ProcessLine(line, index));
    }

    private static ScannedLine ProcessLine(string line, int originalLineIndex)
    {
        string trimmedLine = line.TrimStart(); // Corresponds to dropWhile isSpace

        int numPartLength = 0;
        foreach (char c in trimmedLine)
        {
            if (Char.IsDigit(c))
            {
                numPartLength++;
            }
            else
            {
                break;
            }
        }

        if (numPartLength == 0) // No numeric part found at the beginning
        {
            // The Haskell version passes 's' (original line) if no numPart.
            // However, typical BASIC might treat lines without numbers as comments or errors,
            // or if allowed, the content starts immediately.
            // The Haskell 'procLine' passes 's' (the original string, not trimmedLine) to LineScan Nothing s idx.
            // Let's stick to the original line 's' for content if no number.
            return new ScannedLine(null, line, originalLineIndex);
        }
        else
        {
            string numStr = trimmedLine.Substring(0, numPartLength);
            string statPart = trimmedLine.Substring(numPartLength).TrimStart(); // Corresponds to dropWhile isSpace for statPart

            if (int.TryParse(numStr, out int lineNumber))
            {
                return new ScannedLine(lineNumber, statPart, originalLineIndex);
            }
            else
            {
                // This case should ideally not happen if numPart only contains digits.
                // However, if numStr is too large for int, TryParse will fail.
                // Haskell's `read` would throw an error. Here, we might return it as a non-numbered line.
                // Or, depending on strictness, this could be an error condition.
                // For now, treat as content if number parsing fails after finding digits.
                return new ScannedLine(null, line, originalLineIndex); 
            }
        }
    }
}
