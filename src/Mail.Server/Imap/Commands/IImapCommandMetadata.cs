namespace Vaettir.Mail.Server.Imap.Commands
{
	internal interface IImapCommandMetadata
	{
		string Name { get; }
		SessionState MinimumState { get; }
	}

	public sealed class ImapCommandMetadata : IImapCommandMetadata
	{
		public string Name { get; set; }
		public SessionState MinimumState { get; set; }
	}
}
