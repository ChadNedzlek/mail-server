namespace Vaettir.Mail.Server
{
	public class UserData
	{
	    public UserData(string mailbox)
	    {
	        MailBox = mailbox;
	    }

	    public string MailBox { get; }
	}
}