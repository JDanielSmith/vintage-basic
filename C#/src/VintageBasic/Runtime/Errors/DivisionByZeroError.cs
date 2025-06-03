// src/VintageBasic/Runtime/Errors/DivisionByZeroError.cs
namespace VintageBasic.Runtime.Errors
{
    public class DivisionByZeroError : BasicRuntimeException
    {
        public DivisionByZeroError(string message = "Division by zero", int? lineNumber = null)
            : base(message, lineNumber) { }
    }
}
