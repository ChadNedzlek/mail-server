namespace Vaettir.Mail.Server.Imap.Messages.Data
{
	public class ServerMessageData : IMessageData
	{
		public ServerMessageData(string message)
		{
			Message = message;
		}

		public string Message { get; }

		public string ToMessageString()
		{
			return Message;
		}
	}
}
