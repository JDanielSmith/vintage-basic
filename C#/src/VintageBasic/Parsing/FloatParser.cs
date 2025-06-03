// src/VintageBasic/Parsing/FloatParser.cs
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace VintageBasic.Parsing;

public static class FloatParser
{
    // Main method to attempt parsing a string into a double.
    public static bool TryParseFloat(string s, out double result)
    {
        result = 0.0;
        if (String.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        // Using a regex-based approach for flexibility, similar to Parsec's declarative style.
        // This regex aims to capture the structure defined by the Haskell parser.
        // It handles:
        // 1. Optional sign.
        // 2. Integer part, fractional part, exponent part in various combinations.
        //    - num.fract E exp
        //    - .fract E exp
        //    - num E exp
        //    - num.fract
        //    - .fract
        //    - num
        // It allows 'E' or 'D' for exponents, and optional '+' for positive exponents.

        // Regex breakdown:
        // ^\s*                     : Optional leading whitespace
        // (?<sign>[+-]?)           : Optional sign (+ or -)
        // (                         : Start of number part options
        //   (?<integer>\d+)?       : Optional integer part
        //   (?:\.(?<fraction>\d+))? : Optional fractional part (must have digits if decimal point exists)
        //   |                       : OR (for cases like ".123")
        //   \.(?<fraction_only>\d+) : Fractional part starting with decimal point
        // )
        // (?:[eEdD](?<expsign>[+-]?)(?<exponent>\d+))? : Optional exponent part
        // \s*$                     : Optional trailing whitespace

        // Refined regex to better match the Haskell parser's logic (which is more procedural via combinators)
        // The Haskell parser tries specific combinations:
        //  1. num.fract E exp
        //  2. .fract E exp
        //  3. num E exp
        //  4. num.fract
        //  5. .fract
        //  6. num (implied by natFloat alone, though not explicitly listed as a top-level success in floatLex without exp or fract)

        // Let's try a simpler approach by cleaning the input for C# double.Parse,
        // then manually parsing if that fails for 'D' exponent.
        // C# double.Parse/TryParse with NumberStyles.Float and CultureInfo.InvariantCulture
        // handles most cases like "1.23", "1.23e+10", "1.23E-5", "-.5", "+5".
        // The main things not handled are 'D' exponent and missing '+' in exponent.

        string normalizedString = s.Trim();
        if (String.IsNullOrEmpty(normalizedString)) return false;

        // Replace 'D' or 'd' with 'E' for standard parsing.
        normalizedString = normalizedString.Replace('D', 'E').Replace('d', 'E');

        // Handle missing '+' in exponent: "E10" -> "E+10"
        // Regex to find 'E' followed by a digit (without an intermediate sign)
        normalizedString = Regex.Replace(normalizedString, @"E(\d)", "E+$1");


        if (double.TryParse(normalizedString, NumberStyles.Float | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out result))
        {
            return true;
        }
        
        // If standard parsing fails, it might be due to subtleties not caught or complex invalid format.
        // The Haskell parser is quite specific. Let's try to mimic its structure more directly
        // if the regex + standard parse isn't robust enough.
        // For now, the above normalization and TryParse should cover many common BASIC float formats.

        // Fallback for extremely simple cases if the above fails (e.g. "1", "-2")
        // This part is more of a safeguard, as TryParse should handle these.
        if (long.TryParse(normalizedString, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longVal))
        {
            result = longVal;
            return true;
        }

        return false;
    }
}
