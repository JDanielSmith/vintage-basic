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

	public static string GetSuffix(this object val) => val switch
	{
		int => "%",
		float => "",
		string => "$",
		_ => throw new ArgumentException($"Unknown object type: {val.GetType().Name}")
	};

	public static object CoerceToType(this object vv, object value, int? lineNumber = null, StateManager? stateManager = null)
	{
		if (stateManager is not null && lineNumber.HasValue)
			stateManager.SetCurrentLineNumber(lineNumber.Value);

		static object? CoerceToFloat(object value)
		{
			if (value is int iv) return (float)iv;
			if (value is float) return value;
			return null;
		}
		static object? CoerceToInt(object value)
		{
			if (value is float fv) return RuntimeContext.FloatToInt(fv);
			if (value is int) return value;
			return null;
		}
		static object? CoerceToString(object value)
		{
			if (value is string) return value;
			// BASIC usually doesn't implicitly convert numbers to strings on assignment. STR$() is used.
			// However, if it were to, it would be like:
			// if (value is float fvStr) return RuntimeParsingUtils.PrintFloat(fvStr.Value).Trim();
			// if (value is int ivStr) return ivStr.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
			return null;
		}

		var targetType = vv.GetType();
		if (targetType == value.GetType()) return value;
		if (targetType == typeof(object)) return value; // Allow object as a generic target type

		var retval = vv switch
		{
			float => CoerceToFloat(value),
			int => CoerceToInt(value),
			string => CoerceToString(value),
			_ => null
		};
		if (retval is not null)
			return retval;
		throw new Errors.TypeMismatchError($"Cannot coerce {value.GetTypeName()} to {targetType}", lineNumber ?? stateManager?.CurrentLineNumber);
	}
	// Coerces int to float for expression evaluation if needed, otherwise returns original value.
	public static object CoerceToExpressionType(object value, int? lineNumber = null, StateManager? stateManager = null)
	{
		if (stateManager is not null && lineNumber.HasValue)
			stateManager.SetCurrentLineNumber(lineNumber.Value);
		return value is int iv ? (float)iv : value; // Coerce int to float for expression evaluation
	}

	public static object? TryParse(this object o, string inputString)
	{
		var stringToParse = o.GetType() == typeof(string) ? inputString : inputString.Trim();
		return o switch
		{
			float => TryParseFloat(stringToParse, out var value) ? value : null,
			int => TryParseFloat(stringToParse, out var fvForInt) ? RuntimeContext.FloatToInt(fvForInt) : null,
			string => stringToParse,
			_ => null
		};
	}

	// Convenience methods for type checking and casting
	public static float AsFloat(this object vv, int? lineNumber = null) => vv switch
	{
		float fv => fv,
		int iv => iv,
		/* string sv => VAL() semantics */
		_ => throw new Errors.TypeMismatchError($"Cannot convert {vv.GetType().Name} to Float", lineNumber)
	};
	public static int AsInt(this object vv, int? lineNumber = null) => vv switch
	{
		float fv => RuntimeContext.FloatToInt(fv), // BASIC INT semantics
		int iv => iv,
		/* string sv => VAL() then INT() */
		_ => throw new Errors.TypeMismatchError($"Cannot convert {vv.GetType().Name} to Int", lineNumber)
	};
}


