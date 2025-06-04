namespace VintageBasic.Runtime.Errors;

sealed class WrongNumberOfArgumentsError : BasicRuntimeException
{
    public WrongNumberOfArgumentsError(string message = "Wrong number of arguments", int? lineNumber = null)
        : base(message, lineNumber) { }
}
