using VintageBasic.Runtime.Errors;
using VintageBasic.Syntax;

namespace VintageBasic.Runtime;

sealed class VariableManager(BasicStore store)
{
	readonly BasicStore _store = store;
	public static readonly IReadOnlyList<int> DefaultDimensionBounds = [11]; // For 0-10 elements

	static bool KeyMatchesVarName(VarName key, VarName varName)
	{
		return varName.EqualsName(key) && (varName.Val.GetType() == key.Val.GetType());
	}

	public object GetScalarVar(VarName varName)
	{
		foreach (var key in _store.ScalarVariables.Keys)
		{
			if (KeyMatchesVarName(key, varName))
			{
				return _store.ScalarVariables[key];
			}
		}
		return varName.GetDefaultValue();
	}

	public void SetScalarVar(VarName varName, object value)
	{
		foreach (var key in _store.ScalarVariables.Keys)
		{
			if (KeyMatchesVarName(key, varName))
			{
				_store.ScalarVariables[key] = value;
				return;
			}
		}
		_store.ScalarVariables[varName] = value;
	}

	public BasicArray DimArray(VarName varName, IReadOnlyList<int> dimensionUpperBounds)
	{
		foreach (var key in _store.ArrayVariables.Keys)
		{
			if (key.EqualsName(varName))
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
		Array.Fill(newArray.Data, varName.GetDefaultValue());

		_store.ArrayVariables[varName] = newArray;
		return newArray;
	}

	BasicArray GetOrDimArray(VarName varName, int numDimensions)
	{
		foreach (var key in _store.ArrayVariables.Keys)
		{
			if (key.EqualsName(varName))
			{
				return _store.ArrayVariables[key];
			}
		}

		// Implicitly dimension with default bounds if accessed before DIM
		var defaultUpperBounds = Enumerable.Repeat(DefaultDimensionBounds[0] - 1, numDimensions).ToList();
		return DimArray(varName, defaultUpperBounds);
	}


	public object GetArrayVar(VarName varName, IReadOnlyList<int> indices)
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

	public void SetArrayVar(VarName varName, IReadOnlyList<int> indices, Object value)
	{
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
