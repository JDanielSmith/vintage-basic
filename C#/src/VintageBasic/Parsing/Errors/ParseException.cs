// src/VintageBasic/Parsing/Errors/ParseException.cs
using System;
using VintageBasic.Syntax; // For SourcePosition

namespace VintageBasic.Parsing.Errors;

sealed class ParseException : Exception
{
    public SourcePosition? Position { get; }

    public ParseException(string message, SourcePosition? position = null) 
        : base(FormatMessage(message, position))
    {
        Position = position;
    }

    public ParseException(string message, Exception innerException, SourcePosition? position = null) 
        : base(FormatMessage(message, position), innerException)
    {
        Position = position;
    }

    private static string FormatMessage(string message, SourcePosition? position)
    {
        if (position.HasValue)
        {
            return $"Parse error at {position.Value}: {message}";
        }
        return $"Parse error: {message}";
    }
}
