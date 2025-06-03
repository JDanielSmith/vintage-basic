// src/VintageBasic/IO/ConsoleOutputStream.cs
using System;
using System.IO;
using System.Threading.Tasks;
using VintageBasic.Runtime; // For IOutputStream

namespace VintageBasic.IO;

public class ConsoleOutputStream : IOutputStream
{
    // Console.Out is a TextWriter.

    public void WriteString(string text)
    {
        Console.Write(text);
    }

    public void WriteLine(string text)
    {
        Console.WriteLine(text);
    }

    public void WriteChar(char c)
    {
        Console.Write(c);
    }

    public void Flush()
    {
        // Console.Out typically flushes automatically for WriteLine,
        // but explicit Flush can be called if needed (though often a no-op for console).
        Console.Out.Flush();
    }
}
