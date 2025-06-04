namespace VintageBasic.Runtime.Errors;

sealed class RedimensionedArrayError : BasicRuntimeException
{
    public RedimensionedArrayError(string message = "Re-dimensioned array", int? lineNumber = null)
        : base(message, lineNumber) { }
}
