using VintageBasic.Runtime.Errors;
using System.Collections.Immutable;

namespace VintageBasic.Runtime;

sealed class InputOutputManager(BasicState state)
{
	readonly BasicState _state = state;
	ImmutableList<string> _dataQueue = [];
	int _dataReadPointer;
	public const int ZoneWidth = 14; // As defined in BasicMonad.hs

	public void SetDataStrings(IReadOnlyList<string> allDataStrings)
	{
		_dataQueue = [.. allDataStrings ?? []];
		_dataReadPointer = 0; // Reset pointer when new data is set
	}

	static int CalculateEndColumn(int startColumn, string text)
	{
		int currentColumn = startColumn;
		foreach (char c in text)
		{
			if (c is '\n' or '\r')
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
		_state.OutputStream.WriteString(text);
		_state.OutputColumn = CalculateEndColumn(_state.OutputColumn, text);
		_state.OutputStream.Flush();
	}

	public string ReadLine()
	{
		_state.OutputStream.Flush(); // Ensure any pending output (like a prompt) is written.
		if (_state.InputStream.IsEof)
		{
			throw new EndOfInputError(_state.CurrentLineNumber);
		}

		var line = _state.InputStream.ReadLine() ?? throw new EndOfInputError(_state.CurrentLineNumber);
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
		if (specificLineData is not null)
		{
			// This interpretation of RESTORE <label> means use ONLY that line's data.
			// More commonly, it means start reading from that line onwards from the global data pool.
			// The Haskell version's `dataLookup jumpTable lab` suggests it gets data *starting* from that line.
			// For now, if specificLineData is provided, we use it exclusively.
			// This might need refinement to match standard BASIC behavior for RESTORE <label>.
			_dataQueue = [.. specificLineData];
		}
		// If specificLineData is null, RESTORE resets to the beginning of the *full* data set initially provided.
		// This requires that SetDataStrings was called with the full program data.
		// The _state.RestoreDataPointer() is not used here anymore for managing the read index
		// as _dataReadPointer in this class now handles it.
		_dataReadPointer = 0;
	}

	public int OutputColumn => _state.OutputColumn;
}
