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

	public static bool IsSameType<TVal>(this Type type) where TVal : struct
	{
		return type == typeof(TVal);
	}
	public static bool IsSameType<TVal>(this object val) where TVal : struct
	{
		return val.GetType().IsSameType<TVal>();
	}
	public static bool EqualsType(this object lhs, object rhs)
	{
		return lhs.GetType() == rhs.GetType();
	}
	public static bool EqualsType(this Type lhs, object rhs)
	{
		return lhs == rhs.GetType();
	}
	public static object CoerceToType(this object vv, object value, int? lineNumber = null, StateManager? stateManager = null)
	{
		if (stateManager is not null && lineNumber.HasValue)
			stateManager.SetCurrentLineNumber(lineNumber.Value);

		var targetType = vv.GetType();
		if (targetType == value.GetType()) return value;
		if (targetType == typeof(object)) return value; // Allow object as a generic target type

		var retval = vv switch
		{
			float fv => fv.CoerceToTypeImpl(value),
			int iv => iv.CoerceToTypeImpl(value),
			string sv => sv.CoerceToTypeImpl(value),
			_ => null
		};
		if (retval is not null)
			return retval;
		throw new Errors.TypeMismatchError($"Cannot coerce {value.GetTypeName()} to {targetType}", lineNumber ?? stateManager?.CurrentLineNumber);
	}
	// Coerces int to float for expression evaluation if needed, otherwise returns original value.
	public static object CoerceToExpressionType(object value, int? lineNumber = null, StateManager? stateManager = null)
	{
		if (stateManager is not null && lineNumber.HasValue) stateManager.SetCurrentLineNumber(lineNumber.Value);

		if (value is int iv)
		{
			return (float)iv;
		}
		return value; // Already float or string
	}

	public static object? TryParse(this object vv, string inputString)
	{
		var stringToParse = vv.GetType() == typeof(string) ? inputString : inputString.Trim();
		return vv switch
		{
			float fv => fv.TryParseImpl(stringToParse),
			int iv => iv.TryParseImpl(stringToParse),
			string sv => sv.TryParseImpl(stringToParse),
			_ => null
		};
	}

	// Convenience methods for type checking and casting
	public static float AsFloat(this object vv, int? lineNumber = null) => vv switch
	{
		float fv => fv.AsFloat(lineNumber),
		int iv => iv.AsFloat(lineNumber),
		string sv => sv.AsFloat(lineNumber),
		_ => throw new Errors.TypeMismatchError($"Cannot convert {vv.GetType().Name} to Float", lineNumber)
	};
	public static int AsInt(this object vv, int? lineNumber = null) => vv switch
	{
		float fv => fv.AsInt(lineNumber),
		int iv => iv.AsInt(lineNumber),
		string sv => sv.AsInt(lineNumber),
		_ => throw new Errors.TypeMismatchError($"Cannot convert {vv.GetType().Name} to Int", lineNumber)
	};
}

internal static class SingleExtensions
{
	public static object? CoerceToTypeImpl(this float fv, object value)
	{
		if (value is int iv) return (float)iv;
		if (value is float) return value;
		return null;
	}
	public static object? TryParseImpl(this float fv, string inputString)
	{
		if (TryParseFloat(inputString, out var value))
			return value;
		return null;
	}

	public static float AsFloat(this float fv, int? lineNumber = null) => fv;
	public static int AsInt(this float fv, int? lineNumber = null) => RuntimeContext.FloatToInt(fv); // BASIC INT semantics
}

internal static class Int32Extensions
{
	public static object? CoerceToTypeImpl(this int iv, object value)
	{
		if (value is float fv) return RuntimeContext.FloatToInt(fv);
		if (value is int) return value;
		return null;
	}
	public static object? TryParseImpl(this int iv, string inputString)
	{
		if (TryParseFloat(inputString, out var fvForInt))
			return RuntimeContext.FloatToInt(fvForInt);
		return null;
	}

	public static float AsFloat(this int iv, int? lineNumber = null) => iv;
	public static int AsInt(this int iv, int? lineNumber = null) => iv;
}

internal static class StringExtensions
{
	public static object? CoerceToTypeImpl(this string sv, object value)
	{
		if (value is string) return value;
		// BASIC usually doesn't implicitly convert numbers to strings on assignment. STR$() is used.
		// However, if it were to, it would be like:
		// if (value is float fvStr) return RuntimeParsingUtils.PrintFloat(fvStr.Value).Trim();
		// if (value is int ivStr) return ivStr.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
		return null;
	}
	public static object? TryParseImpl(this string sv, string inputString) => inputString;

	public static float AsFloat(this string sv, int? lineNumber = null) => throw new Errors.TypeMismatchError($"Cannot convert {sv.GetType().Name} to Float", lineNumber);  // Or VAL() semantics
	public static int AsInt(this string sv, int? lineNumber = null) => throw new Errors.TypeMismatchError($"Cannot convert {sv.GetType().Name} to Int", lineNumber); // Or VAL() then INT()
}

