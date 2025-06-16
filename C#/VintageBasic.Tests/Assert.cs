using MSAssert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace VintageBasic.Tests;
static class Assert
{
	/// <summary>
	/// Verifies that the given collection contains only a single element of the given type.
	/// </summary>
	/// <typeparam name="T">The collection type.</typeparam>
	/// <param name="collection">The collection.</param>
	/// <returns>The single item in the collection.</returns>
	/// exactly one element.</exception>
	public static T Single<T>(IEnumerable<T> collection)
	{
		return collection.Single();
	}

	/// <summary>
	/// Verifies that two objects are equal, using a default comparer.
	/// </summary>
	/// <typeparam name="T">The type of the objects to be compared</typeparam>
	/// <param name="expected">The expected value</param>
	/// <param name="actual">The value to be compared against</param>
	public static void Equal<T>(T expected, T actual, int floatPrecision = 0)
	{
		MSAssert.AreEqual(expected, actual);
		MSAssert.IsTrue(floatPrecision >= 0);
	}

	/// <summary>
	/// Verifies that an object is exactly the given type (and not a derived type).
	/// </summary>
	/// <typeparam name="T">The type the object should be</typeparam>
	/// <param name="object">The object to be evaluated</param>
	/// <returns>The object, casted to type T when successful</returns>
	/// <exception cref="IsTypeException">Thrown when the object is not the given type</exception>
	public static T IsType<T>(object @object)
	{
		MSAssert.IsInstanceOfType<T>(@object);
		return (T)@object;
	}

	/// <summary>
	/// Verifies that a collection contains exactly a given number of elements, which meet
	/// the criteria provided by the element inspectors.
	/// </summary>
	/// <typeparam name="T">The type of the object to be verified</typeparam>
	/// <param name="collection">The collection to be inspected</param>
	/// <param name="elementInspectors">The element inspectors, which inspect each element in turn. The
	/// total number of element inspectors must exactly match the number of elements in the collection.</param>
	public static void Collection<T>(
		IEnumerable<T> collection,
		params Action<T>[] elementInspectors)
	{
		int i = 0;
		foreach (var item in collection)
		{
			var inspector = elementInspectors[i++];
			inspector(item);
		}
	}

	/// <summary>
	/// Verifies that a nullable struct value is null.
	/// </summary>
	/// <param name="value">The value to be inspected</param>
	public static void Null<T>(T? value) where T : struct
	{
		MSAssert.IsNull(value);
	}
}
