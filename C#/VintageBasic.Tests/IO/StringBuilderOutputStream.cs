// VintageBasic.Tests/IO/StringBuilderOutputStream.cs
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using VintageBasic.Runtime;

namespace VintageBasic.Tests.IO
{
    public class StringBuilderOutputStream : IOutputStream
    {
        private readonly StringBuilder _stringBuilder;

        public StringBuilderOutputStream()
        {
            _stringBuilder = new StringBuilder();
        }

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
}
