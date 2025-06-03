using System.Text.RegularExpressions;

namespace vintage_basic.Language.VintageBasic;

public class FloatParser
{
	public static bool IsDigit(char c) => char.IsDigit(c);
	public static bool IsDot(char c) => c == '.';
	public static bool IsPlus(char c) => c == '+';
	public static bool IsMinus(char c) => c == '-';
	public static bool IsExponentChar(char c) => c == 'E' || c == 'e';

	public static float? ParseFloat(string input)
	{
		var match = Regex.Match(input, @"^[+-]?(\d+(\.\d*)?|\.\d+)([eE][+-]?\d+)?$");
		return match.Success ? Single.Parse(match.Value) : (float?)null;
	}

	public static string ParseSign(ref string input)
	{
		if (input.Length > 0 && (IsPlus(input[0]) || IsMinus(input[0])))
		{
			string sign = input[0] == '+' ? "" : "-";
			input = input.Substring(1);
			return sign;
		}
		return "";
	}

	public static string ParseExponent(ref string input)
	{
		if (input.Length > 0 && IsExponentChar(input[0]))
		{
			input = input.Substring(1);
			string exponentSign = ParseSign(ref input);
			string digits = new Regex(@"^\d+").Match(input).Value;
			input = input.Substring(digits.Length);
			return $"E{exponentSign}{digits}";
		}
		return "";
	}
}
