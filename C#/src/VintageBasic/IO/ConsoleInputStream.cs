// src/VintageBasic/IO/ConsoleInputStream.cs
using System;
using System.IO;
using System.Threading.Tasks;
using VintageBasic.Runtime; // For IInputStream

namespace VintageBasic.IO;

public class ConsoleInputStream : IInputStream
{
    public string? ReadLine()
    {
        return Console.ReadLine();
    }

    public bool IsEOF()
    {
        // Standard Console.In doesn't have a direct IsEOF property like a file stream.
        // Peek() returns -1 if at EOF. This is the most reliable check for console.
        // However, Peek() can block if input is redirected from a source that blocks.
        // For typical interactive console, it's non-blocking for EOF check.
        // This might not be perfect for all redirected input scenarios but is common for console.
        try
        {
            return Console.In.Peek() == -1;
        }
        catch (IOException) 
        {
            // This can happen if the stream is closed or not available (e.g. during some test runners)
            return true; // Assume EOF if Peek throws IOException
        }
    }
}
