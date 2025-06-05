using VintageBasic.Syntax; // For ValType

namespace VintageBasic.Runtime;

abstract record Val(ValType Type) : IComparable<Val>
{
    public abstract int CompareTo(Val? other);

    // Convenience methods for type checking and casting
    public virtual float AsFloat(int? lineNumber = null) => throw new Errors.TypeMismatchError($"Cannot convert {GetType().Name} to Float", lineNumber);
    public virtual int AsInt(int? lineNumber = null) => throw new Errors.TypeMismatchError($"Cannot convert {GetType().Name} to Int", lineNumber);
    public virtual string AsString(int? lineNumber = null) => throw new Errors.TypeMismatchError($"Cannot convert {GetType().Name} to String", lineNumber);

    public static Val CoerceToType(ValType targetType, Val value, int? lineNumber = null, StateManager? stateManager = null)
    {
        if (stateManager is not null && lineNumber.HasValue) stateManager.SetCurrentLineNumber(lineNumber.Value);

        if (targetType == value.Type) return value;

        switch (targetType)
        {
            case ValType.FloatType:
                if (value is IntVal iv) return new FloatVal(iv.Value);
                if (value is FloatVal) return value;
                break;
            case ValType.IntType:
                if (value is FloatVal fv) return new IntVal(RuntimeContext.FloatToInt(fv.Value));
                if (value is IntVal) return value;
                break;
            case ValType.StringType:
                if (value is StringVal) return value; 
                // BASIC usually doesn't implicitly convert numbers to strings on assignment. STR$() is used.
                // However, if it were to, it would be like:
                // if (value is FloatVal fvStr) return new StringVal(RuntimeParsingUtils.PrintFloat(fvStr.Value).Trim());
                // if (value is IntVal ivStr) return new StringVal(ivStr.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                break;
        }
        throw new Errors.TypeMismatchError($"Cannot coerce {value.Type} to {targetType}", lineNumber ?? stateManager?.CurrentLineNumber);
    }

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
}

sealed record FloatVal(float Value) : Val(ValType.FloatType)
{
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
}

sealed record IntVal(int Value) : Val(ValType.IntType)
{
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
}

sealed record StringVal(string Value) : Val(ValType.StringType)
{
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
}
