using System.Globalization;
using System.Text.RegularExpressions;

namespace VintageBasic.Parsing;

static class FloatParser
{
    // Main method to attempt parsing a string into a double.
    public static bool TryParseFloat(string s, out double result)
    {
        result = 0.0;
        if (String.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        string normalizedString = s.Trim();
        if (String.IsNullOrEmpty(normalizedString)) return false;

        // Replace 'D' or 'd' with 'E' for standard parsing.
        normalizedString = normalizedString.Replace('D', 'E').Replace('d', 'E');

        // Handle missing '+' in exponent: "E10" -> "E+10"
        // Regex to find 'E' followed by a digit (without an intermediate sign)
        normalizedString = Regex.Replace(normalizedString, @"E(\d)", "E+$1");


        var style = NumberStyles.Float | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;
		if (Double.TryParse(normalizedString, style, CultureInfo.InvariantCulture, out result))
        {
            return true;
        }
        
        // If standard parsing fails, it might be due to subtleties not caught or complex invalid format.
        // The Haskell parser is quite specific. Let's try to mimic its structure more directly
        // if the regex + standard parse isn't robust enough.
        // For now, the above normalization and TryParse should cover many common BASIC float formats.

        // Fallback for extremely simple cases if the above fails (e.g. "1", "-2")
        // This part is more of a safeguard, as TryParse should handle these.
        if (Int64.TryParse(normalizedString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longVal))
        {
            result = longVal;
            return true;
        }

        return false;
    }
}
