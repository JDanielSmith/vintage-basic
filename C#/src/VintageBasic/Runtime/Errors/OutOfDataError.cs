// src/VintageBasic/Runtime/Errors/OutOfDataError.cs
namespace VintageBasic.Runtime.Errors
{
    public class OutOfDataError : BasicRuntimeException
    {
        public OutOfDataError(string message = "Out of data", int? lineNumber = null)
            : base(message, lineNumber) { }
    }
}
