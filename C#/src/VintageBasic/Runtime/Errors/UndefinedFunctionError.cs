using VintageBasic.Syntax; // For VarName

namespace VintageBasic.Runtime.Errors;

sealed class UndefinedFunctionError : BasicRuntimeException
{
    public VarName FunctionName { get; }

    public UndefinedFunctionError(VarName functionName, string message = "Undefined function", int? lineNumber = null)
        : base($"{message}: {functionName}", lineNumber)
    {
        FunctionName = functionName;
    }
}
