namespace VintageBasic.Syntax;

sealed record VarName(ValType Type, string Name)
{
    string Suffix => Type switch // Basic-like representation
	{
        ValType.StringType => "$",
        ValType.IntType => "%",
        _ => ""
    };
	public override string ToString() => $"{Name}{Suffix}";
}
