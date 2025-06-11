using System.Collections.Frozen;
using System.Globalization;
using VintageBasic.Runtime;
using VintageBasic.Runtime.Errors;
using VintageBasic.Syntax;

namespace VintageBasic.Interpreter;

sealed class Interpreter(RuntimeContext context)
{
	readonly RuntimeContext _context = context;
	readonly VariableManager _variableManager = context.Variables;
	readonly InputOutputManager _ioManager = context.IO;
	internal readonly FunctionManager _functionManager = context.Functions;
	internal readonly RandomManager _randomManager = context.Random;
	readonly StateManager _stateManager = context.ProgramState;

	List<JumpTableEntry> _jumpTable = [];
	internal bool _programEnded;
	internal int _currentProgramLineIndex = -1;
	internal bool _nextInstructionIsJump;

	internal RuntimeContext Context => _context;
	internal VariableManager VariableManager => _variableManager;
	internal InputOutputManager IoManager => _ioManager;
	internal StateManager StateManager => _stateManager;
	internal IReadOnlyList<JumpTableEntry> JumpTable => _jumpTable;

	List<JumpTableEntry> BuildJumpTable(IReadOnlyList<Line> programLines, out List<string> allDataStrings)
	{
		allDataStrings = [];
		List<JumpTableEntry> jumpTableBuilder = new(programLines.Count);
		foreach (var currentLine in programLines.OrderBy(l => l.Label))
		{
			var lineData = CollectDataFromLine(currentLine); // Uses refined ParseDataLineContent
			allDataStrings.AddRange(lineData);
			void programAction()
			{
				foreach (var taggedStatement in currentLine.Statements)
				{
					_stateManager.SetCurrentLineNumber(currentLine.Label);
					InterpretStatement(taggedStatement);
					if (_programEnded || _nextInstructionIsJump) break;
				}
			}
			jumpTableBuilder.Add(new(currentLine.Label, programAction, lineData));
		}
		return jumpTableBuilder;
	}
	void ProgramAction(JumpTableEntry entry)
	{
		try
		{
			entry.ProgramAction();
		}
		catch (BasicRuntimeException)
		{
			_programEnded = true;
			throw;
		}
		catch (Exception ex)
		{
			_programEnded = true;
			throw new BasicRuntimeException($"Unexpected error: {ex.Message}", _stateManager.CurrentLineNumber, ex);
		}
	}

	public void ExecuteProgram(IReadOnlyList<Line> programLines)
	{
		if (programLines.Count <= 0) return;

		_jumpTable = BuildJumpTable(programLines, out var allDataStrings);
		if (_jumpTable.Count <= 0) return;

		_ioManager.SetDataStrings(allDataStrings);
		_randomManager.SeedRandomFromTime();
		_currentProgramLineIndex = 0;
		_stateManager.SetCurrentLineNumber(_jumpTable[_currentProgramLineIndex].Label);
		while (!_programEnded && (_currentProgramLineIndex < _jumpTable.Count) && (_currentProgramLineIndex >= 0))
		{
			_nextInstructionIsJump = false;
			var entry = _jumpTable[_currentProgramLineIndex];
			_stateManager.SetCurrentLineNumber(entry.Label);

			ProgramAction(entry);
			if (_programEnded) return;

			if (_nextInstructionIsJump)
			{
				int targetLabel = _stateManager.CurrentLineNumber;
				_currentProgramLineIndex = _jumpTable.FindIndex(jte => jte.Label == targetLabel);
				if (_currentProgramLineIndex == -1)
					throw new BadGotoTargetError(targetLabel, lineNumber: entry.Label);
			}
			else
			{
				_currentProgramLineIndex++;
			}
		}
	}

	static List<string> CollectDataFromLine(Line line)
	{
		List<string> lineData = [];
		foreach (var taggedStatement in line.Statements)
		{
			if (taggedStatement.Value is DataStatement dataStmt)
			{
				lineData.AddRange(RuntimeParsingUtils.ParseDataLineContent(dataStmt.Data));
			}
		}
		return lineData;
	}

	internal void InterpretStatement(Tagged<Statement> taggedStatement)
	{
		_stateManager.SetCurrentLineNumber(taggedStatement.Position.Line > 0 ? taggedStatement.Position.Line : _stateManager.CurrentLineNumber);
		taggedStatement.Value.Execute(this);
	}

	internal List<int> EvaluateIndices(IReadOnlyList<Expression> dimExprs, int currentBasicLine)
	{
		List<int> indices = [];
		foreach (var dimExpr in dimExprs)
		{
			var dimVal = EvaluateExpression(dimExpr, currentBasicLine);
			indices.Add(dimVal.AsInt(currentBasicLine));
		}
		return indices;
	}

