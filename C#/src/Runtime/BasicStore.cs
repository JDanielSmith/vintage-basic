using VintageBasic.Runtime.Errors;
using VintageBasic.Syntax;

namespace VintageBasic.Runtime;

sealed class BasicStore()
{	public Dictionary<VarName, object> ScalarVariables { get; } = [];
	public Dictionary<VarName, BasicArray> ArrayVariables { get; } = [];
}

// Represents a BASIC array.
sealed class BasicArray
{
	// Stores the size of each dimension. E.g., for DIM A(10,5), this would be [11, 6] (0-indexed).
	readonly IReadOnlyList<int> dimensionSizes_;

	public int Dimensions => dimensionSizes_.Count;

	// Stores the actual array data. For multi-dimensional arrays, this will be a flattened 1D array.
	// Access will need to be managed by calculating the correct index from multi-dimensional indices.
	readonly object[] data_;

	public BasicArray(IEnumerable<int> dimensionSizes, Type type)
	{
		var totalSize = dimensionSizes.Aggregate((long)1, (sum, size) =>
			sum += size > 0 ? size : throw new ArgumentOutOfRangeException(nameof(dimensionSizes), "Dimension size must be positive."));
		dimensionSizes_ = [.. dimensionSizes]; // Make a copy

		data_ = new object[(int)totalSize];
		Array.Fill(data_, Var.GetDefaultValue(type)); // Initialize array elements to default values.
	}

	// Helper to calculate the flat index from multi-dimensional indices.
	// Indices are typically 0-based in the C# representation.
	int GetFlatIndex(IReadOnlyList<int> indices)
	{
		if (indices.Count != Dimensions)
			throw new System.ArgumentException("Incorrect number of dimensions.", nameof(indices));

		int flatIndex = 0;
		int multiplier = 1;
		for (int i = 0; i < Dimensions; i++)
		{
			if (indices[i] < 0 || indices[i] >= dimensionSizes_[i])
				throw new OutOfArrayBoundsError($"Index {indices[i]} is out of range for dimension {i} (size {dimensionSizes_[i]}).");
			flatIndex += indices[i] * multiplier;
			multiplier *= dimensionSizes_[i];
		}
		return flatIndex;
	}

	public object GetValue(IReadOnlyList<int> indices, string varName = "")
	{
		try
		{
			return data_[GetFlatIndex(indices)];
		}
		catch (IndexOutOfRangeException ex)
		{
			throw new OutOfArrayBoundsError($"Index out of bounds for array {varName}.", ex);
		}
	}
	public void SetValue(IReadOnlyList<int> indices, object value, string varName = "")
	{
		try
		{
			data_[GetFlatIndex(indices)] = value;
		}
		catch (IndexOutOfRangeException ex)
		{
			throw new OutOfArrayBoundsError($"Index out of bounds for array {varName}.", ex);
		}
	}
}


