namespace VintageBasic.Runtime.Errors;
sealed class DivisionByZeroError : BasicRuntimeException
{
	public DivisionByZeroError() : this("Division by zero") { }
	public DivisionByZeroError(string message) : base(message) { }
	public DivisionByZeroError(string message, Exception innerException) : base(message, innerException) { }
	public DivisionByZeroError(int lineNumber) : base("Division by zero", lineNumber) { }
}
