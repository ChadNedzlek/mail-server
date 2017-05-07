namespace Vaettir.Mail.Server.Imap.Messages.Data
{
	public class NumberMessageData : IMessageData
	{
		public NumberMessageData(int value)
		{
			Value = value;
		}

		public int Value { get; }

		public string ToMessageString()
		{
			return Value.ToString();
		}
	}
}