namespace Vaettir.Mail.Server.Imap.Messages.Data
{
	public class QuotedMessageData : IMessageData
	{
		public QuotedMessageData(string value)
		{
			Value = value;
		}

		public string Value { get; }

		public string ToMessageString()
		{
			return "\"" + Value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
		}
	}
}