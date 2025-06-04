namespace VintageBasic.Runtime.Errors;

sealed class InvalidArgumentError : BasicRuntimeException
{
    public InvalidArgumentError(string message = "Invalid argument", int? lineNumber = null)
        : base(message, lineNumber) { }
}
