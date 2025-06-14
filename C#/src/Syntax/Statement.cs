using System.Globalization;
using VintageBasic.Interpreter;
using VintageBasic.Runtime;
using VintageBasic.Runtime.Errors;
using VintageBasic.Parsing;

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

	protected InterpreterContext InterpreterContext => Interpreter._interpreterContext;
	protected RuntimeContext Context => InterpreterContext.Context;
	protected VariableManager VariableManager => InterpreterContext.VariableManager;
	protected InputOutputManager IoManager => InterpreterContext.IoManager;
	protected StateManager StateManager => InterpreterContext.StateManager;
	protected IReadOnlyList<JumpTableEntry> JumpTable => Interpreter._jumpTable;

	protected int CurrentBasicLine => StateManager.CurrentLineNumber;

	protected abstract void ExecuteImpl();
}

sealed record LetStatement(Var Variable, Expression Expression) : Statement
{
	public override string ToString() => $"{nameof(LetStatement)}({Variable}, {Expression})";
	protected override void ExecuteImpl()
	{
		var valueToAssign = Interpreter.EvaluateExpression(Expression, CurrentBasicLine);
		var coercedValue = Variable.CoerceToType(valueToAssign, CurrentBasicLine, StateManager);
		Variable.SetVar(Interpreter, coercedValue);
	}
}

sealed record DimStatement(IReadOnlyList<(VarName Name, IReadOnlyList<Expression> Dimensions)> Declarations) : Statement
{
	public override string ToString() => $"{nameof(DimStatement)}([{string.Join(", ", Declarations.Select(d => $"{d.Name}({string.Join(", ", d.Dimensions)})"))}])";

	protected override void ExecuteImpl()
	{
		foreach (var (Name, Dimensions) in Declarations)
		{
			var bounds = from exprBound in Dimensions
						 let boundVal = Interpreter.EvaluateExpression(exprBound, CurrentBasicLine)
						 select boundVal.AsInt(CurrentBasicLine);
			VariableManager.DimArray(Name, bounds);
		}
	}
}

sealed record GotoStatement(int TargetLabel) : Statement
{
	public override string ToString() => $"{nameof(GotoStatement)}({TargetLabel})";
	protected override void ExecuteImpl()
	{
		if (!JumpTable.Any(jte => jte.Label == TargetLabel))
			throw new BadGotoTargetError(TargetLabel, CurrentBasicLine);
		StateManager.SetCurrentLineNumber(TargetLabel);
		Interpreter._nextInstructionIsJump = true;
	}
}

sealed record GosubStatement(int TargetLabel) : Statement
{
	public override string ToString() => $"{nameof(GosubStatement)}({TargetLabel})";

	protected override void ExecuteImpl()
	{
		if (!JumpTable.Any(jte => jte.Label == TargetLabel))
			throw new BadGosubTargetError(TargetLabel, CurrentBasicLine);
		Context.State.GosubReturnStack.Push(Interpreter._currentProgramLineIndex);
		StateManager.SetCurrentLineNumber(TargetLabel);
		Interpreter._nextInstructionIsJump = true;
	}
}

sealed record OnGotoStatement(Expression Expression, IReadOnlyList<int> TargetLabels) : Statement
{
	public override string ToString() => $"{nameof(OnGotoStatement)}({Expression}, [{string.Join(", ", TargetLabels)}])";
	protected override void ExecuteImpl()
	{
		Object indexValGoto = Interpreter.EvaluateExpression(Expression, CurrentBasicLine);
		int indexGoto = indexValGoto.AsInt(CurrentBasicLine);
		if (indexGoto >= 1 && indexGoto <= TargetLabels.Count)
		{
			int targetLabel = TargetLabels[indexGoto - 1];
			if (!JumpTable.Any(jte => jte.Label == targetLabel))
				throw new BadGotoTargetError(targetLabel, CurrentBasicLine);
			StateManager.SetCurrentLineNumber(targetLabel);
			Interpreter._nextInstructionIsJump = true;
		}
	}
}

