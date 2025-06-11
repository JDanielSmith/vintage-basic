using System.Globalization;
using VintageBasic.Interpreter;
using VintageBasic.Runtime;
using VintageBasic.Runtime.Errors;

namespace VintageBasic.Syntax;

abstract record Statement
{
	Interpreter.Interpreter? _interpreter; // Initialized in Execute method
	protected Interpreter.Interpreter Interpreter => _interpreter!;
	public void Execute(Interpreter.Interpreter interpreter)
	{
		_interpreter = interpreter;
		ExecuteImpl();
	}

	protected RuntimeContext _context => Interpreter.Context;
	protected VariableManager _variableManager => Interpreter.VariableManager;
	protected InputOutputManager _ioManager => Interpreter.IoManager;
	protected StateManager _stateManager => Interpreter.StateManager;
	protected IReadOnlyList<JumpTableEntry> _jumpTable => Interpreter.JumpTable;

	protected int currentBasicLine => _stateManager.CurrentLineNumber;

	protected abstract void ExecuteImpl();

}

sealed record LetStatement(Var Variable, Expression Expression) : Statement
{
	public override string ToString() => $"{nameof(LetStatement)}({Variable}, {Expression})";
	protected override void ExecuteImpl()
	{
		var valueToAssign = Interpreter.EvaluateExpression(Expression, currentBasicLine);
		var coercedValue = Variable.CoerceToType(valueToAssign, currentBasicLine, _stateManager);
		if (Variable is ScalarVar sv) _variableManager.SetScalarVar(sv.VarName, coercedValue);
		else if (Variable is ArrVar av)
		{
			var indices = Interpreter.EvaluateIndices(av.Dimensions, currentBasicLine);
			_variableManager.SetArrayVar(av.VarName, indices, coercedValue);
		}
		else throw new NotImplementedException($"Variable type {Variable.GetType().Name} in LET not supported.");
	}
}

sealed record DimStatement(IReadOnlyList<(VarName Name, IReadOnlyList<Expression> Dimensions)> Declarations) : Statement
{
	public override string ToString() => $"{nameof(DimStatement)}([{string.Join(", ", Declarations.Select(d => $"{d.Name}({string.Join(", ", d.Dimensions)})"))}])";

	protected override void ExecuteImpl()
	{
		foreach (var (Name, Dimensions) in Declarations)
		{
			var bounds = new List<int>();
			foreach (var exprBound in Dimensions)
			{
				var boundVal = Interpreter.EvaluateExpression(exprBound, currentBasicLine);
				bounds.Add(boundVal.AsInt(currentBasicLine));
			}
			_variableManager.DimArray(Name, bounds);
		}
	}
}

sealed record GotoStatement(int TargetLabel) : Statement
{
	public override string ToString() => $"{nameof(GotoStatement)}({TargetLabel})";
	protected override void ExecuteImpl()
	{
		if (!_jumpTable.Any(jte => jte.Label == TargetLabel))
			throw new BadGotoTargetError(TargetLabel, currentBasicLine);
		_stateManager.SetCurrentLineNumber(TargetLabel);
		Interpreter._nextInstructionIsJump = true;
	}
}

sealed record GosubStatement(int TargetLabel) : Statement
{
	public override string ToString() => $"{nameof(GosubStatement)}({TargetLabel})";

	protected override void ExecuteImpl()
	{
		if (!_jumpTable.Any(jte => jte.Label == TargetLabel))
			throw new BadGosubTargetError(TargetLabel, currentBasicLine);
		_context.State.GosubReturnStack.Push(Interpreter._currentProgramLineIndex);
		_stateManager.SetCurrentLineNumber(TargetLabel);
		Interpreter._nextInstructionIsJump = true;
	}
}

