// src/VintageBasic/Runtime/Errors/NegativeArrayDimError.cs
namespace VintageBasic.Runtime.Errors
{
    public class NegativeArrayDimError : BasicRuntimeException
    {
        public NegativeArrayDimError(string message = "Negative array dimension", int? lineNumber = null)
            : base(message, lineNumber) { }
    }
}