sealed record OnGosubStatement(Expression Expression, IReadOnlyList<int> TargetLabels) : Statement
{
	public override string ToString() => $"{nameof(OnGosubStatement)}({Expression}, [{string.Join(", ", TargetLabels)}])";

	protected override void ExecuteImpl()
	{
		var indexValGosub = Interpreter.EvaluateExpression(Expression, CurrentBasicLine);
		int indexGosub = indexValGosub.AsInt(CurrentBasicLine);
		if (indexGosub >= 1 && indexGosub <= TargetLabels.Count)
		{
			int targetLabel = TargetLabels[indexGosub - 1];
			if (!JumpTable.Any(jte => jte.Label == targetLabel))
				throw new BadGosubTargetError(targetLabel, CurrentBasicLine);
			Context.State.GosubReturnStack.Push(Interpreter._currentProgramLineIndex);
			StateManager.SetCurrentLineNumber(targetLabel);
			Interpreter._nextInstructionIsJump = true;
		}
	}
}

sealed record ReturnStatement : Statement
{
	public override string ToString() => nameof(ReturnStatement);

	protected override void ExecuteImpl()
	{
		if (Context.State.GosubReturnStack.Count <= 0)
			throw new BasicRuntimeException("RETURN without GOSUB", CurrentBasicLine);
		Interpreter._currentProgramLineIndex = Context.State.GosubReturnStack.Pop();
		Interpreter._nextInstructionIsJump = false;
	}
}

sealed record IfStatement(Expression Condition, IReadOnlyList<Tagged<Statement>> Statements) : Statement
{
	public override string ToString() => $"{nameof(IfStatement)}({Condition}, [{string.Join("; ", Statements.Select(s => s.ToString()))}])";

	protected override void ExecuteImpl()
	{
		var condition = Interpreter.EvaluateExpression(Condition, CurrentBasicLine);
		if (condition.AsInt(CurrentBasicLine) == 0)
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
		if (Context.State.ForLoopStack.TryPeek(out var existingLoopContext) && (existingLoopContext.LoopVariable.Name == LoopVariable.Name))
		{
			if (existingLoopContext.SingleLine)
			{
				return; // If this is a single-line FOR loop, we don't reinitialize it.
			}
		}
		var startVal = Interpreter.EvaluateExpression(InitialValue, CurrentBasicLine);
		var limitVal = Interpreter.EvaluateExpression(LimitValue, CurrentBasicLine);
		var stepVal = Interpreter.EvaluateExpression(StepValue, CurrentBasicLine);
		var coercedStartVal = LoopVariable.CoerceToType(startVal, CurrentBasicLine, StateManager);
		VariableManager.SetScalarVar(LoopVariable, coercedStartVal);
		Context.State.ForLoopStack.Push(new(LoopVariable, limitVal, stepVal, Interpreter._currentProgramLineIndex));
	}
}

sealed record NextStatement(IReadOnlyList<VarName>? LoopVariables) : Statement // Nullable for simple NEXT
{
	public override string ToString() => $"{nameof(NextStatement)}([{string.Join(", ", LoopVariables?.Select(v => v.ToString()) ?? [])}])";

