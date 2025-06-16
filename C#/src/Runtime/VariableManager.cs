using System.Collections.Generic;
using VintageBasic.Runtime.Errors;
using VintageBasic.Syntax;

namespace VintageBasic.Runtime;

sealed class VariableManager(BasicStore store)
{
	readonly BasicStore _store = store;

	public object GetScalarValue(VarName varName)
	{
		return _store.ScalarVariables.TryGetValue(varName, out var value) ? value : Var.GetDefaultValue(varName.Type);
	}

	public void SetScalarValue(VarName varName, object value)
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

	static readonly IReadOnlyList<int> DefaultDimensionBounds = [11]; // For 0-10 elements
	BasicArray GetOrDimArray(VarName varName, IReadOnlyList<int> indices)
	{
		var numDimensions = indices.Count;
		if (_store.ArrayVariables.TryGetValue(varName, out var retval))
		{
			return retval.Dimensions == numDimensions ? retval :
				throw new MismatchedArrayDimensionsError($"Array {varName} expects {retval.Dimensions} dimensions, but received {numDimensions}.");
		}

		// Implicitly dimension with default bounds if accessed before DIM
		var defaultUpperBounds = Enumerable.Repeat(DefaultDimensionBounds[0] - 1, numDimensions);
		return DimArray_(varName, defaultUpperBounds);
	}

	public object GetArrayValue(VarName varName, IReadOnlyList<int> indices)
	{
		var array = GetOrDimArray(varName, indices);
		return array.GetValue(indices, varName.ToString());
	}

	public void SetArrayValue(VarName varName, IReadOnlyList<int> indices, object value)
	{
		var array = GetOrDimArray(varName, indices);
		array.SetValue(indices, value, varName.ToString());
	}
}
