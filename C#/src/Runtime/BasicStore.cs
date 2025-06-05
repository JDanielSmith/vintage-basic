using VintageBasic.Runtime.Errors;
using VintageBasic.Syntax; // For VarName

namespace VintageBasic.Runtime;

// Represents a user-defined function (DEF FN)
// Takes a list of arguments and returns a Val.
delegate Val UserDefinedFunction(IReadOnlyList<Val> arguments);

// Represents a BASIC array.
sealed class BasicArray
{
    // Stores the size of each dimension. E.g., for DIM A(10,5), this would be [11, 6] (0-indexed).
    public IReadOnlyList<int> DimensionSizes { get; }
    
    // Stores the actual array data. For multi-dimensional arrays, this will be a flattened 1D array.
    // Access will need to be managed by calculating the correct index from multi-dimensional indices.
    public Val[] Data { get; }

    public BasicArray(IReadOnlyList<int> dimensionSizes)
    {
        DimensionSizes = [.. dimensionSizes]; // Make a copy

        long totalSize = 1;
        foreach (int size in dimensionSizes)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(dimensionSizes), "Dimension size must be positive.");
            totalSize *= size;
        }

        Data = new Val[(int)totalSize];

        // Initialize array elements to default values based on type (implicitly handled by Val subclasses if needed,
        // but BASIC often initializes numeric arrays to 0 and string arrays to empty strings).
        // For now, they will be null, which needs to be handled during access.
        // Or, we could initialize them here if we knew the type, but arrays in BASIC can be untyped until first assignment.
        // Let's assume they are initialized to a default Val of a certain type if we can determine it,
        // or handle nulls during access. For now, they are just null.
    }

    // Helper to calculate the flat index from multi-dimensional indices.
    // Indices are typically 0-based in the C# representation.
    public int GetFlatIndex(IReadOnlyList<int> indices)
    {
        if (indices.Count != DimensionSizes.Count)
            throw new System.ArgumentException("Incorrect number of dimensions.", nameof(indices));

        int flatIndex = 0;
        int multiplier = 1;
        for (int i = 0; i < DimensionSizes.Count; i++)
        {
            if (indices[i] < 0 || indices[i] >= DimensionSizes[i])
                throw new OutOfArrayBoundsError($"Index {indices[i]} is out of range for dimension {i} (size {DimensionSizes[i]}).");
            
            flatIndex += indices[i] * multiplier;
            multiplier *= DimensionSizes[i];
        }
        return flatIndex;
    }

    public Val GetValue(IReadOnlyList<int> indices)
    {
        int index = GetFlatIndex(indices);
        // TODO: Consider default value if Data[index] is null (e.g., based on VarName type)
        return Data[index]; 
    }

    public void SetValue(IReadOnlyList<int> indices, Val value)
    {
        int index = GetFlatIndex(indices);
        // TODO: Type checking based on VarName type might be needed here.
        Data[index] = value;
    }
}

sealed class BasicStore()
{
    // Scalar variables: VarName -> Val
    // Using Val directly instead of IORef Val, as C# objects are reference types.
    // Direct mutation of Val objects (if they were mutable) or replacing them in the dictionary.
    public Dictionary<VarName, Val> ScalarVariables { get; } = new();

    // Array variables: VarName -> BasicArray
    public Dictionary<VarName, BasicArray> ArrayVariables { get; } = new();

	// User-defined functions: VarName -> UserDefinedFunction
	public Dictionary<VarName, UserDefinedFunction> UserFunctions { get; } = new();
}
