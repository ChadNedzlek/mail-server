using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IMailboxItemReference
	{
		string Mailbox { get; }
		string Folder { get; }
		string Id { get; }
		MailboxFlags Flags { get; }
	}

	public interface IMailboxItemWriteReference : IWritable, IDisposable
	{
		string Mailbox { get; }
		string Folder { get; }
		string Id { get; }
		MailboxFlags Flags { get; }
	}

	public interface IMailboxItemReadReference : IDisposable
	{
		string Mailbox { get; }
		string Folder { get; }
		string Id { get; }
		MailboxFlags Flags { get; }

		Stream BodyStream { get; }
	}

	public interface IMailboxStore : IWriter
	{
		Task<IMailboxItemWriteReference> NewMailAsync(string id, string mailbox, string folder, CancellationToken token);

		Task MoveAsync(IMailboxItemReference reference, string folder, CancellationToken token);
		Task SetFlags(IMailboxItemReference reference, MailboxFlags flags, CancellationToken token);
		Task<IMailboxItemReadReference> OpenReadAsync(IMailboxItemReference reference, CancellationToken token);
		Task DeleteAsync(IMailboxItemReference reference);

		Task<IEnumerable<IMailboxItemReference>> GetMails(string mailbox, string folder, CancellationToken token);
		Task<IEnumerable<string>> GetFolders(string mailbox, string folder, CancellationToken token);
	}

	public static class MailboxStoreExtensions
	{
		public static Task<IMailboxItemWriteReference> NewMailAsync(
			this IMailboxStore store,
			string id,
			string mailbox,
			CancellationToken token)
		{
			return store.NewMailAsync(id, mailbox, "inbox", token);
		}
	}
}