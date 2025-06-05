using VintageBasic.Syntax; // For SourcePosition

namespace VintageBasic.Parsing.Errors;

sealed class ParseException : Exception
{
    public SourcePosition? Position { get; }

	public ParseException() { }

	public ParseException(string message) : base(message) { }
	public ParseException(string message, SourcePosition? position)
		: this(FormatMessage(message, position))
	{
		Position = position;
	}

	public ParseException(string message, Exception innerException) : base(message, innerException) { 	}
    public ParseException(string message, Exception innerException, SourcePosition? position) 
        : this(FormatMessage(message, position), innerException)
    {
        Position = position;
    }

    static string FormatMessage(string message, SourcePosition? position)
    {
        if (position.HasValue)
        {
            return $"Parse error at {position.Value}: {message}";
        }
        return $"Parse error: {message}";
    }
}
