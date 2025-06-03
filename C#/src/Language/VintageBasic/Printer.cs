using System.Globalization;

namespace vintage_basic.Language.VintageBasic;
static class BasicPrinter
{
	public static string PrintLiteral(object value)
	{
		return value switch
		{
			float v => v == Math.Floor(v) ? ((int)v).ToString() : v.ToString(CultureInfo.InvariantCulture),
			string s => $"\"{s}\"",
			_ => throw new ArgumentException("Unsupported literal type")
		};
	}

	public static string PrintFloat(float x)
	{
		if (x == 0) return " 0";
		return x < 0 ? $"-{PrintPosFloat(-x)}" : $" {PrintPosFloat(x)}";
	}

	//static readonly int MaxFloatDigits = (int)Math.Ceiling(Math.Log(2) / Math.Log(10) * 24);

	private static string PrintPosFloat(float x)
	{
		string formatted = x.ToString("G", CultureInfo.InvariantCulture);
		return formatted.Contains('E') ? formatted : formatted.TrimEnd('0').TrimEnd('.');
	}

	public static string PrintVarName(string name, string type)
	{
		return type switch
		{
			"Float" => name,
			"Int" => name + "%",
			"String" => name + "$",
			_ => name
		};
	}

	public static string PrintOperator(string op)
	{
		return op switch
		{
			"Add" => "+",
			"Sub" => "-",
			"Mul" => "*",
			"Div" => "/",
			"Pow" => "^",
			"Eq" => "=",
			"NE" => "<>",
			"LT" => "<",
			"LE" => "<=",
			"GT" => ">",
			"GE" => ">=",
			"And" => " AND ",
			"Or" => " OR ",
			_ => op
		};
	}

	public static string PrintExpression(string expr, List<string> args)
	{
		return args.Count == 0 ? expr : $"{expr}({string.Join(",", args)})";
	}

	public static string PrintStatement(string statementType, List<string> parts)
	{
		return $"{statementType} {string.Join(" ", parts)}";
	}

	public static string PrintLine(int lineNumber, List<string> statements)
	{
		return $"{lineNumber} {string.Join(":", statements)}\n";
	}

	public static string PrintLines(List<(int, List<string>)> programLines)
	{
		return string.Join("", programLines.Select(line => PrintLine(line.Item1, line.Item2)));
	}
}