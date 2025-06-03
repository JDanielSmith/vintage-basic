// src/VintageBasic/Runtime/FunctionManager.cs
using System;
using VintageBasic.Syntax;
using VintageBasic.Runtime.Errors;

namespace VintageBasic.Runtime;

public class FunctionManager
{
    private readonly BasicStore _store;

    public FunctionManager(BasicStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public UserDefinedFunction GetFunction(VarName funcName)
    {
        if (_store.UserFunctions.TryGetValue(funcName, out UserDefinedFunction? function))
        {
            return function;
        }
        // It's important to pass the current line number for accurate error reporting.
        // However, FunctionManager doesn't have direct access to BasicState.CurrentLineNumber.
        // This suggests that error throwing should ideally happen at a higher level (e.g., in the interpreter)
        // where the current line number is known.
        // For now, we'll throw without a line number from here, or consider if RuntimeContext should be passed.
        throw new UndefinedFunctionError(funcName);
    }

    public void SetFunction(VarName funcName, UserDefinedFunction function)
    {
        if (function == null)
        {
            throw new ArgumentNullException(nameof(function));
        }
        _store.UserFunctions[funcName] = function;
    }
}
