namespace VintageBasic.Syntax;

sealed class ValType
{
	public static readonly ValType FloatType = new(typeof(VintageBasic.Runtime.FloatVal));
	public static readonly ValType IntType = new(typeof(VintageBasic.Runtime.IntVal));
	public static readonly ValType StringType = new(typeof(VintageBasic.Runtime.StringVal));

	Type Type { get; }
	ValType(Type type)
	{
		Type = type;
	}
}