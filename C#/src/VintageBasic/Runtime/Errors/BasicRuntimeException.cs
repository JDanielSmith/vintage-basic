using System;

namespace VintageBasic.Runtime.Errors;
class BasicRuntimeException : Exception
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

    static string AppendLinenumber(string message, int? lineNumber)
    {
        if (lineNumber.HasValue)
        {
            return $"{message} at line {lineNumber.Value}";
        }
        return message;
    }
}
