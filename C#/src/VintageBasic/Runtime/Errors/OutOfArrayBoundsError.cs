// src/VintageBasic/Runtime/Errors/OutOfArrayBoundsError.cs
namespace VintageBasic.Runtime.Errors
{
    public class OutOfArrayBoundsError : BasicRuntimeException
    {
        public OutOfArrayBoundsError(string message = "Out of array bounds", int? lineNumber = null)
            : base(message, lineNumber) { }
    }
}
