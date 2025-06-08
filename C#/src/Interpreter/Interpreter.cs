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
	readonly FunctionManager _functionManager = context.Functions;
	readonly RandomManager _randomManager = context.Random;
	readonly StateManager _stateManager = context.ProgramState;

	List<JumpTableEntry> _jumpTable = [];
    bool _programEnded;
    int _currentProgramLineIndex = -1; 
    bool _nextInstructionIsJump;

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

        if (!_jumpTable.Any())
        {
            return;
        }

        _programEnded = false;
        _currentProgramLineIndex = 0; 
        if(_jumpTable.Any()) _stateManager.SetCurrentLineNumber(_jumpTable[_currentProgramLineIndex].Label);


        while (!_programEnded && _currentProgramLineIndex < _jumpTable.Count && _currentProgramLineIndex >= 0)
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

    void InterpretStatement(Tagged<Statement> taggedStatement)
    {
        _stateManager.SetCurrentLineNumber(taggedStatement.Position.Line > 0 ? taggedStatement.Position.Line : _stateManager.CurrentLineNumber);
        ExecuteStatement(taggedStatement.Value);
    }

    void ExecuteStatement(Statement statement)
    {
        int currentBasicLine = _stateManager.CurrentLineNumber;

        switch (statement)
        {
            case RemStatement _: break;
            case EndStatement _: case StopStatement _: _programEnded = true; break;
            
            case GotoStatement gotoStmt:
                if (!_jumpTable.Any(jte => jte.Label == gotoStmt.TargetLabel))
                    throw new BadGotoTargetError(gotoStmt.TargetLabel, currentBasicLine);
                _stateManager.SetCurrentLineNumber(gotoStmt.TargetLabel);
                _nextInstructionIsJump = true;
                break;
            
            case PrintStatement printStmt: HandlePrintStatement(printStmt, currentBasicLine); break;
            case LetStatement letStmt: HandleLetStatement(letStmt, currentBasicLine); break;

            case DimStatement dimStmt:
                foreach (var decl in dimStmt.Declarations)
                {
                    var bounds = new List<int>();
                    foreach (var exprBound in decl.Dimensions)
                    {
                        Val boundVal = EvaluateExpression(exprBound, currentBasicLine);
                        bounds.Add(boundVal.AsInt(currentBasicLine)); 
                    }
                    _variableManager.DimArray(decl.Name, bounds);
                }
                break;

            case IfStatement ifStmt:
                Val condition = EvaluateExpression(ifStmt.Condition, currentBasicLine);
                if (condition.AsFloat(currentBasicLine) != 0.0f) 
                {
                    foreach (var thenStmtTagged in ifStmt.Statements)
                    {
                        InterpretStatement(thenStmtTagged);
                        if (_programEnded || _nextInstructionIsJump) break;
                    }
                }
                break;

            case GosubStatement gosubStmt:
                if (!_jumpTable.Any(jte => jte.Label == gosubStmt.TargetLabel))
                    throw new BadGosubTargetError(gosubStmt.TargetLabel, currentBasicLine);
                _context.State.GosubReturnStack.Push(_currentProgramLineIndex); 
                _stateManager.SetCurrentLineNumber(gosubStmt.TargetLabel);
                _nextInstructionIsJump = true;
                break;

            case ReturnStatement _:
                if (!_context.State.GosubReturnStack.Any())
                    throw new BasicRuntimeException("RETURN without GOSUB", currentBasicLine);
                _currentProgramLineIndex = _context.State.GosubReturnStack.Pop(); 
                _nextInstructionIsJump = false; 
                break;

            case RandomizeStatement _: _randomManager.SeedRandomFromTime(); break;

            case RestoreStatement restoreStmt:
                if (restoreStmt.TargetLabel.HasValue)
                {
                    int targetLabel = restoreStmt.TargetLabel.Value;
                    if (!_jumpTable.Any(jte => jte.Label == targetLabel))
                        throw new BadRestoreTargetError(targetLabel, currentBasicLine);
                    var dataFromTargetOnwards = _jumpTable.Where(jte => jte.Label >= targetLabel).SelectMany(jte => jte.Data).ToList();
                    _ioManager.RestoreData(dataFromTargetOnwards); 
                }
                else
                {
                    _ioManager.RestoreData(_jumpTable.SelectMany(jte => jte.Data).ToList()); 
                }
                break;

            case ReadStatement readStmt:
                foreach (var varToRead in readStmt.Variables)
                {
                    string dataStr = _ioManager.ReadData();
                    Val? val = RuntimeParsingUtils.CheckInput(varToRead.Name, dataStr) ?? throw new TypeMismatchError($"Invalid data format '{dataStr}' for variable {varToRead.Name}", currentBasicLine);
					var coercedVal = Val.CoerceToType(varToRead.GetValType(), val, currentBasicLine, _stateManager);
                    if (varToRead is ScalarVar sv)
                        _variableManager.SetScalarVar(sv.VarName, coercedVal);
                    else if (varToRead is ArrVar av)
                    {
                        var indices = EvaluateIndices(av.Dimensions, currentBasicLine);
                        _variableManager.SetArrayVar(av.VarName, indices, coercedVal);
                    }
                }
                break;
            
            case InputStatement inputStmt:
                // Improved INPUT statement handling
                var valuesToAssignThisInput = new List<Val>();
                var availableInputStrings = new Queue<string>();
                bool retryCurrentInputEntirely = false;
                bool firstPrompt = true;

                do
                {
                    retryCurrentInputEntirely = false;
                    valuesToAssignThisInput.Clear(); 
                    // availableInputStrings are intentionally not cleared here to allow using leftover from previous good line.
                    // However, on retry, they should be cleared.

                    if (!String.IsNullOrEmpty(inputStmt.Prompt) && firstPrompt)
                    {
                        _ioManager.PrintString(inputStmt.Prompt);
                        firstPrompt = false; // Main prompt only once
                    }

                    for (int varIndex = 0; varIndex < inputStmt.Variables.Count; varIndex++)
                    {
                        var targetVar = inputStmt.Variables[varIndex];
                        
                        if (!availableInputStrings.Any()) // Need more input values from console
                        {
                            _ioManager.PrintString("? "); // Prompt for more input
                            string? lineRead = _ioManager.ReadLine() ?? throw new EndOfInputError(lineNumber: currentBasicLine);
							var parsedLineValues = RuntimeParsingUtils.ParseDataLineContent(lineRead);
                            foreach(var v in parsedLineValues) availableInputStrings.Enqueue(v);
                            
                            if (!availableInputStrings.Any() && inputStmt.Variables.Count > varIndex) 
                            {
                                varIndex--; // Re-process current variable with new input line.
                                continue;
                            }
                        }

                        if (!availableInputStrings.Any()) // Still no values after trying to read
                        {
                            throw new EndOfInputError("Not enough input values provided.", currentBasicLine);
                        }

                        string strValueFromInput = availableInputStrings.Dequeue();
                        Val? parsedVal = RuntimeParsingUtils.CheckInput(targetVar.Name, strValueFromInput);

                        if (parsedVal is null)
                        {
                            _ioManager.PrintString("!NUMBER EXPECTED - RETRY INPUT LINE\n");
                            retryCurrentInputEntirely = true;
                            availableInputStrings.Clear(); // Discard remaining values from this erroneous line
                            break; // Break from variables loop, outer do-while will retry entire INPUT
                        }
                        valuesToAssignThisInput.Add(Val.CoerceToType(targetVar.GetValType(), parsedVal, currentBasicLine, _stateManager));
                    }

                } while (retryCurrentInputEntirely);

                // Assign all collected and validated values
                for (int i = 0; i < inputStmt.Variables.Count; i++)
                {
                    var targetVar = inputStmt.Variables[i];
                    var valueToAssign = valuesToAssignThisInput[i]; 
                    if (targetVar is ScalarVar sv) _variableManager.SetScalarVar(sv.VarName, valueToAssign);
                    else if (targetVar is ArrVar av)
                    {
                        var indices = EvaluateIndices(av.Dimensions, currentBasicLine);
                        _variableManager.SetArrayVar(av.VarName, indices, valueToAssign);
                    }
                }
                // If availableInputStrings still has items, they are extra and ignored (common BASIC behavior).
                break;

            case ForStatement forStmt:
                if (_context.State.ForLoopStack.TryPeek(out var existingLoopContext) &&  (existingLoopContext.LoopVariable.Name == forStmt.LoopVariable.Name))
				{
                    if (existingLoopContext.SingleLine)
                    {
                        break; // If this is a single-line FOR loop, we don't reinitialize it.
					}
				}
                Val startVal = EvaluateExpression(forStmt.InitialValue, currentBasicLine);
                Val limitVal = EvaluateExpression(forStmt.LimitValue, currentBasicLine);
                Val stepVal = EvaluateExpression(forStmt.StepValue, currentBasicLine);
                Val coercedStartVal = forStmt.LoopVariable.CoerceToType(startVal, currentBasicLine, _stateManager);
                _variableManager.SetScalarVar(forStmt.LoopVariable, coercedStartVal);
                _context.State.ForLoopStack.Push(new ForLoopContext(forStmt.LoopVariable, limitVal, stepVal, _currentProgramLineIndex));
                break;

            case NextStatement nextStmt:
                if (!_context.State.ForLoopStack.Any()) throw new BasicRuntimeException("NEXT without FOR", currentBasicLine);
                var loopVarNamesInNext = nextStmt.LoopVariables ?? [_context.State.ForLoopStack.Peek().LoopVariable];
                foreach (var varNameInNextClause in loopVarNamesInNext)
                {
                    if (!_context.State.ForLoopStack.Any() || _context.State.ForLoopStack.Peek().LoopVariable.Name != varNameInNextClause.Name)
                        throw new BasicRuntimeException($"NEXT variable {varNameInNextClause.Name} does not match current FOR loop variable", currentBasicLine);
                    var currentLoop = _context.State.ForLoopStack.Peek();
                    var currentValue = _variableManager.GetScalarVar(currentLoop.LoopVariable);
                    var addedValue = EvaluateBinOp(BinOp.AddOp, currentValue, currentLoop.StepValue, currentBasicLine);
                    var newLoopVal = currentLoop.LoopVariable.CoerceToType(addedValue, currentBasicLine, _stateManager);
                    _variableManager.SetScalarVar(currentLoop.LoopVariable, newLoopVal);
                    var step = currentLoop.StepValue.AsFloat(currentBasicLine); 
                    var limit = currentLoop.LimitValue.AsFloat(currentBasicLine); 
                    var current = newLoopVal.AsFloat(currentBasicLine); 
                    var loopContinues = (step >= 0) ? (current <= limit) : (current >= limit);
                    if (loopContinues)
                    {
						currentLoop.SingleLine = _currentProgramLineIndex == currentLoop.LoopStartLineIndex; ;
						_currentProgramLineIndex = currentLoop.LoopStartLineIndex;
						var index = _currentProgramLineIndex + 1;
                        if (currentLoop.SingleLine)
                        {
                            index--;
						}
						_stateManager.SetCurrentLineNumber(_jumpTable[index].Label);
                        _nextInstructionIsJump = true; 
                        return; 
                    }
                    _context.State.ForLoopStack.Pop(); 
                }
                break;
            
            case DefFnStatement defFnStmt:
                UserDefinedFunction udf = (argsFromInvocation) => {
                    if (argsFromInvocation.Count != defFnStmt.Parameters.Count) 
                        throw new WrongNumberOfArgumentsError($"Function {defFnStmt.FunctionName} expects {defFnStmt.Parameters.Count} args, got {argsFromInvocation.Count}", _stateManager.CurrentLineNumber);
                    var stashedValues = new Dictionary<VarName, Val?>();
                    for(int i=0; i < defFnStmt.Parameters.Count; i++)
                    {
                        var paramName = defFnStmt.Parameters[i];
                        try { stashedValues[paramName] = _variableManager.GetScalarVar(paramName); } 
                        catch { stashedValues[paramName] = null; } 
                        _variableManager.SetScalarVar(paramName, paramName.CoerceToType(argsFromInvocation[i], _stateManager.CurrentLineNumber, _stateManager));
                    }
                    Val result = EvaluateExpression(defFnStmt.Expression, _stateManager.CurrentLineNumber);
                    foreach(var paramName in defFnStmt.Parameters)
                    {
                        if (stashedValues.TryGetValue(paramName, out Val? stashedVal) && stashedVal is not null)
                            _variableManager.SetScalarVar(paramName, stashedVal);
                        else
                        {
                            Val val = paramName.GetValType() == typeof(StringVal) ? StringVal.Empty : FloatVal.Empty;
							_variableManager.SetScalarVar(paramName, paramName.CoerceToType(val, _stateManager.CurrentLineNumber, _stateManager));
                        }
                    }
                    return defFnStmt.FunctionName.CoerceToType(result, _stateManager.CurrentLineNumber, _stateManager);
                };
                _functionManager.SetFunction(defFnStmt.FunctionName, udf);
                break;

            case DataStatement _: break;

            case OnGotoStatement onGotoStmt:
                Val indexValGoto = EvaluateExpression(onGotoStmt.Expression, currentBasicLine);
                int indexGoto = indexValGoto.AsInt(currentBasicLine); 
                if (indexGoto >= 1 && indexGoto <= onGotoStmt.TargetLabels.Count)
                {
                    int targetLabel = onGotoStmt.TargetLabels[indexGoto - 1]; 
                    if (!_jumpTable.Any(jte => jte.Label == targetLabel))
                        throw new BadGotoTargetError(targetLabel, currentBasicLine);
                    _stateManager.SetCurrentLineNumber(targetLabel);
                    _nextInstructionIsJump = true;
                }
                break;

            case OnGosubStatement onGosubStmt:
                Val indexValGosub = EvaluateExpression(onGosubStmt.Expression, currentBasicLine);
                int indexGosub = indexValGosub.AsInt(currentBasicLine);
                if (indexGosub >= 1 && indexGosub <= onGosubStmt.TargetLabels.Count)
                {
                    int targetLabel = onGosubStmt.TargetLabels[indexGosub - 1];
                    if (!_jumpTable.Any(jte => jte.Label == targetLabel))
                        throw new BadGosubTargetError(targetLabel, currentBasicLine);
                    _context.State.GosubReturnStack.Push(_currentProgramLineIndex); 
                    _stateManager.SetCurrentLineNumber(targetLabel);
                    _nextInstructionIsJump = true;
                }
                break;

            default: throw new NotImplementedException($"Statement type {statement.GetType().Name} not implemented yet. Line: {currentBasicLine}");
        }
    }

    void HandlePrintStatement(PrintStatement printStmt, int currentBasicLine)
    {
        foreach (var expr in printStmt.Expressions)
        {
            if (expr is NextZoneExpression)
            {
                int currentColumn = _ioManager.OutputColumn;
                int spacesToNextZone = InputOutputManager.ZoneWidth - (currentColumn % InputOutputManager.ZoneWidth);
                if (currentColumn > 0 && (currentColumn % InputOutputManager.ZoneWidth == 0)) spacesToNextZone = InputOutputManager.ZoneWidth;
                if (spacesToNextZone > 0 && spacesToNextZone <= InputOutputManager.ZoneWidth) _ioManager.PrintString(new string(' ', spacesToNextZone));
            }
            else if (expr is EmptyZoneExpression) { /* No space */ }
            else
            {
                Val val = EvaluateExpression(expr, currentBasicLine);
                if (val is StringVal sv && (sv.Value == "<Special:NextZone>" || sv.Value == "<Special:EmptySeparator>")) continue;
                _ioManager.PrintString(PrintVal(val));
            }
        }
        if (!printStmt.Expressions.Any() || !(printStmt.Expressions.Last().IsPrintSeparator)) _ioManager.PrintString("\n");
    }
    
    static string PrintVal(Val val) 
    {
        switch (val)
        {
            case FloatVal fv: return RuntimeParsingUtils.PrintFloat(fv.Value); 
            case IntVal iv:
                string s = iv.Value.ToString(CultureInfo.InvariantCulture);
                return (iv.Value >= 0 && (s.Length > 0 && s[0] != '-') ? " " : "") + s + " "; 
            case StringVal sv: return sv.Value; 
            default: throw new ArgumentOutOfRangeException(nameof(val), $"Unknown Val type for printing: {val.GetType()}");
        }
    }

    void HandleLetStatement(LetStatement letStmt, int currentBasicLine)
    {
        var valueToAssign = EvaluateExpression(letStmt.Expression, currentBasicLine);
        var coercedValue = Val.CoerceToType(letStmt.Variable.GetValType(), valueToAssign, currentBasicLine, _stateManager);
        if (letStmt.Variable is ScalarVar sv) _variableManager.SetScalarVar(sv.VarName, coercedValue);
        else if (letStmt.Variable is ArrVar av)
        {
            var indices = EvaluateIndices(av.Dimensions, currentBasicLine);
            _variableManager.SetArrayVar(av.VarName, indices, coercedValue);
        }
        else throw new NotImplementedException($"Variable type {letStmt.Variable.GetType().Name} in LET not supported.");
    }

    List<int> EvaluateIndices(IReadOnlyList<Expression> dimExprs, int currentBasicLine)
    {
        var indices = new List<int>();
        foreach (var dimExpr in dimExprs)
        {
            Val dimVal = EvaluateExpression(dimExpr, currentBasicLine);
            indices.Add(dimVal.AsInt(currentBasicLine));
        }
        return indices;
    }

    Val EvaluateExpression(Expression expr, int currentBasicLine)
    {
         _stateManager.SetCurrentLineNumber(currentBasicLine);
        switch (expr)
        {
            case LiteralExpression l: return l.Value switch { FloatLiteral f => new FloatVal(f.Value), StringLiteral s => new StringVal(s.Value), _ => throw new NotSupportedException()};
            case VarExpression v: 
                Val val = v.Value switch { ScalarVar sv => _variableManager.GetScalarVar(sv.VarName), ArrVar av => _variableManager.GetArrayVar(av.VarName, EvaluateIndices(av.Dimensions, currentBasicLine)), _ => throw new NotSupportedException()};
                return Val.CoerceToExpressionType(val, currentBasicLine, _stateManager); 
            case ParenExpression p: return EvaluateExpression(p.Inner, currentBasicLine);
            case MinusExpression m:
                Val op = EvaluateExpression(m.Right, currentBasicLine);
                Val numOpM = Val.CoerceToExpressionType(op, currentBasicLine, _stateManager); 
                if (numOpM is FloatVal fv) return new FloatVal(-fv.Value);
                throw new TypeMismatchError("Numeric operand for unary minus.", currentBasicLine);
            case NotExpression n: 
                Val notOp = EvaluateExpression(n.Right, currentBasicLine);
                Val numOpN = Val.CoerceToExpressionType(notOp, currentBasicLine, _stateManager); 
                if (numOpN is FloatVal fvN) return new FloatVal(fvN.Value == 0.0f ? -1.0f : 0.0f); 
                throw new TypeMismatchError("Numeric operand for NOT.", currentBasicLine);
            case BinOpExpression b: return EvaluateBinOp(b.Op, EvaluateExpression(b.Left, currentBasicLine), EvaluateExpression(b.Right, currentBasicLine), currentBasicLine);
            case BuiltinExpression bi: return EvaluateBuiltin(bi.Builtin, bi.Args, currentBasicLine);
            case FnExpression fn: 
                UserDefinedFunction udf = _functionManager.GetFunction(fn.FunctionName); 
                var fnArgs = new List<Val>();
                foreach(var argExpr in fn.Args) fnArgs.Add(EvaluateExpression(argExpr, currentBasicLine));
                return udf(fnArgs); 
            case NextZoneExpression _: return new StringVal("<Special:NextZone>"); 
            case EmptyZoneExpression _: return new StringVal("<Special:EmptySeparator>"); 
            default: throw new NotImplementedException($"Expression type {expr.GetType().Name} not implemented. Line: {currentBasicLine}");
        }
    }
    
    Val EvaluateBinOp(BinOp op, Val v1, Val v2, int currentBasicLine)
    {
        _stateManager.SetCurrentLineNumber(currentBasicLine);
        Val cV1 = (op == BinOp.AddOp && v1 is StringVal) ? v1 : Val.CoerceToExpressionType(v1, currentBasicLine, _stateManager);
        Val cV2 = (op == BinOp.AddOp && v2 is StringVal) ? v2 : Val.CoerceToExpressionType(v2, currentBasicLine, _stateManager);
        switch (op)
        {
            case BinOp.AddOp:
                if (cV1 is StringVal s1 && cV2 is StringVal s2) return new StringVal(s1.Value + s2.Value);
                if (cV1 is FloatVal f1 && cV2 is FloatVal f2) return new FloatVal(f1.Value + f2.Value);
                throw new TypeMismatchError($"Cannot ADD types {cV1.TypeName} and {cV2.TypeName}", currentBasicLine);
            case BinOp.SubOp: return new FloatVal(cV1.AsFloat(currentBasicLine) - cV2.AsFloat(currentBasicLine));
            case BinOp.MulOp: return new FloatVal(cV1.AsFloat(currentBasicLine) * cV2.AsFloat(currentBasicLine));
            case BinOp.DivOp:
                float divisor = cV2.AsFloat(currentBasicLine);
                if (divisor == 0.0f) throw new DivisionByZeroError(lineNumber: currentBasicLine);
                return new FloatVal(cV1.AsFloat(currentBasicLine) / divisor);
            case BinOp.PowOp: return new FloatVal((float)Math.Pow(cV1.AsFloat(currentBasicLine), cV2.AsFloat(currentBasicLine)));
            case BinOp.EqOp: case BinOp.NEOp: case BinOp.LTOp: case BinOp.LEOp: case BinOp.GTOp: case BinOp.GEOp:
                if (!v1.IsSameType(v2))
                    throw new TypeMismatchError($"Cannot compare types {v1.TypeName} and {v2.TypeName}", currentBasicLine);
                int cr = v1.CompareTo(v2); 
                bool res = op switch { BinOp.EqOp => cr == 0, BinOp.NEOp => cr != 0, BinOp.LTOp => cr < 0, BinOp.LEOp => cr <= 0, BinOp.GTOp => cr > 0, BinOp.GEOp => cr >= 0, _ => false };
                return new FloatVal(res ? -1.0f : 0.0f); 
            case BinOp.AndOp: return new FloatVal((cV1.AsFloat(currentBasicLine) != 0.0f && cV2.AsFloat(currentBasicLine) != 0.0f) ? -1.0f : 0.0f);
            case BinOp.OrOp:  return new FloatVal((cV1.AsFloat(currentBasicLine) != 0.0f || cV2.AsFloat(currentBasicLine) != 0.0f) ? -1.0f : 0.0f);
            default: throw new NotImplementedException($"Binary operator {op}. Line: {currentBasicLine}");
        }
    }
    
    void CheckArgTypes(Builtin builtinName, List<Val> expectedTypes, List<Val> actualArgs, int currentBasicLine)
    {
        _stateManager.SetCurrentLineNumber(currentBasicLine); 
        if (expectedTypes.Count != actualArgs.Count)
        {
            if(!(builtinName == Builtin.Rnd && expectedTypes.Count == 1 && actualArgs.Count == 0)) // RND can be called with 0 or 1 arg
                throw new WrongNumberOfArgumentsError($"For {builtinName}: expected {expectedTypes.Count} arguments, got {actualArgs.Count}", currentBasicLine);
        }

        for (int i = 0; i < Math.Min(expectedTypes.Count, actualArgs.Count) ; i++)
        {
            if ((expectedTypes[i].GetType() == typeof(IntVal)) && (actualArgs[i].GetType() == typeof(FloatVal))) continue; 
            if (!expectedTypes[i].IsSameType(actualArgs[i]))
                throw new TypeMismatchError($"For {builtinName} argument {i+1}: expected {expectedTypes[i]}, got {actualArgs[i].TypeName}", currentBasicLine);
        }
    }

    static readonly FrozenDictionary<Builtin, List<Val>> BuiltinArgTypes = new Dictionary<Builtin, List<Val>>() {
        { Builtin.Abs, [ FloatVal.Empty ] }, { Builtin.Asc, [ StringVal.Empty ] }, { Builtin.Atn, [ FloatVal.Empty ] },
        { Builtin.Cos, [ FloatVal.Empty ] },
        { Builtin.Exp, [ FloatVal.Empty ] },
        { Builtin.Left, [ StringVal.Empty, FloatVal.Empty ] }, { Builtin.Len, [ StringVal.Empty ] }, { Builtin.Log, [ FloatVal.Empty ] },
        { Builtin.Right, [ StringVal.Empty, FloatVal.Empty ] },
		{ Builtin.Sin, [ FloatVal.Empty ] }, { Builtin.Sqr, [ FloatVal.Empty ] },
		{ Builtin.Tan, [ FloatVal.Empty ] },
		{ Builtin.Val, [ StringVal.Empty] },
    }.ToFrozenDictionary();

	static bool HasNumericArg0(IReadOnlyList<Val> args)
	{
        return (args.Count == 1) && args[0].IsNumeric;
	}

	Val EvaluateBuiltin(Builtin builtin, IReadOnlyList<Expression> argExprs, int currentBasicLine)
    {
		void ThrowIfNotNumericArg0(IReadOnlyList<Val> args, string message)
		{
			if (!HasNumericArg0(args))
				throw new TypeMismatchError(message, currentBasicLine);
		}

		_stateManager.SetCurrentLineNumber(currentBasicLine);

        List<Val> args = [];
        foreach (var argExpr in argExprs)
            args.Add(EvaluateExpression(argExpr, currentBasicLine)); 

        if (BuiltinArgTypes.TryGetValue(builtin, out var expectedTypes))
        {
            CheckArgTypes(builtin, expectedTypes, args, currentBasicLine);
        }

		switch (builtin)
        {
            case Builtin.Abs: return new FloatVal(Math.Abs(args[0].AsFloat(currentBasicLine)));
            case Builtin.Asc: string ascStr = ((StringVal)args[0]).Value; if (String.IsNullOrEmpty(ascStr)) throw new InvalidArgumentError("ASC argument is empty", currentBasicLine); return new FloatVal(ascStr[0]); 
            case Builtin.Atn: return new FloatVal((float)Math.Atan(args[0].AsFloat(currentBasicLine)));
            case Builtin.Chr: ThrowIfNotNumericArg0(args, "CHR$ expects 1 numeric arg"); int chrCode = args[0].AsInt(currentBasicLine); if (chrCode < 0 || chrCode > 255) throw new InvalidArgumentError($"CHR$ code {chrCode} out of range (0-255)", currentBasicLine); return new StringVal(((char)chrCode).ToString());
            case Builtin.Cos: return new FloatVal((float)Math.Cos(args[0].AsFloat(currentBasicLine)));
            case Builtin.Exp: return new FloatVal((float)Math.Exp(args[0].AsFloat(currentBasicLine)));
            case Builtin.Int: ThrowIfNotNumericArg0(args, "INT expects 1 numeric arg"); return new FloatVal((float)Math.Floor(args[0].AsFloat(currentBasicLine)));
            case Builtin.Left: string leftStr = ((StringVal)args[0]).Value; int leftN = args[1].AsInt(currentBasicLine); if (leftN < 0) leftN = 0; return new StringVal(leftStr.Substring(0, Math.Min(leftN, leftStr.Length)));
            case Builtin.Len: return new FloatVal(((StringVal)args[0]).Value.Length); 
            case Builtin.Log: float logArg = args[0].AsFloat(currentBasicLine); if (logArg <= 0) throw new InvalidArgumentError("LOG argument must be > 0", currentBasicLine); return new FloatVal((float)Math.Log(logArg));
            case Builtin.Mid:
                if (args.Count < 2 || args.Count > 3)
                    throw new WrongNumberOfArgumentsError("MID$ expects 2 or 3 args", currentBasicLine);
                CheckArgTypes(Builtin.Mid, [StringVal.Empty, FloatVal.Empty], args.Take(2).ToList(), currentBasicLine);
                if(args.Count == 3 && !args[2].IsNumeric)
                    throw new TypeMismatchError("MID$ length arg must be numeric", currentBasicLine);
                string midStr = ((StringVal)args[0]).Value;
                int midStart = args[1].AsInt(currentBasicLine);
                if (midStart < 1) midStart = 1;
                int midLen = (args.Count == 3) ? args[2].AsInt(currentBasicLine) : midStr.Length - (midStart - 1) ;
                if (midLen < 0) midLen = 0;
                if (midStart > midStr.Length || midLen == 0)
                    return StringVal.Empty;
                midStart--; // Adjust to 0-based index for Substring
                if (midStart + midLen > midStr.Length) midLen = midStr.Length - midStart;
                return new StringVal(midStr.Substring(midStart, midLen));
            case Builtin.Right: string rightStr = ((StringVal)args[0]).Value; int rightN = args[1].AsInt(currentBasicLine); if (rightN < 0) rightN = 0; return new StringVal(rightStr.Substring(Math.Max(0, rightStr.Length - rightN)));
            case Builtin.Rnd: float rndArg = (args.Any()) ? args[0].AsFloat(currentBasicLine) : 1.0f; if (rndArg < 0) _randomManager.SeedRandom((int)rndArg); double rndVal = (rndArg == 0) ? _randomManager.PreviousRandomValue : _randomManager.GetRandomValue(); return new FloatVal((float)rndVal);
            case Builtin.Sgn: ThrowIfNotNumericArg0(args, "SGN expects 1 numeric arg"); return new FloatVal(Math.Sign(args[0].AsFloat(currentBasicLine)));
            case Builtin.Sin: return new FloatVal((float)Math.Sin(args[0].AsFloat(currentBasicLine)));
            case Builtin.Spc: ThrowIfNotNumericArg0(args, "SPC expects 1 numeric arg"); int spcCount = args[0].AsInt(currentBasicLine); if (spcCount < 0) spcCount=0; return new StringVal(new string(' ', Math.Min(spcCount, 255))); 
            case Builtin.Sqr: float sqrArg = args[0].AsFloat(currentBasicLine); if (sqrArg < 0) throw new InvalidArgumentError("SQR argument < 0", currentBasicLine); return new FloatVal((float)Math.Sqrt(sqrArg));
            case Builtin.Str: ThrowIfNotNumericArg0(args, "STR$ expects 1 numeric arg"); float strNum = args[0].AsFloat(currentBasicLine); string strRep = strNum.ToString(CultureInfo.InvariantCulture); if (strNum >= 0 && (strRep.Length == 0 || strRep[0] != '-')) strRep = " " + strRep; return new StringVal(strRep);
            case Builtin.Tab: ThrowIfNotNumericArg0(args, "TAB expects 1 numeric arg"); int tabCol = args[0].AsInt(currentBasicLine); if (tabCol < 1 || tabCol > 255) throw new InvalidArgumentError($"TAB col {tabCol} out of range (1-255)", currentBasicLine); int curCol = _ioManager.OutputColumn + 1; return new StringVal(tabCol > curCol ? new string(' ', tabCol - curCol) : "");
            case Builtin.Tan: return new FloatVal((float)Math.Tan(args[0].AsFloat(currentBasicLine)));
            case Builtin.Val: string valStr = (((StringVal)args[0]).Value).Trim(); string numPart = ""; bool d = false; foreach(char c in valStr){if (Char.IsDigit(c)){numPart+=c;d=true;}else if(c=='.'&&!numPart.Contains('.')){numPart+=c;}else if((c=='E'||c=='e')&&!numPart.ToUpper().Contains('E')&&d){numPart+=c;}else if((c=='+'||c=='-')&&(numPart.Length==0||numPart.ToUpper().EndsWith('E'))){numPart+=c;}else if(Char.IsWhiteSpace(c)&&numPart.Length==0){continue;}else break;} return RuntimeParsingUtils.ParseFloat(numPart);
            default: throw new NotImplementedException($"Builtin function {builtin}. Line: {currentBasicLine}");
        }
    }
}
