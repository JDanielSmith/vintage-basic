using System.Data;
using System.Globalization;
using System.Xml.Linq;
using VintageBasic.Interpreter;
using VintageBasic.Parsing;
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

	InterpreterContext InterpreterContext => Interpreter._interpreterContext;
	protected RuntimeContext Context => InterpreterContext.Context;
	protected VariableManager VariableManager => InterpreterContext.VariableManager;
	protected InputOutputManager IoManager => InterpreterContext.IoManager;
	protected StateManager StateManager => InterpreterContext.StateManager;
	protected IReadOnlyList<JumpTableEntry> JumpTable => Interpreter._jumpTable;

	protected int CurrentBasicLine => StateManager.CurrentLineNumber;

	protected abstract void ExecuteImpl();

	protected object Evaluate(Expression expr)
	{
		return Interpreter.EvaluateExpression(expr, CurrentBasicLine);
	}
	protected int EvaluateAsInt(Expression expr)
	{
		return Evaluate(expr).AsInt(CurrentBasicLine);
	}

	protected void SetNextInstruction(int targetLabel)
	{
		StateManager.SetCurrentLineNumber(targetLabel);
		Interpreter._nextInstructionIsJump = true;
	}

	protected void ValidateJumpTarget(int targetLabel, Exception ex)
	{
		if (!JumpTable.Any(jte => jte.Label == targetLabel))
			throw ex;
	}
}

sealed record LetStatement(Var Variable, Expression Expression) : Statement
{
	public override string ToString() => $"{nameof(LetStatement)}({Variable}, {Expression})";
	protected override void ExecuteImpl()
	{
		Variable.SetVar(Interpreter, Evaluate(Expression));
	}
}

sealed record DimStatement(IEnumerable<(VarName Name, IEnumerable<Expression> Dimensions)> Declarations) : Statement
{
	public override string ToString() => $"{nameof(DimStatement)}([{string.Join(", ", Declarations.Select(d => $"{d.Name}({string.Join(", ", d.Dimensions)})"))}])";

	protected override void ExecuteImpl()
	{
		var dimArrays = from decl in Declarations
						let bounds = from exprBound in decl.Dimensions
									 select EvaluateAsInt(exprBound)
						select VariableManager.DimArray(decl.Name, bounds);
		var _ = dimArrays.ToList(); // make the actual calls to VariableManager.DimArray()
	}
}

sealed record GotoStatement(int TargetLabel) : Statement
{
	public override string ToString() => $"{nameof(GotoStatement)}({TargetLabel})";
	protected override void ExecuteImpl()
	{
		ValidateJumpTarget(TargetLabel, new BadGotoTargetError(TargetLabel, CurrentBasicLine));
		SetNextInstruction(TargetLabel);
	}
}

sealed record GosubStatement(int TargetLabel) : Statement
{
	public override string ToString() => $"{nameof(GosubStatement)}({TargetLabel})";

	protected override void ExecuteImpl()
	{
		ValidateJumpTarget(TargetLabel, new BadGosubTargetError(TargetLabel, CurrentBasicLine));
		Context.State.GosubReturnStack.Push(Interpreter._currentProgramLineIndex);
		SetNextInstruction(TargetLabel);
	}
}

sealed record OnGotoStatement(Expression Expression, IReadOnlyList<int> TargetLabels) : Statement
{
	public override string ToString() => $"{nameof(OnGotoStatement)}({Expression}, [{string.Join(", ", TargetLabels)}])";
	protected override void ExecuteImpl()
	{
		var indexGoto = EvaluateAsInt(Expression);
		if (indexGoto >= 1 && indexGoto <= TargetLabels.Count)
		{
			int targetLabel = TargetLabels[indexGoto - 1];
			ValidateJumpTarget(targetLabel, new BadGotoTargetError(targetLabel, CurrentBasicLine));
			SetNextInstruction(targetLabel);
		}
	}
}

sealed record OnGosubStatement(Expression Expression, IReadOnlyList<int> TargetLabels) : Statement
{
	public override string ToString() => $"{nameof(OnGosubStatement)}({Expression}, [{string.Join(", ", TargetLabels)}])";

