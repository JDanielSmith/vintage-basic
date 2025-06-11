namespace VintageBasic.Runtime.Errors;
sealed class OutOfDataError : BasicRuntimeException
{
	public OutOfDataError() : this("Out of data") { }
	public OutOfDataError(string message) : base(message) { }
	public OutOfDataError(string message, Exception innerException) : base(message, innerException) { }
	public OutOfDataError(int lineNumber) : base("Out of data", lineNumber) { }
}
