namespace Vaettir.Mail.Server.Imap.Messages.Data
{
	public class NilMessageData : IMessageData
	{
		private NilMessageData()
		{
		}

		public static NilMessageData Value { get; } = new NilMessageData();

		public string ToMessageString()
		{
			return "NIL";
		}
	}
}
