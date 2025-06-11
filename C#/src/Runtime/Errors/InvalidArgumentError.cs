namespace VintageBasic.Runtime.Errors;

sealed class InvalidArgumentError : BasicRuntimeException
{
	public InvalidArgumentError() : this("Invalid argument") { }
	public InvalidArgumentError(string message) : base(message) { }
	public InvalidArgumentError(string message, Exception innerException) : base(message, innerException) { }
	public InvalidArgumentError(string message, int lineNumber) : base(message, lineNumber) { }
}
