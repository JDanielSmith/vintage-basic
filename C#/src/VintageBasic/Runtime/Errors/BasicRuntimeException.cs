// src/VintageBasic/Runtime/Errors/BasicRuntimeException.cs
using System;

namespace VintageBasic.Runtime.Errors
{
    public class BasicRuntimeException : Exception
    {
        public int? LineNumber { get; }

        public BasicRuntimeException(string message, int? lineNumber = null) : base(AppendLinenumber(message, lineNumber))
        {
            LineNumber = lineNumber;
        }

        public BasicRuntimeException(string message, Exception innerException, int? lineNumber = null) : base(AppendLinenumber(message, lineNumber), innerException)
        {
            LineNumber = lineNumber;
        }

        private static string AppendLinenumber(string message, int? lineNumber)
        {
            if (lineNumber.HasValue)
            {
                return $"{message} at line {lineNumber.Value}";
            }
            return message;
        }
    }
}
