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

	public void ExecuteProgram(IReadOnlyList<Line> programLines)
	{
		if (programLines.Count <= 0)
		{
			return;
		}

		var sortedLines = programLines.OrderBy(l => l.Label).ToList();
		List<string> allDataStrings = [];
		List<JumpTableEntry> jumpTableBuilder = new(sortedLines.Count);

		foreach (var line in sortedLines)
		{
			var lineData = CollectDataFromLine(line); // Uses refined ParseDataLineContent
			allDataStrings.AddRange(lineData);

			var currentLine = line;
			void programAction()
			{
				foreach (var taggedStatement in currentLine.Statements)
				{
					_stateManager.SetCurrentLineNumber(currentLine.Label);
					InterpretStatement(taggedStatement);
					if (_programEnded || _nextInstructionIsJump) break;
				}
			}
			jumpTableBuilder.Add(new JumpTableEntry(currentLine.Label, programAction, lineData));
		}
		_jumpTable = jumpTableBuilder;
		_ioManager.SetDataStrings(allDataStrings);
		_randomManager.SeedRandomFromTime();

		if (_jumpTable.Count <= 0)
		{
			return;
		}

		_programEnded = false;
		_currentProgramLineIndex = 0;
		if (_jumpTable.Count > 0)
			_stateManager.SetCurrentLineNumber(_jumpTable[_currentProgramLineIndex].Label);

		while (!_programEnded && (_currentProgramLineIndex < _jumpTable.Count) && (_currentProgramLineIndex >= 0))
		{
			_nextInstructionIsJump = false;
			var entry = _jumpTable[_currentProgramLineIndex];
			_stateManager.SetCurrentLineNumber(entry.Label);

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
				throw new BasicRuntimeException($"Unexpected error: {ex.Message}", ex, _stateManager.CurrentLineNumber);
			}

			if (_programEnded) break;

			if (_nextInstructionIsJump)
			{
				int targetLabel = _stateManager.CurrentLineNumber;
				_currentProgramLineIndex = _jumpTable.ToList().FindIndex(jte => jte.Label == targetLabel);
				if (_currentProgramLineIndex == -1)
				{
					throw new BadGotoTargetError(targetLabel, lineNumber: entry.Label);
				}
			}
			else
			{
				_currentProgramLineIndex++;
			}
		}
	}

	static List<string> CollectDataFromLine(Line line)
	{
		var lineData = new List<string>();
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
			Object dimVal = EvaluateExpression(dimExpr, currentBasicLine);
			indices.Add(dimVal.AsInt(currentBasicLine));
		}
		return indices;
	}

	internal Object EvaluateExpression(Expression expr, int currentBasicLine)
	{
		_stateManager.SetCurrentLineNumber(currentBasicLine);
		switch (expr)
		{
			case LiteralExpression l: return l.Value switch { FloatLiteral f => f.Value, StringLiteral s => s.Value, _ => throw new NotSupportedException() };
			case VarExpression v:
				Object val = v.Value switch { ScalarVar sv => _variableManager.GetScalarVar(sv.VarName), ArrVar av => _variableManager.GetArrayVar(av.VarName, EvaluateIndices(av.Dimensions, currentBasicLine)), _ => throw new NotSupportedException() };
				return ValExtensions.CoerceToExpressionType(val, currentBasicLine, _stateManager);
			case ParenExpression p: return EvaluateExpression(p.Inner, currentBasicLine);
			case MinusExpression m:
				Object op = EvaluateExpression(m.Right, currentBasicLine);
				Object numOpM = ValExtensions.CoerceToExpressionType(op, currentBasicLine, _stateManager);
				if (numOpM is float fv)
					return -fv;
				throw new TypeMismatchError("Numeric operand for unary minus.", currentBasicLine);
			case NotExpression n:
				Object notOp = EvaluateExpression(n.Right, currentBasicLine);
				Object numOpN = ValExtensions.CoerceToExpressionType(notOp, currentBasicLine, _stateManager);
				if (numOpN is float fvN) return fvN == 0.0f ? -1.0f : 0.0f;
				throw new TypeMismatchError("Numeric operand for NOT.", currentBasicLine);
			case BinOpExpression b: return EvaluateBinOp(b.Op, EvaluateExpression(b.Left, currentBasicLine), EvaluateExpression(b.Right, currentBasicLine), currentBasicLine);
			case BuiltinExpression bi: return EvaluateBuiltin(bi.Builtin, bi.Args, currentBasicLine);
			case FnExpression fn:
				UserDefinedFunction udf = _functionManager.GetFunction(fn.FunctionName);
				List<object> fnArgs = [];
				foreach (var argExpr in fn.Args) fnArgs.Add(EvaluateExpression(argExpr, currentBasicLine));
				return udf(fnArgs);
			case NextZoneExpression _: return "<Special:NextZone>";
			case EmptyZoneExpression _: return "<Special:EmptySeparator>";
			default: throw new NotImplementedException($"Expression type {expr.GetType().Name} not implemented. Line: {currentBasicLine}");
		}
	}

	internal Object EvaluateBinOp(BinOp op, Object v1, Object v2, int currentBasicLine)
	{
		_stateManager.SetCurrentLineNumber(currentBasicLine);
		var cV1 = (op == BinOp.AddOp && v1 is string) ? v1 : ValExtensions.CoerceToExpressionType(v1, currentBasicLine, _stateManager);
		var cV2 = (op == BinOp.AddOp && v2 is string) ? v2 : ValExtensions.CoerceToExpressionType(v2, currentBasicLine, _stateManager);
		switch (op)
		{
			case BinOp.AddOp:
				if (cV1 is string s1 && cV2 is string s2) return s1 + s2;
				if (cV1 is float f1 && cV2 is float f2) return f1 + f2;
				throw new TypeMismatchError($"Cannot ADD types {cV1.GetTypeName()} and {cV2.GetTypeName()}", currentBasicLine);
			case BinOp.SubOp: return cV1.AsFloat(currentBasicLine) - cV2.AsFloat(currentBasicLine);
			case BinOp.MulOp: return cV1.AsFloat(currentBasicLine) * cV2.AsFloat(currentBasicLine);
			case BinOp.DivOp:
				float divisor = cV2.AsFloat(currentBasicLine);
				if (divisor == 0.0f) throw new DivisionByZeroError(lineNumber: currentBasicLine);
				return cV1.AsFloat(currentBasicLine) / divisor;
			case BinOp.PowOp: return (float)Math.Pow(cV1.AsFloat(currentBasicLine), cV2.AsFloat(currentBasicLine));
			case BinOp.EqOp:
			case BinOp.NEOp:
			case BinOp.LTOp:
			case BinOp.LEOp:
			case BinOp.GTOp:
			case BinOp.GEOp:
				if (!v1.EqualsType(v2))
					throw new TypeMismatchError($"Cannot compare types {v1.GetTypeName()} and {v2.GetTypeName()}", currentBasicLine);
				int cr = Comparer<object>.Default.Compare(v1, v2);
				bool res = op switch { BinOp.EqOp => cr == 0, BinOp.NEOp => cr != 0, BinOp.LTOp => cr < 0, BinOp.LEOp => cr <= 0, BinOp.GTOp => cr > 0, BinOp.GEOp => cr >= 0, _ => false };
				return res ? -1.0f : 0.0f;
			case BinOp.AndOp: return (cV1.AsFloat(currentBasicLine) != 0.0f && cV2.AsFloat(currentBasicLine) != 0.0f) ? -1.0f : 0.0f;
			case BinOp.OrOp: return (cV1.AsFloat(currentBasicLine) != 0.0f || cV2.AsFloat(currentBasicLine) != 0.0f) ? -1.0f : 0.0f;
			default: throw new NotImplementedException($"Binary operator {op}. Line: {currentBasicLine}");
		}
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
			if (expectedTypes[i].IsSameType<int>() && actualArgs[i].IsSameType<float>()) continue;
			if (!expectedTypes[i].EqualsType(actualArgs[i]))
				throw new TypeMismatchError($"For {builtinName} argument {i + 1}: expected {expectedTypes[i]}, got {actualArgs[i].GetTypeName()}", currentBasicLine);
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

	static bool HasNumericArg0(IReadOnlyList<object> args)
	{
		return (args.Count == 1) && args[0].IsNumeric();
	}

	List<object> EvaluateArgs(IReadOnlyList<Expression> argExprs, int currentBasicLine)
	{
		List<object> args = [];
		foreach (var argExpr in argExprs)
			args.Add(EvaluateExpression(argExpr, currentBasicLine));
		return args;
	}

	Object EvaluateBuiltin(Builtin builtin, IReadOnlyList<Expression> argExprs, int currentBasicLine)
	{
		_stateManager.SetCurrentLineNumber(currentBasicLine);

		var args = EvaluateArgs(argExprs, currentBasicLine);
		if (BuiltinArgTypes.TryGetValue(builtin, out var expectedTypes))
		{
			CheckArgTypes(builtin, expectedTypes, args, currentBasicLine);
		}
		return builtin switch
		{
			Builtin.Abs => BultinAbs(args, currentBasicLine),
			Builtin.Asc => BuiltinAsc(args, currentBasicLine),
			Builtin.Atn => BultinAtn(args, currentBasicLine),
			Builtin.Chr => BuiltinChr(args, currentBasicLine),
			Builtin.Cos => BuiltinCos(args, currentBasicLine),
			Builtin.Exp => BultinExp(args, currentBasicLine),
			Builtin.Int => BuiltinInt(args, currentBasicLine),
			Builtin.Left => BuiltinLeft(args, currentBasicLine),
			Builtin.Len => BultinLen(args, currentBasicLine),
			Builtin.Log => BuiltinLog(args, currentBasicLine),
			Builtin.Mid => BuildinMid(args, currentBasicLine),
			Builtin.Right => BuiltinRight(args, currentBasicLine),
			Builtin.Rnd => BuiltinRnd(args, currentBasicLine),
			Builtin.Sgn => BuiltinSgn(args, currentBasicLine),
			Builtin.Sin => BuiltinSin(args, currentBasicLine),
			Builtin.Spc => BuiltinSpc(args, currentBasicLine),
			Builtin.Sqr => BultinSqr(args, currentBasicLine),
			Builtin.Str => BultinStr(args, currentBasicLine),
			Builtin.Tab => BultinTab(args, currentBasicLine),
			Builtin.Tan => BultinTan(args, currentBasicLine),
			Builtin.Val => BultinVal(args, currentBasicLine),
			_ => throw new NotImplementedException($"Builtin function {builtin}. Line: {currentBasicLine}"),
		};
	}

	static void ThrowIfNotNumericArg0(IReadOnlyList<object> args, string message, int currentBasicLine)
	{
		if (!HasNumericArg0(args))
			throw new TypeMismatchError(message, currentBasicLine);
	}

	static float BultinAbs(List<object> args, int currentBasicLine)
	{
		return Math.Abs(args[0].AsFloat(currentBasicLine));
	}

	static float BuiltinAsc(List<object> args, int currentBasicLine)
	{
		var ascStr = (string)args[0];
		if (string.IsNullOrEmpty(ascStr))
			throw new InvalidArgumentError("ASC argument is empty", currentBasicLine);
		return ascStr[0];
	}

	static float BultinAtn(List<object> args, int currentBasicLine)
	{
		return (float)Math.Atan(args[0].AsFloat(currentBasicLine));
	}

	static string BuiltinChr(List<object> args, int currentBasicLine)
	{
		ThrowIfNotNumericArg0(args, "CHR$ expects 1 numeric arg", currentBasicLine);

		var chrCode = args[0].AsInt(currentBasicLine);
		if (chrCode < 0 || chrCode > 255)
			throw new InvalidArgumentError($"CHR$ code {chrCode} out of range (0-255)", currentBasicLine);
		return ((char)chrCode).ToString();
	}

	static float BuiltinCos(List<object> args, int currentBasicLine)
	{
		return (float)Math.Cos(args[0].AsFloat(currentBasicLine));
	}

	static float BultinExp(List<object> args, int currentBasicLine)
	{
		return (float)Math.Exp(args[0].AsFloat(currentBasicLine));
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

	static float BultinLen(List<object> args, int currentBasicLine)
	{
		return ((string)args[0]).Length;
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

	static float BuiltinSin(List<object> args, int currentBasicLine)
	{
		return (float)Math.Sin(args[0].AsFloat(currentBasicLine));
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

	static float BultinTan(List<object> args, int currentBasicLine)
	{
		return (float)Math.Tan(args[0].AsFloat(currentBasicLine));
	}

	static float BultinVal(List<object> args, int currentBasicLine)
	{
		string valStr = ((string)args[0]).Trim();
		string numPart = "";
		bool d = false;
		foreach (char c in valStr)
		{
			if (Char.IsDigit(c))
			{
				numPart += c;
				d = true;
			}
			else if (c == '.' && !numPart.Contains('.'))
			{
				numPart += c;
			}
			else if ((c == 'E' || c == 'e') && !numPart.ToUpper().Contains('E') && d)
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

		return RuntimeParsingUtils.ParseFloat(numPart);
	}
}
