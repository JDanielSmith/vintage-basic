namespace VintageBasic.Runtime;

sealed class BasicState(IInputStream inputStream, IOutputStream outputStream, int initialSeed = 0)
{
	public IInputStream InputStream { get; set; } = inputStream;
	public IOutputStream OutputStream { get; set; } = outputStream;

	public int CurrentLineNumber { get; set; }
	public int OutputColumn { get; set; }
	public double PreviousRandomValue { get; set; }
	public Random RandomGenerator { get; set; } = (initialSeed == 0) ? new() : new(initialSeed);

	public Stack<int> GosubReturnStack { get; } = new();
	public Stack<Interpreter.ForLoopContext> ForLoopStack { get; } = new();
}
