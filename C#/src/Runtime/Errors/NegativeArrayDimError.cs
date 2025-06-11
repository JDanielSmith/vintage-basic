namespace VintageBasic.Runtime.Errors;
sealed class NegativeArrayDimError : BasicRuntimeException
{
	public NegativeArrayDimError() : this("Negative array dimension") { }
	public NegativeArrayDimError(string message) : base(message) { }
	public NegativeArrayDimError(string message, Exception innerException) : base(message, innerException) { }
}
