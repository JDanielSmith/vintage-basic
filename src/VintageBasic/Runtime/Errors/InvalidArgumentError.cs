// src/VintageBasic/Runtime/Errors/InvalidArgumentError.cs
namespace VintageBasic.Runtime.Errors
{
    public class InvalidArgumentError : BasicRuntimeException
    {
        public InvalidArgumentError(string message = "Invalid argument", int? lineNumber = null)
            : base(message, lineNumber) { }
    }
}
