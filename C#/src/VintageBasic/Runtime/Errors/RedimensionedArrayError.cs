// src/VintageBasic/Runtime/Errors/RedimensionedArrayError.cs
namespace VintageBasic.Runtime.Errors
{
    public class RedimensionedArrayError : BasicRuntimeException
    {
        public RedimensionedArrayError(string message = "Re-dimensioned array", int? lineNumber = null)
            : base(message, lineNumber) { }
    }
}
