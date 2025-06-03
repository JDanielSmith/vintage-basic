// src/VintageBasic/Runtime/InputOutputManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VintageBasic.Runtime.Errors;

namespace VintageBasic.Runtime;

public class InputOutputManager
{
    private readonly BasicState _state;
    private List<string> _dataQueue = new List<string>();
    private int _dataReadPointer = 0;
    public const int ZoneWidth = 14; // As defined in BasicMonad.hs

    public InputOutputManager(BasicState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        // Initialize _dataQueue from BasicState if it has initial data.
        // However, BasicState's allDataStatements is private and readonly.
        // For now, _dataQueue starts empty and is populated by SetDataStrings.
    }

    public void SetDataStrings(IReadOnlyList<string> allDataStrings)
    {
        _dataQueue = new List<string>(allDataStrings ?? new List<string>());
        _dataReadPointer = 0; // Reset pointer when new data is set
    }

    private int CalculateEndColumn(int startColumn, string text)
    {
        int currentColumn = startColumn;
        foreach (char c in text)
        {
            if (c == '\n' || c == '\r')
            {
                currentColumn = 0;
            }
            else
            {
                currentColumn++;
            }
        }
        return currentColumn;
    }

    public void PrintString(string text)
    {
        if (text == null) return;

        _state.OutputStream.WriteString(text);
        _state.OutputColumn = CalculateEndColumn(_state.OutputColumn, text);
        _state.OutputStream.Flush();
    }

    public string ReadLine()
    {
        _state.OutputStream.Flush(); // Ensure any pending output (like a prompt) is written.
        
        if ( _state.InputStream.IsEOF())
        {
            throw new EndOfInputError(lineNumber: _state.CurrentLineNumber);
        }

        string? line = _state.InputStream.ReadLine();
        if (line == null) // Should be caught by IsEOFAsync, but as a safeguard.
        {
            throw new EndOfInputError(lineNumber: _state.CurrentLineNumber);
        }
        
        _state.OutputColumn = 0; // Reading input resets the column.
        return line;
    }

    public string ReadData()
    {
        if (_dataReadPointer >= _dataQueue.Count)
        {
            throw new OutOfDataError(lineNumber: _state.CurrentLineNumber);
        }
        return _dataQueue[_dataReadPointer++];
    }

    public void RestoreData(IReadOnlyList<string>? specificLineData = null)
    {
        if (specificLineData != null)
        {
            // This interpretation of RESTORE <label> means use ONLY that line's data.
            // More commonly, it means start reading from that line onwards from the global data pool.
            // The Haskell version's `dataLookup jumpTable lab` suggests it gets data *starting* from that line.
            // For now, if specificLineData is provided, we use it exclusively.
            // This might need refinement to match standard BASIC behavior for RESTORE <label>.
            _dataQueue = new List<string>(specificLineData);
        }
        // If specificLineData is null, RESTORE resets to the beginning of the *full* data set initially provided.
        // This requires that SetDataStrings was called with the full program data.
        // The _state.RestoreDataPointer() is not used here anymore for managing the read index
        // as _dataReadPointer in this class now handles it.
        _dataReadPointer = 0;
    }

    public int GetOutputColumn()
    {
        return _state.OutputColumn;
    }
}
