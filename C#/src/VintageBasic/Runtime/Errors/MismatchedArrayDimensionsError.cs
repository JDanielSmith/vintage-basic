// src/VintageBasic/Runtime/Errors/MismatchedArrayDimensionsError.cs
namespace VintageBasic.Runtime.Errors
{
    public class MismatchedArrayDimensionsError : BasicRuntimeException
    {
        public MismatchedArrayDimensionsError(string message = "Mismatched array dimensions", int? lineNumber = null)
            : base(message, lineNumber) { }
    }
}