sealed record OnGotoStatement(Expression Expression, IReadOnlyList<int> TargetLabels) : Statement
{
	public override string ToString() => $"{nameof(OnGotoStatement)}({Expression}, [{string.Join(", ", TargetLabels)}])";
	protected override void ExecuteImpl()
	{
		Object indexValGoto = Interpreter.EvaluateExpression(Expression, currentBasicLine);
		int indexGoto = indexValGoto.AsInt(currentBasicLine);
		if (indexGoto >= 1 && indexGoto <= TargetLabels.Count)
		{
			int targetLabel = TargetLabels[indexGoto - 1];
			if (!_jumpTable.Any(jte => jte.Label == targetLabel))
				throw new BadGotoTargetError(targetLabel, currentBasicLine);
			_stateManager.SetCurrentLineNumber(targetLabel);
			Interpreter._nextInstructionIsJump = true;
		}
	}
}

sealed record OnGosubStatement(Expression Expression, IReadOnlyList<int> TargetLabels) : Statement
{
	public override string ToString() => $"{nameof(OnGosubStatement)}({Expression}, [{string.Join(", ", TargetLabels)}])";

	protected override void ExecuteImpl()
	{
		Object indexValGosub = Interpreter.EvaluateExpression(Expression, currentBasicLine);
		int indexGosub = indexValGosub.AsInt(currentBasicLine);
		if (indexGosub >= 1 && indexGosub <= TargetLabels.Count)
		{
			int targetLabel = TargetLabels[indexGosub - 1];
			if (!_jumpTable.Any(jte => jte.Label == targetLabel))
				throw new BadGosubTargetError(targetLabel, currentBasicLine);
			_context.State.GosubReturnStack.Push(Interpreter._currentProgramLineIndex);
			_stateManager.SetCurrentLineNumber(targetLabel);
			Interpreter._nextInstructionIsJump = true;
		}
	}
}

sealed record ReturnStatement : Statement
{
	public override string ToString() => nameof(ReturnStatement);

	protected override void ExecuteImpl()
	{
		if (!_context.State.GosubReturnStack.Any())
			throw new BasicRuntimeException("RETURN without GOSUB", currentBasicLine);
		Interpreter._currentProgramLineIndex = _context.State.GosubReturnStack.Pop();
		Interpreter._nextInstructionIsJump = false;
	}
}

sealed record IfStatement(Expression Condition, IReadOnlyList<Tagged<Statement>> Statements) : Statement
{
	public override string ToString() => $"{nameof(IfStatement)}({Condition}, [{string.Join("; ", Statements.Select(s => s.ToString()))}])";

	protected override void ExecuteImpl()
	{
		Object condition = Interpreter.EvaluateExpression(Condition, currentBasicLine);
		if (condition.AsInt(currentBasicLine) == 0)
			return;
		foreach (var thenStmtTagged in Statements)
		{
			Interpreter.InterpretStatement(thenStmtTagged);
			if (Interpreter._programEnded || Interpreter._nextInstructionIsJump)
				return;
		}
	}
}

sealed record ForStatement(VarName LoopVariable, Expression InitialValue, Expression LimitValue, Expression StepValue) : Statement
{
	public override string ToString() => $"{nameof(ForStatement)}({LoopVariable}, {InitialValue}, {LimitValue}, {StepValue})";

	protected override void ExecuteImpl()
	{
		if (_context.State.ForLoopStack.TryPeek(out var existingLoopContext) && (existingLoopContext.LoopVariable.Name == LoopVariable.Name))
		{
			if (existingLoopContext.SingleLine)
			{
				return; // If this is a single-line FOR loop, we don't reinitialize it.
			}
		}
		Object startVal = Interpreter.EvaluateExpression(InitialValue, currentBasicLine);
		Object limitVal = Interpreter.EvaluateExpression(LimitValue, currentBasicLine);
		Object stepVal = Interpreter.EvaluateExpression(StepValue, currentBasicLine);
		Object coercedStartVal = LoopVariable.CoerceToType(startVal, currentBasicLine, _stateManager);
		_variableManager.SetScalarVar(LoopVariable, coercedStartVal);
		_context.State.ForLoopStack.Push(new ForLoopContext(LoopVariable, limitVal, stepVal, Interpreter._currentProgramLineIndex));
	}
}

