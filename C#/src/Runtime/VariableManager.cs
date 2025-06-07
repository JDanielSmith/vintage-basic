using VintageBasic.Runtime.Errors;
using VintageBasic.Syntax;

namespace VintageBasic.Runtime;

sealed class VariableManager(BasicStore store)
{
    readonly BasicStore _store = store;
	public static readonly IReadOnlyList<int> DefaultDimensionBounds = [ 11 ]; // For 0-10 elements

    private static Val GetDefaultValue(ValType type)
    {
        if (type == ValType.FloatType) return new FloatVal(0f);
        if (type == ValType.IntType) return new IntVal(0);
        if (type == ValType.StringType) return new StringVal("");
        throw new ArgumentOutOfRangeException(nameof(type), "Invalid ValType for default value.");
    }

    static string GetVarName(VarName varName)
    {
        // Variables are 1) case-insensitive, and 2) unique in only the first two characters.
        return varName.Name[..Math.Min(2, varName.Name.Length)].ToUpperInvariant();
	}

	public Val GetScalarVar(VarName varName)
    {
        var name = GetVarName(varName);
		foreach (var key in _store.ScalarVariables.Keys)
        {
            if ((GetVarName(key) == name) && (key.Type == varName.Type))
            {
                return _store.ScalarVariables[key];
            }
		}
        return GetDefaultValue(varName.Type);
    }

    public void SetScalarVar(VarName varName, Val value)
    {
        if (varName.Type != value.Type)
        {
            // This check might be too strict for BASIC, which often allows implicit conversion.
            // However, for a strongly-typed internal representation, it's safer.
            // Consider if type conversion logic should be here or in the expression evaluation.
            // For now, strict type matching for direct assignment.
            // throw new TypeMismatchError($"Cannot assign {value.Type} to scalar variable {varName} of type {varName.Type}");
        }

		var name = GetVarName(varName);
		foreach (var key in _store.ScalarVariables.Keys)
		{
			if ((GetVarName(key) == name) && (key.Type == varName.Type))
			{
				_store.ScalarVariables[key] = value;
                return;
			}
		}
		_store.ScalarVariables[varName] = value;
    }

    public BasicArray DimArray(VarName varName, IReadOnlyList<int> dimensionUpperBounds)
    {
		var name = GetVarName(varName);
        foreach (var key in _store.ArrayVariables.Keys)
        {
            if (GetVarName(key) == name)
            {
                throw new RedimensionedArrayError($"Array {varName} already dimensioned.");
            }
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dimensionUpperBounds.Count, nameof(dimensionUpperBounds));

        foreach (var bound in dimensionUpperBounds)
        {
            if (bound < 0) // In BASIC, DIM A(-1) is invalid. Bounds are 0 to N. So size is N+1.
            {
                // The input `dimensionUpperBounds` are the N in DIM A(N). So the size is N+1.
                // If N itself is negative, it's an error.
                throw new NegativeArrayDimError($"Dimension upper bound cannot be negative for array {varName}.");
            }
        }
        
        // Convert BASIC upper bounds (e.g., 10 in DIM A(10)) to array dimension sizes (e.g., 11 for 0-10).
        var dimensionSizes = dimensionUpperBounds.Select(b => b + 1).ToList();
		BasicArray newArray = new(dimensionSizes);

        // Initialize array elements to default values.
        var defaultValue = GetDefaultValue(varName.Type);
		Array.Fill(newArray.Data, defaultValue);

        _store.ArrayVariables[varName] = newArray;
        return newArray;
    }
    
    BasicArray GetOrDimArray(VarName varName, int numDimensions)
    {
		var name = GetVarName(varName);
		foreach (var key in _store.ArrayVariables.Keys)
		{
			if (GetVarName(key) == name)
			{
				return _store.ArrayVariables[key];
			}
		}

        // Implicitly dimension with default bounds if accessed before DIM
        var defaultUpperBounds = Enumerable.Repeat(DefaultDimensionBounds[0] - 1, numDimensions).ToList();
        return DimArray(varName, defaultUpperBounds);
    }


    public Val GetArrayVar(VarName varName, IReadOnlyList<int> indices)
    {
        var array = GetOrDimArray(varName, indices.Count);
        if (array.DimensionSizes.Count != indices.Count)
        {
            throw new MismatchedArrayDimensionsError($"Array {varName} expects {array.DimensionSizes.Count} dimensions, but received {indices.Count}.");
        }
        
        // The BasicArray GetValue and SetValue methods already handle index calculation and bounds checking using 0-based indices.
        // The indices provided here are also 0-based as per typical C# collections.
        try
        {
            return array.GetValue(indices);
        }
        catch (IndexOutOfRangeException ex)
        {
            throw new OutOfArrayBoundsError($"Index out of bounds for array {varName}.", ex);
        }
    }

    public void SetArrayVar(VarName varName, IReadOnlyList<int> indices, Val value)
    {
        if (varName.Type != value.Type)
        {
            // Similar to SetScalarVar, consider type coercion strategy.
            // For now, strict.
            // throw new TypeMismatchError($"Cannot assign {value.Type} to array {varName} of type {varName.Type}");
        }

        var array = GetOrDimArray(varName, indices.Count);        
        if (array.DimensionSizes.Count != indices.Count)
        {
            throw new MismatchedArrayDimensionsError($"Array {varName} expects {array.DimensionSizes.Count} dimensions, but received {indices.Count}.");
        }

        // The BasicArray GetValue and SetValue methods already handle index calculation and bounds checking using 0-based indices.
        try
        {
             array.SetValue(indices, value);
        }
        catch (IndexOutOfRangeException ex)
        {
            throw new OutOfArrayBoundsError($"Index out of bounds for array {varName}.", ex);
        }
    }
}
