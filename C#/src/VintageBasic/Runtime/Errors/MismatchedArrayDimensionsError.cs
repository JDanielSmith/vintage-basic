namespace VintageBasic.Runtime.Errors;

sealed class MismatchedArrayDimensionsError : BasicRuntimeException
{
    public MismatchedArrayDimensionsError(string message = "Mismatched array dimensions", int? lineNumber = null)
        : base(message, lineNumber) { }
}