sealed record NextStatement(IReadOnlyList<VarName>? LoopVariables) : Statement // Nullable for simple NEXT
{
	public override string ToString() => $"{nameof(NextStatement)}([{string.Join(", ", LoopVariables?.Select(v => v.ToString()) ?? [])}])";

	protected override void ExecuteImpl()
	{
		if (!_context.State.ForLoopStack.Any()) throw new BasicRuntimeException("NEXT without FOR", currentBasicLine);
		var loopVarNamesInNext = LoopVariables ?? [_context.State.ForLoopStack.Peek().LoopVariable];
		foreach (var varNameInNextClause in loopVarNamesInNext)
		{
			if (!_context.State.ForLoopStack.Any() || _context.State.ForLoopStack.Peek().LoopVariable.Name != varNameInNextClause.Name)
				throw new BasicRuntimeException($"NEXT variable {varNameInNextClause.Name} does not match current FOR loop variable", currentBasicLine);
			var currentLoop = _context.State.ForLoopStack.Peek();
			var currentValue = _variableManager.GetScalarVar(currentLoop.LoopVariable);
			var addedValue = Interpreter.EvaluateBinOp(BinOp.AddOp, currentValue, currentLoop.StepValue, currentBasicLine);
			var newLoopVal = currentLoop.LoopVariable.CoerceToType(addedValue, currentBasicLine, _stateManager);
			_variableManager.SetScalarVar(currentLoop.LoopVariable, newLoopVal);
			var step = currentLoop.StepValue.AsFloat(currentBasicLine);
			var limit = currentLoop.LimitValue.AsFloat(currentBasicLine);
			var current = newLoopVal.AsFloat(currentBasicLine);
			var loopContinues = (step >= 0) ? (current <= limit) : (current >= limit);
			if (loopContinues)
			{
				currentLoop.SingleLine = Interpreter._currentProgramLineIndex == currentLoop.LoopStartLineIndex; ;
				Interpreter._currentProgramLineIndex = currentLoop.LoopStartLineIndex;
				var index = Interpreter._currentProgramLineIndex + 1;
				if (currentLoop.SingleLine)
				{
					index--;
				}
				_stateManager.SetCurrentLineNumber(_jumpTable[index].Label);
				Interpreter._nextInstructionIsJump = true;
				return;
			}
			_context.State.ForLoopStack.Pop();
		}
	}
}

sealed record PrintStatement(IEnumerable<Expression> Expressions) : Statement
{
	public override string ToString() => $"{nameof(PrintStatement)}([{string.Join(", ", Expressions.Select(e => e.ToString()))}])";

	static string PrintVal(object val)
	{
		switch (val)
		{
			case Single fv: return RuntimeParsingUtils.PrintFloat(fv);
			case Int32 iv:
				string s = iv.ToString(CultureInfo.InvariantCulture);
				return (iv >= 0 && (s.Length > 0 && s[0] != '-') ? " " : "") + s + " ";
			case String sv: return sv;
			default: throw new ArgumentOutOfRangeException(nameof(val), $"Unknown Object type for printing: {val.GetType()}");
		}
	}
	protected override void ExecuteImpl()
	{
		foreach (var expr in Expressions)
		{
			if (expr is NextZoneExpression)
			{
				int currentColumn = _ioManager.OutputColumn;
				int spacesToNextZone = InputOutputManager.ZoneWidth - (currentColumn % InputOutputManager.ZoneWidth);
				if (currentColumn > 0 && (currentColumn % InputOutputManager.ZoneWidth == 0)) spacesToNextZone = InputOutputManager.ZoneWidth;
				if (spacesToNextZone > 0 && spacesToNextZone <= InputOutputManager.ZoneWidth) _ioManager.PrintString(new String(' ', spacesToNextZone));
			}
			else if (expr is EmptyZoneExpression) { /* No space */ }
			else
			{
				Object val = Interpreter.EvaluateExpression(expr, currentBasicLine);
				if (val is string sv && (sv == "<Special:NextZone>" || sv == "<Special:EmptySeparator>")) continue;
				_ioManager.PrintString(PrintVal(val));
			}
		}
		if (!Expressions.Any() || !(Expressions.Last().IsPrintSeparator)) _ioManager.PrintString("\n");
	}
}

