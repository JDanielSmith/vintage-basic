using static VintageBasic.Interpreter.RuntimeParsingUtils;

namespace VintageBasic.Runtime;

internal static class ValExtensions
{
	public static string GetTypeName(this object val) => val switch
	{
		int or float or string => val.GetType().Name,
		_ => throw new ArgumentException($"Unknown object type: {val.GetType().Name}")
	};
	 
	// Convenience methods for type checking and casting
	public static float AsFloat(this object o, int? lineNumber = null) => o switch
	{
		float or int => (float)o,
		/* string sv => VAL() semantics */
		_ => throw new Errors.TypeMismatchError($"Cannot convert {o.GetType().Name} to Float", lineNumber)
	};
	public static int AsInt(this object o, int? lineNumber = null) => o switch
	{
		float fv => (int) Math.Floor(fv), // BASIC INT semantics
		int iv => iv,
		/* string sv => VAL() then INT() */
		_ => throw new Errors.TypeMismatchError($"Cannot convert {o.GetType().Name} to Int", lineNumber)
	};
}


