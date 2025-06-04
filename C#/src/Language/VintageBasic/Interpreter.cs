namespace vintage_basic.Language.VintageBasic;

sealed class JumpTableEntry
{
	public int Label { get; }
	public Action Program { get; }
	public List<string> Data { get; }

	public JumpTableEntry(int label, Action program, List<string> data)
	{
		Label = label;
		Program = program;
		Data = data;
	}
}

sealed class JumpTable
{
	readonly List<JumpTableEntry> entries = [];

	public void AddEntry(int label, Action program, List<string> data)
	{
		entries.Add(new JumpTableEntry(label, program, data));
	}

	public Action? GetProgram(int label)
	{
		return entries.FirstOrDefault(entry => entry.Label == label)?.Program;
	}

	public List<string>? GetData(int label)
	{
		return entries.FirstOrDefault(entry => entry.Label == label)?.Data;
	}
}

static class Interpreter
{
	public static void InterpLines(List<ParsedLine> progLines)
	{
		JumpTable jumpTable = new();

		foreach (var line in progLines)
		{
			int label = line.Label;
			Action codeSegment = () => InterpStatements(jumpTable, line.Statements);
			List<string> dataFromLine = ExtractData(line);

			jumpTable.AddEntry(label, codeSegment, dataFromLine);
		}

		if (jumpTable.GetProgram(progLines.First().Label) is Action firstProgram)
		{
			SeedRandomFromTime();
			SetDataStrings(jumpTable.GetData(progLines.First().Label));
			firstProgram();
		}
	}

	static void InterpStatements(JumpTable jumpTable, List<Tagged<Statement>> statements)
	{
		foreach (var taggedStatement in statements)
		{
			Console.WriteLine($"Executing statement at line {taggedStatement.Position}");
		}
	}

	static List<string> ExtractData(ParsedLine line) => new();

	static void SeedRandomFromTime()
	{
		Random rand = new();
		Console.WriteLine($"Random seed set: {rand.Next()}");
	}

	static void SetDataStrings(List<string>? data)
	{
		if (data is not null)
		{
			Console.WriteLine($"Setting DATA values: {string.Join(", ", data)}");
		}
	}
}

static class ExpressionEvaluator
{
	public static float BoolToVal(bool condition) => condition ? -1f : 0f;

	public static float LiftFVOp1(Func<float, float> operation, float value) => operation(value);

	public static float LiftFVOp2(Func<float, float, float> operation, float v1, float v2) => operation(v1, v2);

	public static string LiftSVOp2(Func<string, string, string> operation, string s1, string s2) => operation(s1, s2);

	public static float LiftFSCmpOp2(Func<float, float, bool> comparison, float v1, float v2)
	{
		return BoolToVal(comparison(v1, v2));
	}

	public static float Eval(Expression expr)
	{
		throw new NotImplementedException("Expression evaluation not implemented yet");
		//return expr switch
		//{
		//	FloatLiteral floatLit => floatLit.Value,
		//	BinaryOperator binOp => EvalBinaryOp(binOp.Operator, Eval(binOp.Left), Eval(binOp.Right)),
		//	_ => throw new ArgumentException("Unsupported expression type"),
		//};
	}

	private static float EvalBinaryOp(BinaryOperator op, float left, float right)
	{
		return op switch
		{
			BinaryOperator.Add => left + right,
			BinaryOperator.Subtract => left - right,
			BinaryOperator.Multiply => left * right,
			BinaryOperator.Divide => right == 0 ? throw new DivideByZeroException() : left / right,
			BinaryOperator.Power => MathF.Pow(left, right),
			_ => throw new ArgumentException("Unsupported binary operator")
		};
	}
}

static class BuiltinFunctions
{
	//public static float EvalBuiltin(Builtin builtin, List<float> args)
	//{
	//	return builtin switch
	//	{
	//		Builtin.AbsBI => MathF.Abs(args[0]),
	//		Builtin.AtnBI => MathF.Atan(args[0]),
	//		Builtin.CosBI => MathF.Cos(args[0]),
	//		Builtin.ExpBI => MathF.Exp(args[0]),
	//		Builtin.IntBI => MathF.Floor(args[0]),
	//		Builtin.LogBI => args[0] > 0 ? MathF.Log(args[0]) : throw new ArgumentException("Invalid argument"),
	//		Builtin.SgnBI => args[0] > 0 ? 1 : args[0] < 0 ? -1 : 0,
	//		Builtin.SinBI => MathF.Sin(args[0]),
	//		Builtin.SqrBI => args[0] >= 0 ? MathF.Sqrt(args[0]) : throw new ArgumentException("Invalid argument"),
	//		Builtin.TanBI => MathF.Tan(args[0]),
	//		_ => throw new ArgumentException("Unsupported built-in function")
	//	};
	//}
}
static class StatementInterpreter
{
	public static void InterpStatement(JumpTable jumpTable, Statement statement)
	{
		switch (statement)
		{
			case RemStatement _:
			case DataStatement _:
				return;

			case EndStatement _:
			case StopStatement _:
				Console.WriteLine("Program Ended");
				return;

			case PrintStatement printStmt:
				foreach (var expr in printStmt.Expressions)
				{
					Console.Write($"{ExpressionEvaluator.Eval(expr)} ");
				}
				Console.WriteLine();
				return;

			case InputStatement inputStmt:
				Console.Write(inputStmt.Prompt ?? "? ");
				inputStmt.Variables.ForEach(var => Console.ReadLine()); // Placeholder for user input
				return;

			case GotoStatement gotoStmt:
				Console.WriteLine($"Jumping to line {gotoStmt.Target}");
				return;

			case DimStatement dimStmt:
				Console.WriteLine($"Dimensioning array for {dimStmt.VarName}");
				return;

			default:
				throw new NotImplementedException("Unsupported statement type");
		}
	}

	public static void InterpNextVar(VarName varName)
	{
		throw new NotImplementedException("Next statement not implemented yet");
		//if (varName.Type == VarType.FloatType)
		//	throw new InvalidOperationException($"Next Exception {varName.Name}");
		//else
		//	throw new InvalidOperationException("Type Mismatch Error");
	}

	public static void InputVars(List<Var> vars)
	{
		Console.Write("? ");
		string input = Console.ReadLine() ?? "";
		var inputValues = input.Split(',');

		for (int i = 0; i < vars.Count; i++)
		{
			if (i >= inputValues.Length)
			{
				Console.Write("? ");
				inputValues.Append(Console.ReadLine() ?? "");
			}

			string val = inputValues[i];
			SetVar(vars[i], ParseInput(vars[i], val));
		}
	}

	private static Val ParseInput(Var var, string input)
	{
		throw new NotImplementedException();
		//return var.Type switch
		//{
		//	VarType.StringType => new StringVal(input),
		//	VarType.FloatType => float.TryParse(input, out var fVal) ? new FloatVal(fVal) : throw new Exception("Number Expected"),
		//	_ => throw new Exception("Unsupported VarType")
		//};
	}

	public static void SetVar(Var var, Val val)
	{
		//Console.WriteLine($"Setting variable {var.Name} to {val}");
	}
}