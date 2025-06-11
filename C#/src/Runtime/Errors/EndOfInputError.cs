namespace VintageBasic.Runtime.Errors;

sealed class EndOfInputError : BasicRuntimeException
{
	public EndOfInputError() : this("End of input") { }
	public EndOfInputError(string message) : base(message) { }
	public EndOfInputError(string message, Exception innerException) : base(message, innerException) { }
	public EndOfInputError(string message, int lineNumber) : base(message, lineNumber) { }
	public EndOfInputError(int lineNumber) : this("End of input", lineNumber) { }
}
