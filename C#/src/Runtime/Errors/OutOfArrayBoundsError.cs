namespace VintageBasic.Runtime.Errors;

sealed class OutOfArrayBoundsError : BasicRuntimeException
{
    public OutOfArrayBoundsError(string message = "Out of array bounds", int? lineNumber = null)
        : base(message, lineNumber) { }
}
