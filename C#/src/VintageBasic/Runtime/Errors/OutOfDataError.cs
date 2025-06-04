namespace VintageBasic.Runtime.Errors;
sealed class OutOfDataError : BasicRuntimeException
{
    public OutOfDataError(string message = "Out of data", int? lineNumber = null)
        : base(message, lineNumber) { }
}
