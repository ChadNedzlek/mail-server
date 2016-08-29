namespace Vaettir.Mail.Server
{
	public static class MailUtilities
	{
		public static string GetDomainFromMailbox(string mailbox)
		{
			int atIndex = mailbox.LastIndexOf('@');
			if (atIndex == -1)
			{
				return null;
			}

			return mailbox.Substring(atIndex + 1);
		}
	}
}