namespace VintageBasic.Runtime.Errors;

sealed class MismatchedArrayDimensionsError : BasicRuntimeException
{
	public MismatchedArrayDimensionsError() : this("Mismatched array dimensions") { }
	public MismatchedArrayDimensionsError(string message) : base(message) { }
	public MismatchedArrayDimensionsError(string message, Exception innerException) : base(message, innerException) { }
}
