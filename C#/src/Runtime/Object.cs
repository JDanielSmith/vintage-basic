using VintageBasic.Runtime.Errors;
using static VintageBasic.Interpreter.RuntimeParsingUtils;

namespace VintageBasic.Runtime;

internal static class ValExtensions
{
	public static string GetTypeName(this object val) => val switch
	{
		int or float or string => val.GetType().Name,
		_ => throw new ArgumentException($"Unknown object type: {val.GetType().Name}")
	};

	public static object CoerceToType(this Type targetType, object value, int? lineNumber = null, StateManager? stateManager = null)
	{
		if (stateManager is not null && lineNumber.HasValue)
			stateManager.SetCurrentLineNumber(lineNumber.Value);

		if (targetType == value.GetType()) return value;
		if (targetType == typeof(object)) return value; // Allow object as a generic target type
		if (targetType == typeof(float)) return value.AsFloat(lineNumber);
		if (targetType == typeof(int)) return value.AsInt(lineNumber);
		if (targetType == typeof(string)) return value;
		throw new TypeMismatchError($"Cannot coerce {value.GetTypeName()} to {targetType}", lineNumber ?? stateManager?.CurrentLineNumber);
	}

	// Convenience methods for type checking and casting
	public static float AsFloat(this object o, int? lineNumber = null) => o switch
	{
		float or int => (float)o,
		/* string sv => VAL() semantics */
		_ => throw new TypeMismatchError($"Cannot convert {o.GetType().Name} to Float", lineNumber)
	};
	public static int AsInt(this object o, int? lineNumber = null) => o switch
	{
		float fv => (int) Math.Floor(fv), // BASIC INT semantics
		int iv => iv,
		/* string sv => VAL() then INT() */
		_ => throw new TypeMismatchError($"Cannot convert {o.GetType().Name} to Int", lineNumber)
	};
}


