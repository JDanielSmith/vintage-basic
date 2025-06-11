namespace VintageBasic.Runtime.Errors;

sealed class TypeMismatchError : BasicRuntimeException
{
	public TypeMismatchError() : this("Type mismatch") { }
	public TypeMismatchError(string message) : base(message) { }
	public TypeMismatchError(string message, Exception innerException) : base(message, innerException) { }
	public TypeMismatchError(int lineNumber) : this("Type mismatch", lineNumber) { }
	public TypeMismatchError(string message, int? lineNumber) : base(message, lineNumber) { }
}
