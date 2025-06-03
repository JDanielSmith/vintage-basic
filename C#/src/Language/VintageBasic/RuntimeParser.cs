using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace vintage_basic.Language.VintageBasic;

static class RuntimeParser
{
	// Attempts to parse a floating point value from a string.
	public static float? ReadFloat(string input)
	{
		return float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
			? result
			: (float?)null;
	}

	// Trims leading and trailing spaces from a string.
	public static string Trim(string input)
	{
		return input.Trim();
	}

	// Parses a single data value.
	public static string ParseDataValue(string input)
	{
		input = Trim(input);
		if (input.StartsWith('"') && input.EndsWith('"') && input.Length > 1)
		{
			return input.Substring(1, input.Length - 2);
		}
		return input;
	}

	// Parses a list of data values (works for both INPUT and DATA statements).
	public static List<string> ParseDataValues(string input)
	{
		var values = new List<string>();
		var parts = Regex.Split(input, @"\s*,\s*");

		foreach (var part in parts)
		{
			values.Add(ParseDataValue(part));
		}

		return values;
	}
}