// src/VintageBasic/Runtime/Errors/WrongNumberOfArgumentsError.cs
namespace VintageBasic.Runtime.Errors
{
    public class WrongNumberOfArgumentsError : BasicRuntimeException
    {
        public WrongNumberOfArgumentsError(string message = "Wrong number of arguments", int? lineNumber = null)
            : base(message, lineNumber) { }
    }
}