	internal object EvaluateExpression(Expression expr, int currentBasicLine)
	{
		_stateManager.SetCurrentLineNumber(currentBasicLine);
		return expr.Evaluate(this, currentBasicLine);
	}

	internal object EvaluateBinOp(BinOp op, object v1, object v2, int currentBasicLine)
	{
		_stateManager.SetCurrentLineNumber(currentBasicLine);
		var cV1 = (op == BinOp.AddOp && v1 is string) ? v1 : ValExtensions.CoerceToExpressionType(v1, currentBasicLine, _stateManager);
		var cV2 = (op == BinOp.AddOp && v2 is string) ? v2 : ValExtensions.CoerceToExpressionType(v2, currentBasicLine, _stateManager);

		object Add()
		{
			if (cV1 is string s1 && cV2 is string s2) return s1 + s2;
			if (cV1 is float f1 && cV2 is float f2) return f1 + f2;
			throw new TypeMismatchError($"Cannot ADD types {cV1.GetTypeName()} and {cV2.GetTypeName()}", currentBasicLine);
		}
		float Div()
		{
			var divisor = cV2.AsFloat(currentBasicLine);
			if (divisor == 0.0f)
				throw new DivisionByZeroError(lineNumber: currentBasicLine);
			return cV1.AsFloat(currentBasicLine) / divisor;
		}
		float Comparison()
		{
			if (v1.GetType() != v2.GetType())
				throw new TypeMismatchError($"Cannot compare types {v1.GetTypeName()} and {v2.GetTypeName()}", currentBasicLine);
			var cr = Comparer<object>.Default.Compare(v1, v2);
			var res = op switch { BinOp.EqOp => cr == 0, BinOp.NEOp => cr != 0, BinOp.LTOp => cr < 0, BinOp.LEOp => cr <= 0, BinOp.GTOp => cr > 0, BinOp.GEOp => cr >= 0, _ => false };
			return res ? -1.0f : 0.0f;
		}
		return op switch
		{
			BinOp.AddOp => Add(),
			BinOp.SubOp => cV1.AsFloat(currentBasicLine) - cV2.AsFloat(currentBasicLine),
			BinOp.MulOp => cV1.AsFloat(currentBasicLine) * cV2.AsFloat(currentBasicLine),
			BinOp.DivOp => Div(),
			BinOp.PowOp => (float)Math.Pow(cV1.AsFloat(currentBasicLine), cV2.AsFloat(currentBasicLine)),
			BinOp.EqOp or BinOp.NEOp or BinOp.LTOp or BinOp.LEOp or BinOp.GTOp or BinOp.GEOp => Comparison(),
			BinOp.AndOp => (cV1.AsFloat(currentBasicLine) != 0.0f && cV2.AsFloat(currentBasicLine) != 0.0f) ? -1.0f : 0.0f,
			BinOp.OrOp => (cV1.AsFloat(currentBasicLine) != 0.0f || cV2.AsFloat(currentBasicLine) != 0.0f) ? -1.0f : 0.0f,
			_ => throw new NotImplementedException($"Binary operator {op}. Line: {currentBasicLine}"),
		};
	}

	void CheckArgTypes(Builtin builtinName, List<Type> expectedTypes, List<object> actualArgs, int currentBasicLine)
	{
		_stateManager.SetCurrentLineNumber(currentBasicLine);
		if (expectedTypes.Count != actualArgs.Count)
		{
			if (!(builtinName == Builtin.Rnd && expectedTypes.Count == 1 && actualArgs.Count == 0)) // RND can be called with 0 or 1 arg
				throw new WrongNumberOfArgumentsError($"For {builtinName}: expected {expectedTypes.Count} arguments, got {actualArgs.Count}", currentBasicLine);
		}

		for (int i = 0; i < Math.Min(expectedTypes.Count, actualArgs.Count); i++)
		{
			var actualType = actualArgs[i].GetType();
			if ((expectedTypes[i] == typeof(int)) && (actualType == typeof(float))) continue;
			if (expectedTypes[i] != actualType)
				throw new TypeMismatchError($"For {builtinName} argument {i + 1}: expected {expectedTypes[i].Name}, got {actualType.Name}", currentBasicLine);
		}
	}

	static readonly FrozenDictionary<Builtin, List<Type>> BuiltinArgTypes = new Dictionary<Builtin, List<Type>>() {
		{ Builtin.Abs, [ typeof(float) ] }, { Builtin.Asc, [ typeof(string) ] }, { Builtin.Atn, [ typeof(float) ] },
		{ Builtin.Cos, [ typeof(float) ] },
		{ Builtin.Exp, [ typeof(float) ] },
		{ Builtin.Left, [ typeof(string), typeof(float) ] }, { Builtin.Len, [ typeof(string) ] }, { Builtin.Log, [ typeof(float) ] },
		{ Builtin.Right, [ typeof(string), typeof(float) ] },
		{ Builtin.Sin, [ typeof(float) ] }, { Builtin.Sqr, [ typeof(float) ] },
		{ Builtin.Tan, [ typeof(float) ] },
		{ Builtin.Val, [ typeof(string)] },
	}.ToFrozenDictionary();

