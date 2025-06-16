using System;
using System.Collections.Generic;

namespace vintage_basic.Language.VintageBasic;


public enum BasicResultType
{
	Pass,
	ScanError,
	SyntaxError,
	LabeledRuntimeException
}

public class BasicResult
{
	public BasicResultType Type { get; }
	public int? LineNumber { get; }
	public string Message { get; }

	public BasicResult(BasicResultType type, string message = "", int? lineNumber = null)
	{
		Type = type;
		Message = message;
		LineNumber = lineNumber;
	}

	public override string ToString()
	{
		return Type switch
		{
			BasicResultType.Pass => "NORMAL TERMINATION",
			BasicResultType.ScanError => $"!LINE NUMBERING ERROR: {Message}",
			BasicResultType.SyntaxError => $"!SYNTAX ERROR: {Message}",
			BasicResultType.LabeledRuntimeException => $"!{Message} IN LINE {LineNumber}",
			_ => "!UNKNOWN ERROR"
		};
	}
}

public enum RuntimeErrorType
{
	TypeMismatchError,
	WrongNumberOfArgumentsError,
	InvalidArgumentError,
	DivisionByZeroError,
	BadGotoTargetError,
	BadGosubTargetError,
	BadRestoreTargetError,
	NegativeArrayDimError,
	ReDimensionedArrayError,
	MismatchedArrayDimensionsError,
	OutOfArrayBoundsError,
	UndefinedFunctionError,
	OutOfDataError,
	EndOfInputError
}

public class RuntimeError
{
	public RuntimeErrorType Type { get; }
	public string AdditionalInfo { get; }

	public RuntimeError(RuntimeErrorType type, string additionalInfo = "")
	{
		Type = type;
		AdditionalInfo = additionalInfo;
	}

	public override string ToString()
	{
		return Type switch
		{
			RuntimeErrorType.TypeMismatchError => "!TYPE MISMATCH",
			RuntimeErrorType.WrongNumberOfArgumentsError => "!WRONG NUMBER OF ARGUMENTS",
			RuntimeErrorType.InvalidArgumentError => "!INVALID ARGUMENT",
			RuntimeErrorType.DivisionByZeroError => "!DIVISION BY ZERO",
			RuntimeErrorType.BadGotoTargetError => $"!BAD GOTO TARGET {AdditionalInfo}",
			RuntimeErrorType.BadGosubTargetError => $"!BAD GOSUB TARGET {AdditionalInfo}",
			RuntimeErrorType.BadRestoreTargetError => $"!BAD RESTORE TARGET {AdditionalInfo}",
			RuntimeErrorType.NegativeArrayDimError => "!NEGATIVE ARRAY DIM",
			RuntimeErrorType.ReDimensionedArrayError => "!REDIM'D ARRAY",
			RuntimeErrorType.MismatchedArrayDimensionsError => "!MISMATCHED ARRAY DIMENSIONS",
			RuntimeErrorType.OutOfArrayBoundsError => "!OUT OF ARRAY BOUNDS",
			RuntimeErrorType.UndefinedFunctionError => $"!UNDEFINED FUNCTION {AdditionalInfo}",
			RuntimeErrorType.OutOfDataError => "!OUT OF DATA",
			RuntimeErrorType.EndOfInputError => "!END OF INPUT",
			_ => "!UNKNOWN ERROR"
		};
	}
}