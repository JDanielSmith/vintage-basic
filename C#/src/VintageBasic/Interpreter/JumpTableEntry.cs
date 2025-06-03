// src/VintageBasic/Interpreter/JumpTableEntry.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VintageBasic.Interpreter;

public class JumpTableEntry
{
    public int Label { get; }
    public Action ProgramAction { get; }
    public IReadOnlyList<string> Data { get; } // Data strings from DATA statements on this line

    public JumpTableEntry(int label, Action programAction, IReadOnlyList<string> data)
    {
        Label = label;
        ProgramAction = programAction ?? throw new ArgumentNullException(nameof(programAction));
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }
}
