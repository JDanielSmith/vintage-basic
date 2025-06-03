// src/VintageBasic/Runtime/Interfaces.cs
using System.Threading.Tasks;

namespace VintageBasic.Runtime;

public interface IInputStream
{
    string? ReadLine(); // Nullable for EOF
    bool IsEOF();
    // Potentially other methods like ReadCharAsync, etc.
}

public interface IOutputStream
{
    void WriteString(string text);
	void WriteLine(string text); // Convenience for WriteStringAsync + newline
	void WriteChar(char c);
	void Flush();
    // Potentially other methods like SetCursorPositionAsync, etc.
}
