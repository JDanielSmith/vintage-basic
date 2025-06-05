using System.Text;
using VintageBasic.Runtime;

namespace VintageBasic.Tests.IO;
sealed class StringBuilderOutputStream : IOutputStream
{
    readonly StringBuilder _stringBuilder = new();

    public void WriteString(string text)
    {
        _stringBuilder.Append(text);
    }

    public void WriteLine(string text)
    {
        _stringBuilder.AppendLine(text);
    }

    public void WriteChar(char c)
    {
        _stringBuilder.Append(c);
    }

    public void Flush()
    {
        // No-op for StringBuilder
    }

    public string GetOutput()
    {
        return _stringBuilder.ToString();
    }

    public void Clear()
    {
        _stringBuilder.Clear();
    }
}
