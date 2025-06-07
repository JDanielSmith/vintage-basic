namespace VintageBasic.Syntax;

sealed record VarName(ValType Type, string Name)
{
	string Suffix = Type == ValType.StringType? "$" : Type == ValType.IntType? "%" : ""; // Type suffixes for BASIC variables
	public override string ToString() => $"{Name}{Suffix}";
}