sealed record InputStatement(string? Prompt, IReadOnlyList<Var> Variables) : Statement
{
	public override string ToString() => $"{nameof(InputStatement)}(\"{Prompt}\", [{string.Join(", ", Variables.Select(v => v.ToString()))}])";

	protected override void ExecuteImpl()
	{
		// Improved INPUT statement handling
		List<object> valuesToAssignThisInput = [];
		Queue<string> availableInputStrings = new();
		bool retryCurrentInputEntirely;
		bool firstPrompt = true;

		do
		{
			retryCurrentInputEntirely = false;
			valuesToAssignThisInput.Clear();
			// availableInputStrings are intentionally not cleared here to allow using leftover from previous good line.
			// However, on retry, they should be cleared.

			if (!string.IsNullOrEmpty(Prompt) && firstPrompt)
			{
				_ioManager.PrintString(Prompt);
				firstPrompt = false; // Main prompt only once
			}

			for (int varIndex = 0; varIndex < Variables.Count; varIndex++)
			{
				var targetVar = Variables[varIndex];

				if (!availableInputStrings.Any()) // Need more input values from console
				{
					_ioManager.PrintString("? "); // Prompt for more input
					var lineRead = _ioManager.ReadLine();
					var parsedLineValues = RuntimeParsingUtils.ParseDataLineContent(lineRead);
					foreach (var v in parsedLineValues)
						availableInputStrings.Enqueue(v);
					if (!availableInputStrings.Any() && Variables.Count > varIndex)
					{
						varIndex--; // Re-process current variable with new input line.
						continue;
					}
				}

				if (!availableInputStrings.Any()) // Still no values after trying to read
				{
					throw new EndOfInputError("Not enough input values provided.", currentBasicLine);
				}

				var strValueFromInput = availableInputStrings.Dequeue();
				var parsedVal = targetVar.Name.Val.TryParse(strValueFromInput);
				if (parsedVal is null)
				{
					_ioManager.PrintString("!NUMBER EXPECTED - RETRY INPUT LINE\n");
					retryCurrentInputEntirely = true;
					availableInputStrings.Clear(); // Discard remaining values from this erroneous line
					break; // Break from variables loop, outer do-while will retry entire INPUT
				}
				valuesToAssignThisInput.Add(targetVar.CoerceToType(parsedVal, currentBasicLine, _stateManager));
			}

		} while (retryCurrentInputEntirely);

		// Assign all collected and validated values
		for (int i = 0; i < Variables.Count; i++)
		{
			var targetVar = Variables[i];
			var valueToAssign = valuesToAssignThisInput[i];
			targetVar.SetVar(Interpreter, valueToAssign);
		}
		// If availableInputStrings still has items, they are extra and ignored (common BASIC behavior).
	}
}

sealed record EndStatement : Statement
{
	public override string ToString() => nameof(EndStatement);
	protected override void ExecuteImpl()
	{
		Interpreter._programEnded = true;
	}
}

sealed record StopStatement : Statement
{
	public override string ToString() => nameof(StopStatement);
	protected override void ExecuteImpl()
	{
		Interpreter._programEnded = true;
	}
}

sealed record RandomizeStatement : Statement
{
	public override string ToString() => nameof(RandomizeStatement);
	protected override void ExecuteImpl()
	{
		Interpreter._randomManager.SeedRandomFromTime();
	}
}

sealed record ReadStatement(IReadOnlyList<Var> Variables) : Statement
{
	public override string ToString() => $"{nameof(ReadStatement)}([{string.Join(", ", Variables.Select(v => v.ToString()))}])";

