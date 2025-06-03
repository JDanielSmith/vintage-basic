// src/VintageBasic/Runtime/Errors/BadRestoreTargetError.cs
namespace VintageBasic.Runtime.Errors
{
    public class BadRestoreTargetError : BasicRuntimeException
    {
        public int TargetLabel { get; }

		public BadRestoreTargetError(int targetLabel, int? lineNumber) : this(targetLabel, "Bad RESTORE target", lineNumber)
		{
		}

		public BadRestoreTargetError(int targetLabel, string message) : this(targetLabel, message, null)
		{
		}

		public BadRestoreTargetError(int targetLabel, string message, int? lineNumber)
            : base($"{message}: {targetLabel}", lineNumber)
        {
            TargetLabel = targetLabel;
        }
    }
}
