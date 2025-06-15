using VintageBasic.Runtime.Errors;
using VintageBasic.Syntax;

namespace VintageBasic.Runtime;

sealed class VariableManager(BasicStore store)
{
	readonly BasicStore _store = store;
	static readonly IReadOnlyList<int> DefaultDimensionBounds = [11]; // For 0-10 elements

	public object GetScalarVar(VarName varName)
	{
		return _store.ScalarVariables.TryGetValue(varName, out var value) ? value : Var.GetDefaultValue(varName.Type);
	}

	public void SetScalarVar(VarName varName, object value)
	{
		_store.ScalarVariables[varName] = value;
	}

	BasicArray DimArray_(VarName varName, IEnumerable<int> dimensionUpperBounds)
	{
		// Convert BASIC upper bounds (e.g., 10 in DIM A(10)) to array dimension sizes (e.g., 11 for 0-10).
		var dimensionSizes = dimensionUpperBounds.Select(b => b + 1);
		_store.ArrayVariables[varName] = new(dimensionSizes, varName.Type);
		return _store.ArrayVariables[varName];
	}
	public BasicArray DimArray(VarName varName, IEnumerable<int> dimensionUpperBounds) =>
		!_store.ArrayVariables.ContainsKey(varName) ? DimArray_(varName, dimensionUpperBounds) : throw new RedimensionedArrayError($"Array {varName} already dimensioned.");

	BasicArray GetOrDimArray(VarName varName, int numDimensions)
	{
		if (!_store.ArrayVariables.TryGetValue(varName, out var retval))
		{
			// Implicitly dimension with default bounds if accessed before DIM
			var defaultUpperBounds = Enumerable.Repeat(DefaultDimensionBounds[0] - 1, numDimensions);
			retval = DimArray_(varName, defaultUpperBounds);
		}
		else if (retval.Dimensions != numDimensions)
		{
			throw new MismatchedArrayDimensionsError($"Array {varName} expects {retval.Dimensions} dimensions, but received {numDimensions}.");
		}
		return retval;
	}

	public object GetArrayVar(VarName varName, IReadOnlyList<int> indices)
	{
		var array = GetOrDimArray(varName, indices.Count);
		return array.GetValue(indices, varName.ToString());
	}

	public void SetArrayVar(VarName varName, IReadOnlyList<int> indices, Object value)
	{
		var array = GetOrDimArray(varName, indices.Count);
		array.SetValue(indices, value, varName.ToString());
	}
}
