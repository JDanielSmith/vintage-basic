namespace VintageBasic.Runtime;

sealed record RuntimeContext(BasicStore Store, BasicState State)
{
	public VariableManager Variables { get; } = new(Store);
	public InputOutputManager IO { get; } = new(State);
	public RandomManager Random { get; } = new(State);
	public StateManager ProgramState { get; } = new(State);
}
