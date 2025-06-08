using VintageBasic.Syntax; // For ValType
using static VintageBasic.Interpreter.RuntimeParsingUtils;

namespace VintageBasic.Runtime;

abstract record Val : IComparable<Val>
{
	public abstract int CompareTo(Val? other);
	public abstract string Suffix { get; }  // Type suffixes for BASIC variables
	public abstract Val DefaultValue { get; }
	public virtual bool IsNumeric => false; // Override in numeric types

	public abstract string TypeName { get; }
	internal bool EqualsType(Val val)
	{
		return EqualsType(val.GetType());
	}
	internal bool EqualsType(Type type)
	{
		return GetType() == type;
	}
	internal bool IsSameType<TVal>() where TVal : Val
	{
		return GetType() == typeof(TVal);
	}

	// Convenience methods for type checking and casting
	public virtual float AsFloat(int? lineNumber = null) => throw new Errors.TypeMismatchError($"Cannot convert {GetType().Name} to Float", lineNumber);
    public virtual int AsInt(int? lineNumber = null) => throw new Errors.TypeMismatchError($"Cannot convert {GetType().Name} to Int", lineNumber);
    public virtual string AsString(int? lineNumber = null) => throw new Errors.TypeMismatchError($"Cannot convert {GetType().Name} to String", lineNumber);
	public Val CoerceToType(Val value, int? lineNumber = null, StateManager? stateManager = null)
	{
		if (stateManager is not null && lineNumber.HasValue)
			stateManager.SetCurrentLineNumber(lineNumber.Value);

		var targetType = GetType();
		if (targetType == value.GetType()) return value;
		if (targetType == typeof(Val)) return value; // Allow Val as a generic target type

		if (CoerceToTypeImpl(value) is Val retval)
			return retval;
		throw new Errors.TypeMismatchError($"Cannot coerce {value.TypeName} to {targetType}", lineNumber ?? stateManager?.CurrentLineNumber);
	}
	internal abstract Val? CoerceToTypeImpl(Val value);

	// Coerces IntVal to FloatVal for expression evaluation if needed, otherwise returns original value.
	public static Val CoerceToExpressionType(Val value, int? lineNumber = null, StateManager? stateManager = null)
    {
        if (stateManager is not null && lineNumber.HasValue) stateManager.SetCurrentLineNumber(lineNumber.Value);

        if (value is IntVal iv)
        {
            return new FloatVal(iv.Value);
        }
        return value; // Already FloatVal or StringVal
    }

	public Val? TryParse(string inputString)
	{
		var stringToParse = GetType() == typeof(StringVal) ? inputString : inputString.Trim();
		return TryParseImpl(stringToParse);
	}
	internal abstract Val? TryParseImpl(string inputString);
}

sealed record FloatVal(float Value) : Val
{
    public FloatVal() : this(0.0f) { }
	public override float AsFloat(int? lineNumber = null) => Value;
    public override int AsInt(int? lineNumber = null) => RuntimeContext.FloatToInt(Value); // BASIC INT semantics
    public override string AsString(int? lineNumber = null) => base.AsString(lineNumber); // Or specific formatting if needed, like STR$

    public override int CompareTo(Val? other)
    {
        if (other is null) return 1;
        if (other is FloatVal fv) return Value.CompareTo(fv.Value);
        if (other is IntVal iv) return Value.CompareTo((float)iv.Value); // Promote IntVal to float for comparison
        throw new ArgumentException("Cannot compare FloatVal with " + other.GetType().Name);
    }
    public override string ToString() => $"{Value}";
    public override string Suffix => "";
	public static FloatVal Empty => new FloatVal();
    public override Val DefaultValue => Empty;
	public override bool IsNumeric => true;
	public override string TypeName => nameof(FloatVal);

	internal override Val? CoerceToTypeImpl(Val value)
	{
		if (value is IntVal iv) return new FloatVal(iv.Value);
		if (value is FloatVal) return value;
		return null;
	}
	internal override Val? TryParseImpl(string inputString)
	{
		if (TryParseFloat(inputString, out var fv))
			return new FloatVal(fv);
		return null;
	}
}

sealed record IntVal(int Value) : Val
{
	public IntVal() : this(0) { }
	public override float AsFloat(int? lineNumber = null) => Value;
    public override int AsInt(int? lineNumber = null) => Value;
    public override string AsString(int? lineNumber = null) => base.AsString(lineNumber);
    public override int CompareTo(Val? other)
    {
        if (other is null) return 1;
        if (other is IntVal iv) return Value.CompareTo(iv.Value);
        if (other is FloatVal fv) return ((float)Value).CompareTo(fv.Value); // Promote self to float for comparison
        throw new ArgumentException("Cannot compare IntVal with " + other.GetType().Name);
    }
	public override string ToString() => $"{Value}";
	public override string Suffix => "%";
	public static IntVal Empty => new IntVal();
    public override Val DefaultValue => Empty;
	public override bool IsNumeric => true;
	public override string TypeName => nameof(IntVal);

	internal override Val? CoerceToTypeImpl(Val value)
	{
		if (value is FloatVal fv) return new IntVal(RuntimeContext.FloatToInt(fv.Value));
		if (value is IntVal) return value;
		return null;
	}
	internal override Val? TryParseImpl(string inputString)
	{
		if (TryParseFloat(inputString, out var fvForInt))
			return new IntVal(RuntimeContext.FloatToInt(fvForInt));
		return null;
	}
}

sealed record StringVal(string Value) : Val
{
	public StringVal() : this(String.Empty) { }

	public override float AsFloat(int? lineNumber = null) => base.AsFloat(lineNumber); // Or VAL() semantics
    public override int AsInt(int? lineNumber = null) => base.AsInt(lineNumber);       // Or VAL() then INT()
    public override string AsString(int? lineNumber = null) => Value;

    public override int CompareTo(Val? other)
    {
        if (other is null) return 1;
        if (other is StringVal sv) return String.Compare(Value, sv.Value, StringComparison.Ordinal);
        throw new ArgumentException("Cannot compare StringVal with " + other.GetType().Name);
    }
	public override string ToString() => Value;
	public override string Suffix => "$";
	public static StringVal Empty => new StringVal();
	public override Val DefaultValue => Empty;
	public override string TypeName => nameof(StringVal);

	internal override Val? CoerceToTypeImpl(Val value)
	{
		if (value is StringVal) return value;
		// BASIC usually doesn't implicitly convert numbers to strings on assignment. STR$() is used.
		// However, if it were to, it would be like:
		// if (value is FloatVal fvStr) return new StringVal(RuntimeParsingUtils.PrintFloat(fvStr.Value).Trim());
		// if (value is IntVal ivStr) return new StringVal(ivStr.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
		return null;
	}
	internal override Val? TryParseImpl(string inputString) => new StringVal(inputString);
}