	protected override void ExecuteImpl()
	{
		var indexGosub = EvaluateAsInt(Expression);
		if (indexGosub >= 1 && indexGosub <= TargetLabels.Count)
		{
			int targetLabel = TargetLabels[indexGosub - 1];
			ValidateJumpTarget(targetLabel, new BadGosubTargetError(targetLabel, CurrentBasicLine));
			Context.State.GosubReturnStack.Push(Interpreter._currentProgramLineIndex);
			SetNextInstruction(targetLabel);
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

sealed record IfStatement(Expression Condition, IEnumerable<Tagged<Statement>> Statements) : Statement
{
	public override string ToString() => $"{nameof(IfStatement)}({Condition}, [{string.Join("; ", Statements.Select(s => s.ToString()))}])";

	protected override void ExecuteImpl()
	{
		var condition = EvaluateAsInt(Condition);
		if (condition == 0)
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
		var forLoopStack = Context.State.ForLoopStack;
		if (forLoopStack.TryPeek(out var existingLoopContext))
		{
			if (existingLoopContext.SingleLine && (existingLoopContext.LoopVariable == LoopVariable))
			{
				return; // If this is a single-line FOR loop, we don't reinitialize it.
			}
		}
		var coercedInitialValue = LoopVariable.CoerceToType(Evaluate(InitialValue), CurrentBasicLine, StateManager);
		VariableManager.SetScalarVar(LoopVariable, coercedInitialValue);
		forLoopStack.Push(new(LoopVariable, Evaluate(LimitValue), Evaluate(StepValue), Interpreter._currentProgramLineIndex));
	}
}

sealed record NextStatement(IEnumerable<VarName> LoopVariables) : Statement
{
	public override string ToString() => $"{nameof(NextStatement)}([{string.Join(", ", LoopVariables.Select(v => v.ToString()) ?? [])}])";

	protected override void ExecuteImpl()
	{
		var forLoopStack = Context.State.ForLoopStack;
		if (forLoopStack.Count <= 0)
			throw new BasicRuntimeException("NEXT without FOR", CurrentBasicLine);

		var loopVarNamesInNext = LoopVariables.Any() ? LoopVariables : [forLoopStack.Peek().LoopVariable];
		foreach (var varNameInNextClause in loopVarNamesInNext)
		{
			if ((forLoopStack.Count <= 0) || forLoopStack.Peek().LoopVariable.Name != varNameInNextClause.Name)
				throw new BasicRuntimeException($"NEXT variable {varNameInNextClause.Name} does not match current FOR loop variable", CurrentBasicLine);

			var currentLoop = forLoopStack.Peek();
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
				SetNextInstruction(JumpTable[index].Label);
				return;
			}
			forLoopStack.Pop();
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
				var val = Evaluate(expr);
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
				var parsedVal = targetVar.TryParse(strValueFromInput);
				if (parsedVal is null)
				{
					IoManager.PrintString("!NUMBER EXPECTED - RETRY INPUT LINE\n");
					retryCurrentInputEntirely = true;
					availableInputStrings.Clear(); // Discard remaining values from this erroneous line
					break; // Break from variables loop, outer do-while will retry entire INPUT
				}
				valuesToAssignThisInput.Add(parsedVal);
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

sealed record ReadStatement(IEnumerable<Var> Variables) : Statement
{
	public override string ToString() => $"{nameof(ReadStatement)}([{string.Join(", ", Variables.Select(v => v.ToString()))}])";

	protected override void ExecuteImpl()
	{
		foreach (var varToRead in Variables)
		{
			var dataStr = IoManager.ReadData();
			var val = varToRead.TryParse(dataStr) ?? throw new TypeMismatchError($"Invalid data format '{dataStr}' for variable {varToRead.Name}", CurrentBasicLine);
			varToRead.SetVar(Interpreter, val);
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
			ValidateJumpTarget(targetLabel, new BadRestoreTargetError(targetLabel, CurrentBasicLine));
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
