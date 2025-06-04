namespace VintageBasic.Runtime.Errors;
sealed class DivisionByZeroError : BasicRuntimeException
{
    public DivisionByZeroError(string message = "Division by zero", int? lineNumber = null)
        : base(message, lineNumber) { }
}
