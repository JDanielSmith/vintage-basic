// src/VintageBasic/Runtime/Errors/BadGosubTargetError.cs
namespace VintageBasic.Runtime.Errors
{
    public class BadGosubTargetError : BasicRuntimeException
    {
        public int TargetLabel { get; }

		public BadGosubTargetError(int targetLabel, int? lineNumber) : this(targetLabel, "Bad GOSUB target", lineNumber)
		{
		}

		public BadGosubTargetError(int targetLabel, string message) : this(targetLabel, message, null)
		{
		}

		public BadGosubTargetError(int targetLabel, string message, int? lineNumber)
            : base($"{message}: {targetLabel}", lineNumber)
        {
            TargetLabel = targetLabel;
        }
    }
}
