using System.Text.RegularExpressions;

namespace vintage_basic.Language.VintageBasic;

sealed record RawLine(int Label, string Content);

static class LineScanner
{
	public static RawLine ParseRawLine(string input)
	{
		var matches = Regex.Matches(input, @"^\s*(\d+)\s*(.*)$", RegexOptions.Singleline);
		var rawLines = from match in matches where match.Success
					 let lineNumber = LexCommon.ParseLabel(match.Groups[1].Value)
					 let content = match.Groups[2].Value.Trim()
					 select new RawLine(lineNumber ?? -1, content);
		return rawLines.Single();
	}

	public static IEnumerable<RawLine> ParseRawLines(string input)
	{
		var matches = Regex.Matches(input, @"^\s*(\d+)\s*(.*)$", RegexOptions.Multiline);
		foreach (Match match in matches)
		{
			if (match.Success)
			{
				var lineNumber = LexCommon.ParseLabel(match.Groups[1].Value);
				string content = match.Groups[2].Value.Trim();
				yield return new(lineNumber ?? -1, content);
			}
		}
	}
}