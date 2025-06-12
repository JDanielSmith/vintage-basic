using VintageBasic.Syntax;

namespace VintageBasic.Runtime.Errors;

class BasicRuntimeException : Exception
{
	public BasicRuntimeException() : base() { }
	public BasicRuntimeException(string message) : this(message, (int?)null) { }
	public BasicRuntimeException(string message, Exception innerException) : this(message, lineNumber: null, innerException) { }

	public int? LineNumber { get; }
	public BasicRuntimeException(string message, int? lineNumber, Exception? innerException = null) : base(AppendLinenumber(message, lineNumber), innerException)
	{
		LineNumber = lineNumber;
	}

	static string AppendLinenumber(string message, int? lineNumber)
	{
		if (lineNumber.HasValue)
		{
			return $"{message} at line {lineNumber.Value}";
		}
		return message;
	}
}
sealed class BadGosubTargetError : BasicRuntimeException
{
	public BadGosubTargetError() : this("Bad GOSUB target") { }
	public BadGosubTargetError(string message) : base(message) { }
	public BadGosubTargetError(string message, Exception innerException) : base(message, innerException) { }
	public BadGosubTargetError(int targetLabel, int? lineNumber) : base($"Bad GOSUB target: {targetLabel}", lineNumber) { }
}

sealed class BadGotoTargetError : BasicRuntimeException
{
	public BadGotoTargetError() : this("Bad GOTO target") { }
	public BadGotoTargetError(string message) : base(message) { }
	public BadGotoTargetError(string message, Exception innerException) : base(message, innerException) { }
	public BadGotoTargetError(int targetLabel, int lineNumber) : base($"Bad GOTO target: {targetLabel}", lineNumber) { }
}
sealed class BadRestoreTargetError : BasicRuntimeException
{
	public BadRestoreTargetError() : this("Bad RESTORE target") { }
	public BadRestoreTargetError(string message) : base(message) { }
	public BadRestoreTargetError(string message, Exception innerException) : base(message, innerException) { }
	public BadRestoreTargetError(int targetLabel, int lineNumber) : base($"Bad RESTORE target: {targetLabel}", lineNumber) { }
}

sealed class DivisionByZeroError : BasicRuntimeException
{
	public DivisionByZeroError() : this("Division by zero") { }
	public DivisionByZeroError(string message) : base(message) { }
	public DivisionByZeroError(string message, Exception innerException) : base(message, innerException) { }
	public DivisionByZeroError(int lineNumber) : base("Division by zero", lineNumber) { }
}

sealed class EndOfInputError : BasicRuntimeException
{
	public EndOfInputError() : this("End of input") { }
	public EndOfInputError(string message) : base(message) { }
	public EndOfInputError(string message, Exception innerException) : base(message, innerException) { }
	public EndOfInputError(string message, int lineNumber) : base(message, lineNumber) { }
	public EndOfInputError(int lineNumber) : this("End of input", lineNumber) { }
}

sealed class InvalidArgumentError : BasicRuntimeException
{
	public InvalidArgumentError() : this("Invalid argument") { }
	public InvalidArgumentError(string message) : base(message) { }
	public InvalidArgumentError(string message, Exception innerException) : base(message, innerException) { }
	public InvalidArgumentError(string message, int lineNumber) : base(message, lineNumber) { }
}

sealed class MismatchedArrayDimensionsError : BasicRuntimeException
{
	public MismatchedArrayDimensionsError() : this("Mismatched array dimensions") { }
	public MismatchedArrayDimensionsError(string message) : base(message) { }
	public MismatchedArrayDimensionsError(string message, Exception innerException) : base(message, innerException) { }
}

sealed class NegativeArrayDimError : BasicRuntimeException
{
	public NegativeArrayDimError() : this("Negative array dimension") { }
	public NegativeArrayDimError(string message) : base(message) { }
	public NegativeArrayDimError(string message, Exception innerException) : base(message, innerException) { }
}

sealed class OutOfArrayBoundsError : BasicRuntimeException
{
	public OutOfArrayBoundsError() : this("Out of array bounds") { }
	public OutOfArrayBoundsError(string message, int? lineNumber) : base(message, lineNumber) { }
	public OutOfArrayBoundsError(string message) : base(message) { }
	public OutOfArrayBoundsError(string message, Exception innerException) : base(message, innerException) { }
}

sealed class OutOfDataError : BasicRuntimeException
{
	public OutOfDataError() : this("Out of data") { }
	public OutOfDataError(string message) : base(message) { }
	public OutOfDataError(string message, Exception innerException) : base(message, innerException) { }
	public OutOfDataError(int lineNumber) : base("Out of data", lineNumber) { }
}

sealed class RedimensionedArrayError : BasicRuntimeException
{
	public RedimensionedArrayError() : this("Re-dimensioned array") { }
	public RedimensionedArrayError(string message) : base(message) { }
	public RedimensionedArrayError(string message, Exception innerException) : base(message, innerException) { }
}

sealed class TypeMismatchError : BasicRuntimeException
{
	public TypeMismatchError() : this("Type mismatch") { }
	public TypeMismatchError(string message) : base(message) { }
	public TypeMismatchError(string message, Exception innerException) : base(message, innerException) { }
	public TypeMismatchError(int lineNumber) : this("Type mismatch", lineNumber) { }
	public TypeMismatchError(string message, int? lineNumber) : base(message, lineNumber) { }
}

sealed class UndefinedFunctionError : BasicRuntimeException
{
	public UndefinedFunctionError() : this("Undefined function") { }
	public UndefinedFunctionError(string message) : base(message) { }
	public UndefinedFunctionError(string message, Exception innerException) : base(message, innerException) { }
	public UndefinedFunctionError(VarName functionName) : this($"Undefined function: {functionName}") { }
}

sealed class WrongNumberOfArgumentsError : BasicRuntimeException
{
	public WrongNumberOfArgumentsError() : this("Wrong number of arguments") { }
	public WrongNumberOfArgumentsError(string message) : base(message) { }
	public WrongNumberOfArgumentsError(string message, Exception innerException) : base(message, innerException) { }
	public WrongNumberOfArgumentsError(string message, int lineNumber) : base(message, lineNumber) { }
}