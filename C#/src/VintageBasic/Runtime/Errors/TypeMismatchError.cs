// src/VintageBasic/Runtime/Errors/TypeMismatchError.cs
namespace VintageBasic.Runtime.Errors
{
    public class TypeMismatchError : BasicRuntimeException
    {
        public TypeMismatchError(string message = "Type mismatch", int? lineNumber = null) 
            : base(message, lineNumber) { }
    }
}
