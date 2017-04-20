namespace Vaettir.Mail.Server
{
	public class UserData
	{
		public UserData(string mailbox)
		{
			Mailbox = mailbox;
		}

		public string Mailbox { get; }
	}
}
