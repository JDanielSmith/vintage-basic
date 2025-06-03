// src/VintageBasic/Runtime/Errors/EndOfInputError.cs
namespace VintageBasic.Runtime.Errors
{
    public class EndOfInputError : BasicRuntimeException
    {
        public EndOfInputError(string message = "End of input", int? lineNumber = null)
            : base(message, lineNumber) { }
    }
}
