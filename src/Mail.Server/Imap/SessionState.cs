namespace Vaettir.Mail.Server.Imap
{
	public enum SessionState
	{
		Open = 0,
		NotAuthenticated,
		Authenticated,
		Selected,
		Logout,
		Closed
	}
}