	protected override void ExecuteImpl()
	{
		if (Context.State.ForLoopStack.Count <= 0)
			throw new BasicRuntimeException("NEXT without FOR", CurrentBasicLine);

		var loopVarNamesInNext = LoopVariables ?? [Context.State.ForLoopStack.Peek().LoopVariable];
		foreach (var varNameInNextClause in loopVarNamesInNext)
		{
			if ((Context.State.ForLoopStack.Count <= 0) || Context.State.ForLoopStack.Peek().LoopVariable.Name != varNameInNextClause.Name)
				throw new BasicRuntimeException($"NEXT variable {varNameInNextClause.Name} does not match current FOR loop variable", CurrentBasicLine);

			var currentLoop = Context.State.ForLoopStack.Peek();
			var currentValue = VariableManager.GetScalarVar(currentLoop.LoopVariable);
			var addedValue = Interpreter.EvaluateBinOp(BinOp.AddOp, currentValue, currentLoop.StepValue, CurrentBasicLine);
			var newLoopVal = currentLoop.LoopVariable.CoerceToType(addedValue, CurrentBasicLine, StateManager);
			VariableManager.SetScalarVar(currentLoop.LoopVariable, newLoopVal);
			var step = currentLoop.StepValue.AsFloat(CurrentBasicLine);
			var limit = currentLoop.LimitValue.AsFloat(CurrentBasicLine);
			var current = newLoopVal.AsFloat(CurrentBasicLine);
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
				StateManager.SetCurrentLineNumber(JumpTable[index].Label);
				Interpreter._nextInstructionIsJump = true;
				return;
			}
			Context.State.ForLoopStack.Pop();
		}
	}
}

sealed record PrintStatement(IEnumerable<Expression> Expressions) : Statement
{
	public override string ToString() => $"{nameof(PrintStatement)}([{string.Join(", ", Expressions.Select(e => e.ToString()))}])";

	static string PrintVal(object val)
	{
		static string PrintFloat(float f)
		{
			string s;
			if (f == 0f && BitConverter.SingleToUInt32Bits(f) == 0x80000000)
			{
				s = "-0";
			}
			else if (Math.Abs(f) >= 1e-4 && Math.Abs(f) < 1e7 || f == 0.0)
			{
				s = f.ToString("0.#######", CultureInfo.InvariantCulture);
				if (s.Contains('.', StringComparison.OrdinalIgnoreCase))
				{
					s = s.TrimEnd('0').TrimEnd('.');
				}
			}
			else
			{
				s = f.ToString("0.######E+00", CultureInfo.InvariantCulture);
			}
			return (f >= 0 && s[0] != '-' ? " " : "") + s + " ";
		}
		static string PrintInt(int iv)
		{
			string s = iv.ToString(CultureInfo.InvariantCulture);
			return (iv >= 0 && (s.Length > 0 && s[0] != '-') ? " " : "") + s + " ";
		}
		return val switch
		{
			float fv => PrintFloat(fv),
			int iv => PrintInt(iv),
			string sv => sv,
			_ => throw new ArgumentOutOfRangeException(nameof(val), $"Unknown Object type for printing: {val.GetType()}"),
		};
	}
	protected override void ExecuteImpl()
	{
		foreach (var expr in Expressions)
		{
			if (expr is NextZoneExpression)
			{
				int currentColumn = IoManager.OutputColumn;
				int spacesToNextZone = InputOutputManager.ZoneWidth - (currentColumn % InputOutputManager.ZoneWidth);
				if (currentColumn > 0 && (currentColumn % InputOutputManager.ZoneWidth == 0))
					spacesToNextZone = InputOutputManager.ZoneWidth;
				if (spacesToNextZone > 0 && spacesToNextZone <= InputOutputManager.ZoneWidth)
					IoManager.PrintString(new String(' ', spacesToNextZone));
			}
			else if (expr is EmptyZoneExpression) { /* No space */ }
			else
			{
				var val = Interpreter.EvaluateExpression(expr, CurrentBasicLine);
				if (val is string sv && (sv is NextZoneExpression.Value or EmptyZoneExpression.Value)) continue;
				IoManager.PrintString(PrintVal(val));
			}
		}
		if (!Expressions.Any() || !(Expressions.Last().IsPrintSeparator))
			IoManager.PrintString("\n");
	}
}

sealed record InputStatement(string? Prompt, IReadOnlyList<Var> Variables) : Statement
{
	public override string ToString() => $"{nameof(InputStatement)}(\"{Prompt}\", [{String.Join(", ", Variables.Select(v => v.ToString()))}])";

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

			if (!String.IsNullOrEmpty(Prompt) && firstPrompt)
			{
				IoManager.PrintString(Prompt);
				firstPrompt = false; // Main prompt only once
			}

