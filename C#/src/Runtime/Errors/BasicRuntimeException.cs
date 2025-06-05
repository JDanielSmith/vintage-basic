using System;

namespace VintageBasic.Runtime.Errors;
class BasicRuntimeException : Exception
{
    public int? LineNumber { get; }

    public BasicRuntimeException() { }

	public BasicRuntimeException(string message, int? lineNumber) : base(AppendLinenumber(message, lineNumber))
    {
        LineNumber = lineNumber;
    }
	public BasicRuntimeException(string message) : this(message, (int?)null) { }

	public BasicRuntimeException(string message, Exception innerException, int? lineNumber) : base(AppendLinenumber(message, lineNumber), innerException)
    {
        LineNumber = lineNumber;
    }
	public BasicRuntimeException(string message, Exception innerException) : this(message, innerException, null) { }

	static string AppendLinenumber(string message, int? lineNumber)
    {
        if (lineNumber.HasValue)
        {
            return $"{message} at line {lineNumber.Value}";
        }
        return message;
    }
}
