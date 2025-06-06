namespace VintageBasic.Runtime;

sealed class RuntimeContext(BasicStore store, BasicState state)
{
    public BasicStore Store { get; } = store;
    public BasicState State { get; } = state;

	public VariableManager Variables { get; } = new(store);
	public FunctionManager Functions { get; } = new(store);
	public InputOutputManager IO { get; } = new(state);
	public RandomManager Random { get; } = new(state);
	public StateManager ProgramState { get; } = new(state);

    /// <summary>
    /// Converts a double to an int, similar to BASIC's INT function (floor).
    /// </summary>
    public static int FloatToInt(double val)
    {
        return (int)System.Math.Floor(val);
    }
}