			for (int varIndex = 0; varIndex < Variables.Count; varIndex++)
			{
				var targetVar = Variables[varIndex];

				if (availableInputStrings.Count <= 0) // Need more input values from console
				{
					IoManager.PrintString("? "); // Prompt for more input
					var lineRead = IoManager.ReadLine();
					var parsedLineValues = RuntimeParsingUtils.ParseDataLineContent(lineRead);
					foreach (var v in parsedLineValues)
						availableInputStrings.Enqueue(v);
					if ((availableInputStrings.Count <= 0) && Variables.Count > varIndex)
					{
						varIndex--; // Re-process current variable with new input line.
						continue;
					}
				}

				if (availableInputStrings.Count <= 0) // Still no values after trying to read
				{
					throw new EndOfInputError("Not enough input values provided.", CurrentBasicLine);
				}

				var strValueFromInput = availableInputStrings.Dequeue();
				var parsedVal = targetVar.Val.TryParse(strValueFromInput);
				if (parsedVal is null)
				{
					IoManager.PrintString("!NUMBER EXPECTED - RETRY INPUT LINE\n");
					retryCurrentInputEntirely = true;
					availableInputStrings.Clear(); // Discard remaining values from this erroneous line
					break; // Break from variables loop, outer do-while will retry entire INPUT
				}
				valuesToAssignThisInput.Add(targetVar.CoerceToType(parsedVal, CurrentBasicLine, StateManager));
			}

		} while (retryCurrentInputEntirely);

		for (int i = 0; i < Variables.Count; i++) // Assign all collected and validated values
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
	protected override void ExecuteImpl() => Interpreter._programEnded = true;
}

sealed record StopStatement : Statement
{
	public override string ToString() => nameof(StopStatement);
	protected override void ExecuteImpl() => Interpreter._programEnded = true;
}

sealed record RandomizeStatement : Statement
{
	public override string ToString() => nameof(RandomizeStatement);
	protected override void ExecuteImpl() => Interpreter.RandomManager.SeedRandomFromTime();
}

sealed record ReadStatement(IReadOnlyList<Var> Variables) : Statement
{
	public override string ToString() => $"{nameof(ReadStatement)}([{string.Join(", ", Variables.Select(v => v.ToString()))}])";

	protected override void ExecuteImpl()
	{
		foreach (var varToRead in Variables)
		{
			var dataStr = IoManager.ReadData();
			var val = varToRead.Val.TryParse(dataStr) ?? throw new TypeMismatchError($"Invalid data format '{dataStr}' for variable {varToRead.Name}", CurrentBasicLine);
			varToRead.SetVar(Interpreter, varToRead.CoerceToType(val, CurrentBasicLine, StateManager));
		}
	}
}

sealed record RestoreStatement(int? TargetLabel) : Statement
{
	public override string ToString() => $"{nameof(RestoreStatement)}({TargetLabel})";

	protected override void ExecuteImpl()
	{
		if (TargetLabel.HasValue)
		{
			var targetLabel = TargetLabel.Value;
			if (!JumpTable.Any(jte => jte.Label == targetLabel))
				throw new BadRestoreTargetError(targetLabel, CurrentBasicLine);
			var dataFromTargetOnwards = JumpTable.Where(jte => jte.Label >= targetLabel).SelectMany(jte => jte.Data);
			IoManager.RestoreData(dataFromTargetOnwards);
		}
		else
		{
			IoManager.RestoreData([.. JumpTable.SelectMany(jte => jte.Data)]);
		}
	}
}

sealed record DataStatement(string Data) : Statement
{
	public override string ToString() => $"{nameof(DataStatement)}(\"{Data}\")";
	protected override void ExecuteImpl() { }
}

sealed record RemStatement(string Comment) : Statement
{
	public override string ToString() => $"{nameof(RemStatement)}(\"{Comment}\")";
	protected override void ExecuteImpl() { }
}
