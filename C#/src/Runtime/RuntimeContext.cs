namespace VintageBasic.Runtime;

sealed record RuntimeContext(BasicStore Store, BasicState State)
{
	public VariableManager Variables { get; } = new(Store);
	public FunctionManager Functions { get; } = new(Store);
	public InputOutputManager IO { get; } = new(State);
	public RandomManager Random { get; } = new(State);
	public StateManager ProgramState { get; } = new(State);

	/// <summary>
	/// Converts a double to an int, similar to BASIC's INT function (floor).
	/// </summary>
	public static int FloatToInt(double val)
	{
		return (int)System.Math.Floor(val);
	}
}
