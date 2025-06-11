namespace VintageBasic.Runtime.Errors;

sealed class WrongNumberOfArgumentsError : BasicRuntimeException
{
	public WrongNumberOfArgumentsError() : this("Wrong number of arguments") { }
	public WrongNumberOfArgumentsError(string message) : base(message) { }
	public WrongNumberOfArgumentsError(string message, Exception innerException) : base(message, innerException) { }
	public WrongNumberOfArgumentsError(string message, int lineNumber) : base(message, lineNumber) { }
}