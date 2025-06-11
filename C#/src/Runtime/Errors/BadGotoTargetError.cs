namespace VintageBasic.Runtime.Errors;
sealed class BadGotoTargetError : BasicRuntimeException
{
	public BadGotoTargetError() : this("Bad GOTO target") { }
	public BadGotoTargetError(string message) : base(message) { }
	public BadGotoTargetError(string message, Exception innerException) : base(message, innerException) { }
	public BadGotoTargetError(int targetLabel, int lineNumber) : base($"Bad GOTO target: {targetLabel}", lineNumber) { }
}

