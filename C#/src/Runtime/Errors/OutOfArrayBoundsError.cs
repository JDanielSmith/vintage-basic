namespace VintageBasic.Runtime.Errors;

sealed class OutOfArrayBoundsError : BasicRuntimeException
{
	public OutOfArrayBoundsError() : this("Out of array bounds") { }
	public OutOfArrayBoundsError(string message, int? lineNumber) : base(message, lineNumber) { }
	public OutOfArrayBoundsError(string message) : base(message) { }
	public OutOfArrayBoundsError(string message, Exception innerException) : base(message, innerException) { }
}
