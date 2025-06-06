namespace VintageBasic.Runtime;

sealed class BasicState(IInputStream inputStream, IOutputStream outputStream, IReadOnlyList<string> initialDataStatements, int initialSeed = 0)
{
    public IInputStream InputStream { get; set; } = inputStream;
	public IOutputStream OutputStream { get; set; } = outputStream;

	public int CurrentLineNumber { get; set; } 
    public int OutputColumn { get; set; } 
    public double PreviousRandomValue { get; set; }
	public Random RandomGenerator { get; set; } = (initialSeed == 0) ? new() : new(initialSeed);
    
    // Stores all strings from DATA statements in the program, in order of appearance.
    readonly List<string> allDataStatements = [.. initialDataStatements ?? []]; // Store a copy

	// Pointer to the current DATA statement item to be read.
	public int DataReadPointer { get; private set; }

    public Stack<int> GosubReturnStack { get; } = new();
    public Stack<Interpreter.ForLoopContext> ForLoopStack { get; } = new();

    // Gets the next available data String. Returns null if no more data is available.
    public string? ReadNextData()
    {
        if (DataReadPointer < allDataStatements.Count)
        {
            return allDataStatements[DataReadPointer++];
        }
        return null;
    }
}
