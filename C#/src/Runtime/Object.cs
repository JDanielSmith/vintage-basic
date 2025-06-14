using System.Collections.Frozen;
using static VintageBasic.Interpreter.RuntimeParsingUtils;

namespace VintageBasic.Runtime;

internal static class ValExtensions
{
	// Helper to check if a object is numeric (int or float)
	public static bool IsNumeric(this object val)
	{
		return val is int or float;
	}

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
		if (targetType == typeof(float)) return AsFloat(value);
		if (targetType == typeof(int)) return AsInt(value);
		if (targetType == typeof(string)) return value;
		throw new Errors.TypeMismatchError($"Cannot coerce {value.GetTypeName()} to {targetType}", lineNumber ?? stateManager?.CurrentLineNumber);
	}

	// Coerces int to float for expression evaluation if needed, otherwise returns original value.
	public static object CoerceToExpressionType(object value, int? lineNumber = null, StateManager? stateManager = null)
	{
		if (stateManager is not null && lineNumber.HasValue)
			stateManager.SetCurrentLineNumber(lineNumber.Value);
		return value is int iv ? (float)iv : value; // Coerce int to float for expression evaluation
	}

	static int FloatToInt(double val)
	{
		return (int)System.Math.Floor(val);
	}

	public static object? TryParse(this Type type, string inputString)
	{
		var stringToParse = type == typeof(string) ? inputString : inputString.Trim();
		if (type == typeof(float)) return TryParseFloat(stringToParse, out var value) ? value : null;
		if (type == typeof(int)) return TryParseFloat(stringToParse, out var fvForInt) ? FloatToInt(fvForInt) : null;
		if (type == typeof(string)) return stringToParse;
		return null;
	}

	// Convenience methods for type checking and casting
	public static float AsFloat(this object o, int? lineNumber = null) => o switch
	{
		float or int => (float)o,
		/* string sv => VAL() semantics */
		_ => throw new Errors.TypeMismatchError($"Cannot convert {o.GetType().Name} to Float", lineNumber)
	};
	public static int AsInt(this object o, int? lineNumber = null) => o switch
	{
		float fv => FloatToInt(fv), // BASIC INT semantics
		int iv => iv,
		/* string sv => VAL() then INT() */
		_ => throw new Errors.TypeMismatchError($"Cannot convert {o.GetType().Name} to Int", lineNumber)
	};
}


