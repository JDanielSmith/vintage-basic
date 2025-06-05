namespace VintageBasic.Runtime;

interface IInputStream
{
    string? ReadLine(); // Nullable for EOF
	bool IsEof { get; }
	// Potentially other methods like ReadCharAsync, etc.
}

interface IOutputStream
{
    void WriteString(string text);
	void WriteLine(string text); // Convenience for WriteStringAsync + newline
	void WriteChar(char c);
	void Flush();
    // Potentially other methods like SetCursorPositionAsync, etc.
}