	protected override void ExecuteImpl()
	{
		foreach (var varToRead in Variables)
		{
			var dataStr = _ioManager.ReadData();
			var val = varToRead.Name.Val.TryParse(dataStr) ?? throw new TypeMismatchError($"Invalid data format '{dataStr}' for variable {varToRead.Name}", currentBasicLine);
			var coercedVal = varToRead.CoerceToType(val, currentBasicLine, _stateManager);
			if (varToRead is ScalarVar sv)
				_variableManager.SetScalarVar(sv.VarName, coercedVal);
			else if (varToRead is ArrVar av)
			{
				var indices = Interpreter.EvaluateIndices(av.Dimensions, currentBasicLine);
				_variableManager.SetArrayVar(av.VarName, indices, coercedVal);
			}
		}
	}
}

sealed record RestoreStatement(int? TargetLabel) : Statement
{
	public override string ToString() => $"{nameof(RestoreStatement)}({TargetLabel?.ToString() ?? "Start"})";

	protected override void ExecuteImpl()
	{
		if (TargetLabel.HasValue)
		{
			var targetLabel = TargetLabel.Value;
			if (!_jumpTable.Any(jte => jte.Label == targetLabel))
				throw new BadRestoreTargetError(targetLabel, currentBasicLine);
			var dataFromTargetOnwards = _jumpTable.Where(jte => jte.Label >= targetLabel).SelectMany(jte => jte.Data).ToList();
			_ioManager.RestoreData(dataFromTargetOnwards);
		}
		else
		{
			_ioManager.RestoreData([.. _jumpTable.SelectMany(jte => jte.Data)]);
		}
	}
}

sealed record DataStatement(string Data) : Statement
{
	public override string ToString() => $"{nameof(DataStatement)}(\"{Data}\")";
	protected override void ExecuteImpl() { }
}

sealed record DefFnStatement(VarName FunctionName, IReadOnlyList<VarName> Parameters, Expression Expression) : Statement
{
	public override string ToString() => $"{nameof(DefFnStatement)}({FunctionName}, [{string.Join(", ", Parameters.Select(p => p.ToString()))}], {Expression})";

	protected override void ExecuteImpl()
	{
		object udf(IReadOnlyList<object> argsFromInvocation)
		{
			if (argsFromInvocation.Count != Parameters.Count)
				throw new WrongNumberOfArgumentsError($"Function {FunctionName} expects {Parameters.Count} args, got {argsFromInvocation.Count}", _stateManager.CurrentLineNumber);
			Dictionary<VarName, object?> stashedValues = [];
			for (int i = 0; i < Parameters.Count; i++)
			{
				var paramName = Parameters[i];
				stashedValues[paramName] = _variableManager.GetScalarVar(paramName);
				_variableManager.SetScalarVar(paramName, paramName.CoerceToType(argsFromInvocation[i], _stateManager.CurrentLineNumber, _stateManager));
			}
			var result = Interpreter.EvaluateExpression(Expression, _stateManager.CurrentLineNumber);
			foreach (var paramName in Parameters)
			{
				//if (stashedValues.TryGetValue(paramName, out Object? stashedVal) && stashedVal is not null)
				//    _variableManager.SetScalarVar(paramName, stashedVal);
				//else
				//{
				//    Object val = paramName.XXXEqualsType(String.Empty) ? String.Empty : Single.Empty;
				// _variableManager.SetScalarVar(paramName, paramName.CoerceToType(val, _stateManager.CurrentLineNumber, _stateManager));
				//}
				throw new NotImplementedException($"Function {FunctionName} does not support array parameters yet."); // TODO: Handle arrays in UDFs
			}
			return FunctionName.CoerceToType(result, _stateManager.CurrentLineNumber, _stateManager);
		}
		Interpreter._functionManager.SetFunction(FunctionName, udf);
	}
}

sealed record RemStatement(string Comment) : Statement
{
	public override string ToString() => $"{nameof(RemStatement)}(\"{Comment}\")";

	protected override void ExecuteImpl()
	{
	}
}
