// src/VintageBasic/Runtime/RuntimeContext.cs
namespace VintageBasic.Runtime;

sealed class RuntimeContext
{
    public BasicStore Store { get; }
    public BasicState State { get; }

    public VariableManager Variables { get; }
    public FunctionManager Functions { get; }
    public InputOutputManager IO { get; }
    public RandomManager Random { get; }
    public StateManager ProgramState { get; }


    public RuntimeContext(BasicStore store, BasicState state)
    {
        Store = store ?? throw new System.ArgumentNullException(nameof(store));
        State = state ?? throw new System.ArgumentNullException(nameof(state));

        Variables = new VariableManager(store);
        Functions = new FunctionManager(store);
        IO = new InputOutputManager(state);
        Random = new RandomManager(state);
        ProgramState = new StateManager(state);
    }

    /// <summary>
    /// Converts a double to an int, similar to BASIC's INT function (floor).
    /// </summary>
    public static int FloatToInt(double val)
    {
        return (int)System.Math.Floor(val);
    }

    /// <summary>
    /// Converts a float to an int, similar to BASIC's INT function (floor).
    /// </summary>
    public static int FloatToInt(float val)
    {
        return (int)System.Math.Floor(val);
    }
}
