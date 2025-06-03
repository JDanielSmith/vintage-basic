// src/VintageBasic/Runtime/Errors/UndefinedFunctionError.cs
using VintageBasic.Syntax; // For VarName

namespace VintageBasic.Runtime.Errors
{
    public class UndefinedFunctionError : BasicRuntimeException
    {
        public VarName FunctionName { get; }

        public UndefinedFunctionError(VarName functionName, string message = "Undefined function", int? lineNumber = null)
            : base($"{message}: {functionName}", lineNumber)
        {
            FunctionName = functionName;
        }
    }
}
