// src/VintageBasic/Runtime/Errors/BadGotoTargetError.cs
namespace VintageBasic.Runtime.Errors
{
    public class BadGotoTargetError : BasicRuntimeException
    {
        public int TargetLabel { get; }

		public BadGotoTargetError(int targetLabel, int? lineNumber) : this(targetLabel, "Bad GOTO target", lineNumber)
		{
		}

		public BadGotoTargetError(int targetLabel, string message) : this(targetLabel, message, null)
		{
		}

		public BadGotoTargetError(int targetLabel, string message, int? lineNumber)
			: base($"{message}: {targetLabel}", lineNumber)
		{
			TargetLabel = targetLabel;
		}
	}
}
