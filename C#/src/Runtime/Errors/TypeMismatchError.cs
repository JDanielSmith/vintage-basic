namespace VintageBasic.Runtime.Errors;

sealed class TypeMismatchError : BasicRuntimeException
{
    public TypeMismatchError(string message = "Type mismatch", int? lineNumber = null) 
        : base(message, lineNumber) { }
}
