namespace VintageBasic.Runtime.Errors;

sealed class EndOfInputError : BasicRuntimeException
{
    public EndOfInputError(string message = "End of input", int? lineNumber = null)
        : base(message, lineNumber) { }
}
