namespace VintageBasic.Runtime.Errors;
sealed class NegativeArrayDimError : BasicRuntimeException
{
    public NegativeArrayDimError(string message = "Negative array dimension", int? lineNumber = null)
        : base(message, lineNumber) { }
}