	List<object> EvaluateArgs(IReadOnlyList<Expression> argExprs, int currentBasicLine)
	{
		List<object> args = [];
		foreach (var argExpr in argExprs)
			args.Add(EvaluateExpression(argExpr, currentBasicLine));
		return args;
	}

	internal object EvaluateBuiltin(Builtin builtin, IReadOnlyList<Expression> argExprs, int currentBasicLine)
	{
		_stateManager.SetCurrentLineNumber(currentBasicLine);

		var args = EvaluateArgs(argExprs, currentBasicLine);
		if (BuiltinArgTypes.TryGetValue(builtin, out var expectedTypes))
		{
			CheckArgTypes(builtin, expectedTypes, args, currentBasicLine);
		}
		return builtin switch
		{
			Builtin.Abs => Math.Abs(args[0].AsFloat(currentBasicLine)),
			Builtin.Asc => BuiltinAsc(args, currentBasicLine),
			Builtin.Atn => (float)Math.Atan(args[0].AsFloat(currentBasicLine)),
			Builtin.Chr => BuiltinChr(args, currentBasicLine),
			Builtin.Cos => (float)Math.Cos(args[0].AsFloat(currentBasicLine)),
			Builtin.Exp => (float)Math.Exp(args[0].AsFloat(currentBasicLine)),
			Builtin.Int => BuiltinInt(args, currentBasicLine),
			Builtin.Left => BuiltinLeft(args, currentBasicLine),
			Builtin.Len => (float)((string)args[0]).Length,
			Builtin.Log => BuiltinLog(args, currentBasicLine),
			Builtin.Mid => BuildinMid(args, currentBasicLine),
			Builtin.Right => BuiltinRight(args, currentBasicLine),
			Builtin.Rnd => BuiltinRnd(args, currentBasicLine),
			Builtin.Sgn => BuiltinSgn(args, currentBasicLine),
			Builtin.Sin => (float)Math.Sin(args[0].AsFloat(currentBasicLine)),
			Builtin.Spc => BuiltinSpc(args, currentBasicLine),
			Builtin.Sqr => BultinSqr(args, currentBasicLine),
			Builtin.Str => BultinStr(args, currentBasicLine),
			Builtin.Tab => BultinTab(args, currentBasicLine),
			Builtin.Tan => (float)Math.Tan(args[0].AsFloat(currentBasicLine)),
			Builtin.Val => BultinVal(args),
			_ => throw new NotImplementedException($"Builtin function {builtin}. Line: {currentBasicLine}"),
		};
	}

	static void ThrowIfNotNumericArg0(List<object> args, string message, int currentBasicLine)
	{
		bool hasNumericArg0 = (args.Count == 1) && args[0].IsNumeric();
		if (!hasNumericArg0)
			throw new TypeMismatchError(message, currentBasicLine);
	}

	static float BuiltinAsc(List<object> args, int currentBasicLine)
	{
		var ascStr = (string)args[0];
		if (string.IsNullOrEmpty(ascStr))
			throw new InvalidArgumentError("ASC argument is empty", currentBasicLine);
		return ascStr[0];
	}

	static string BuiltinChr(List<object> args, int currentBasicLine)
	{
		ThrowIfNotNumericArg0(args, "CHR$ expects 1 numeric arg", currentBasicLine);
		var chrCode = args[0].AsInt(currentBasicLine);
		if (chrCode < 0 || chrCode > 255)
			throw new InvalidArgumentError($"CHR$ code {chrCode} out of range (0-255)", currentBasicLine);
		return ((char)chrCode).ToString();
	}

	static float BuiltinInt(List<object> args, int currentBasicLine)
	{
		ThrowIfNotNumericArg0(args, "INT expects 1 numeric arg", currentBasicLine);
		return (float)Math.Floor(args[0].AsFloat(currentBasicLine));
	}

	static string BuiltinLeft(List<object> args, int currentBasicLine)
	{
		var leftStr = (string)args[0];
		var leftN = args[1].AsInt(currentBasicLine);
		if (leftN < 0)
			leftN = 0;
		return leftStr[..Math.Min(leftN, leftStr.Length)];
	}

	static float BuiltinLog(List<object> args, int currentBasicLine)
	{
		var logArg = args[0].AsFloat(currentBasicLine);
		if (logArg <= 0)
			throw new InvalidArgumentError("LOG argument must be > 0", currentBasicLine);
		return (float)Math.Log(logArg);
	}

