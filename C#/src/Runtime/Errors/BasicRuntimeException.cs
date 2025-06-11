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
