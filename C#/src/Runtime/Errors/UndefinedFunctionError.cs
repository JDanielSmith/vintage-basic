using VintageBasic.Syntax; // For VarName

namespace VintageBasic.Runtime.Errors;

sealed class UndefinedFunctionError : BasicRuntimeException
{
	public UndefinedFunctionError() : this("Undefined function") { }
	public UndefinedFunctionError(string message) : base(message) { }
	public UndefinedFunctionError(string message, Exception innerException) : base(message, innerException) { }
	public UndefinedFunctionError(VarName functionName) : this($"Undefined function: {functionName}") { }
}
