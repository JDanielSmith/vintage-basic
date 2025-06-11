namespace VintageBasic.Runtime.Errors;
sealed class BadRestoreTargetError : BasicRuntimeException
{
	public BadRestoreTargetError() : this("Bad RESTORE target") { }
	public BadRestoreTargetError(string message) : base(message) { }
	public BadRestoreTargetError(string message, Exception innerException) : base(message, innerException) { }
	public BadRestoreTargetError(int targetLabel, int lineNumber) : base($"Bad RESTORE target: {targetLabel}", lineNumber) { }
}
