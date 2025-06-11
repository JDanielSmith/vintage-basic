namespace VintageBasic.Runtime.Errors;
sealed class BadGosubTargetError : BasicRuntimeException
{
	public BadGosubTargetError() : this("Bad GOSUB target") { }
	public BadGosubTargetError(string message) : base(message) { }
	public BadGosubTargetError(string message, Exception innerException) : base(message, innerException) { }
	public BadGosubTargetError(int targetLabel, int? lineNumber) : base($"Bad GOSUB target: {targetLabel}", lineNumber) { }
}
