namespace Vaettir.Mail.Server.Imap.Commands
{
	public interface IImapCommandMetadata
	{
		string Name { get; }
		SessionState MinimumState { get; }
	}
}