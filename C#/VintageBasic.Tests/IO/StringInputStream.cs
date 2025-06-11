using VintageBasic.Runtime;

namespace VintageBasic.Tests.IO;
sealed class StringInputStream : IInputStream
{
	readonly Queue<string> _lines;
	bool _eofReached;

	public StringInputStream(IEnumerable<string> lines)
	{
		_lines = new(lines ?? Enumerable.Empty<string>());
	}

	public StringInputStream(string singleLine)
	{
		_lines = new();
		if (singleLine is not null)
		{
			_lines.Enqueue(singleLine);
		}
	}

	public static StringInputStream FromStringWithNewlines(string input)
	{
		if (String.IsNullOrEmpty(input))
		{
			return new(Enumerable.Empty<string>());
		}
		return new(input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));
	}


	public string? ReadLine()
	{
		if (_lines.Count > 0)
		{
			return _lines.Dequeue();
		}
		_eofReached = true;
		return null;
	}

	public bool IsEof
	{
		get
		{
			// This simple version considers EOF if the queue is empty.
			// More sophisticated checks might be needed if interactions with ReadLineAsync
			// can change the EOF state in a more complex way (e.g. if ReadLineAsync could block
			// and wait for more input to be added to the queue later, which is not the case here).
			if (_lines.Count == 0) _eofReached = true;
			return _eofReached;
		}
	}

	// Helper to simulate adding more input, useful for some test scenarios
	public void AddLine(string line)
	{
		_lines.Enqueue(line);
		_eofReached = false; // If we add lines, we are no longer at EOF
	}
}