	string BuildinMid(List<object> args, int currentBasicLine)
	{
		if (args.Count < 2 || args.Count > 3)
			throw new WrongNumberOfArgumentsError("MID$ expects 2 or 3 args", currentBasicLine);
		CheckArgTypes(Builtin.Mid, [typeof(string), typeof(float)], [.. args.Take(2)], currentBasicLine);
		if (args.Count == 3 && !args[2].IsNumeric())
			throw new TypeMismatchError("MID$ length arg must be numeric", currentBasicLine);
		string midStr = (string)args[0];
		int midStart = args[1].AsInt(currentBasicLine);
		if (midStart < 1) midStart = 1;
		int midLen = (args.Count == 3) ? args[2].AsInt(currentBasicLine) : midStr.Length - (midStart - 1);
		if (midLen < 0) midLen = 0;
		if (midStart > midStr.Length || midLen == 0)
			return String.Empty;
		midStart--; // Adjust to 0-based index for Substring
		if (midStart + midLen > midStr.Length) midLen = midStr.Length - midStart;
		return midStr.Substring(midStart, midLen);
	}

	static string BuiltinRight(List<object> args, int currentBasicLine)
	{
		var rightStr = (string)args[0];
		var rightN = args[1].AsInt(currentBasicLine);
		if (rightN < 0)
			rightN = 0;
		return rightStr[Math.Max(0, rightStr.Length - rightN)..];
	}

	float BuiltinRnd(List<object> args, int currentBasicLine)
	{
		var rndArg = (args.Count > 0) ? args[0].AsFloat(currentBasicLine) : 1.0f;
		if (rndArg < 0)
			_randomManager.SeedRandom((int)rndArg);
		var rndVal = (rndArg == 0) ? _randomManager.PreviousRandomValue : _randomManager.GetRandomValue();
		return (float)rndVal;
	}

	static float BuiltinSgn(List<object> args, int currentBasicLine)
	{
		ThrowIfNotNumericArg0(args, "SGN expects 1 numeric arg", currentBasicLine);
		return Math.Sign(args[0].AsFloat(currentBasicLine));
	}

	static string BuiltinSpc(List<object> args, int currentBasicLine)
	{
		ThrowIfNotNumericArg0(args, "SPC expects 1 numeric arg", currentBasicLine);
		var spcCount = args[0].AsInt(currentBasicLine);
		if (spcCount < 0)
			spcCount = 0;
		return new(' ', Math.Min(spcCount, 255));
	}

	static float BultinSqr(List<object> args, int currentBasicLine)
	{
		float sqrArg = args[0].AsFloat(currentBasicLine);
		if (sqrArg < 0)
			throw new InvalidArgumentError("SQR argument < 0", currentBasicLine);
		return (float)Math.Sqrt(sqrArg);
	}

	static string BultinStr(List<object> args, int currentBasicLine)
	{
		ThrowIfNotNumericArg0(args, "STR$ expects 1 numeric arg", currentBasicLine);
		var strNum = args[0].AsFloat(currentBasicLine);
		var strRep = strNum.ToString(CultureInfo.InvariantCulture);
		if (strNum >= 0 && (strRep.Length == 0 || strRep[0] != '-'))
			strRep = " " + strRep;
		return strRep;
	}

	string BultinTab(List<object> args, int currentBasicLine)
	{
		ThrowIfNotNumericArg0(args, "TAB expects 1 numeric arg", currentBasicLine);
		int tabCol = args[0].AsInt(currentBasicLine);
		if (tabCol < 1 || tabCol > 255)
			throw new InvalidArgumentError($"TAB col {tabCol} out of range (1-255)", currentBasicLine);
		int curCol = _ioManager.OutputColumn + 1;
		return tabCol > curCol ? new System.String(' ', tabCol - curCol) : "";
	}

	static float BultinVal(List<object> args)
	{
		string valStr = ((string)args[0]).Trim();
		string numPart = "";
		bool digit = false;
		foreach (char c in valStr)
		{
			if (Char.IsDigit(c))
			{
				numPart += c;
				digit = true;
			}
			else if (c == '.' && !numPart.Contains('.'))
			{
				numPart += c;
			}
			else if ((c == 'E' || c == 'e') && !numPart.ToUpper().Contains('E') && digit)
			{
				numPart += c;
			}
			else if ((c == '+' || c == '-') && ((numPart.Length == 0) || (numPart is [.., 'E'])))
			{
				numPart += c;
			}
			else if (Char.IsWhiteSpace(c) && (numPart.Length == 0))
				continue;
			else
				break;
		}
		return RuntimeParsingUtils.TryParseFloat(numPart, out var v) ? v : default;
	}
}
