namespace VintageBasic.Runtime.Errors;

sealed class RedimensionedArrayError : BasicRuntimeException
{
	public RedimensionedArrayError() : this("Re-dimensioned array") { }
	public RedimensionedArrayError(string message) : base(message) { }
	public RedimensionedArrayError(string message, Exception innerException) : base(message, innerException) { }
}
