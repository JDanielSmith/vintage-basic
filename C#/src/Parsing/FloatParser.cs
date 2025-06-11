using System.Globalization;
using System.Text.RegularExpressions;

namespace VintageBasic.Parsing;

static partial class FloatParser
{
	/// <summary>
	/// Main method to attempt parsing a string into a double.
	/// </summary>
	public static bool TryParseFloat(string s, out double result)
	{
		result = 0.0;
		if (String.IsNullOrWhiteSpace(s))
		{
			return false;
		}

		// Replace 'D' or 'd' with 'E' for standard parsing.
		var normalizedString = s.Trim().Replace('D', 'E').Replace('d', 'E');

		// Handle missing '+' in exponent: "E10" -> "E+10"
		// Regex to find 'E' followed by a digit (without an intermediate sign)
		normalizedString = E_followed_by_digit().Replace(normalizedString, "E+$1");

		var style = NumberStyles.Float | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;
		if (Double.TryParse(normalizedString, style, CultureInfo.InvariantCulture, out result))
		{
			return true;
		}

		// Fallback for extremely simple cases if the above fails (e.g. "1", "-2")
		// This part is more of a safeguard, as TryParse should handle these.
		if (Int64.TryParse(normalizedString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longVal))
		{
			result = longVal;
			return true;
		}

		return false;
	}

	[GeneratedRegex(@"E(\d)")]
	private static partial Regex E_followed_by_digit();
}
