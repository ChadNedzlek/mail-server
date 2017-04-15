using System.IO;
using System.Text;
using Vaettir.Mail.Server;
using Vaettir.Utility;

namespace Vaettir.Mail.Test.Utilities
{
	public class MockMailboxItemReference : IMailboxItemReference, IMailboxItemReadReference, IMailboxItemWriteReference
	{
		public MockMailboxItemReference(string id, string mailbox, string folder, MailboxFlags flags, bool saved, IWriter store)
			: this(id, mailbox, folder, flags, saved, (byte[]) null, store)
		{
		}

		public MockMailboxItemReference(string id, string mailbox, string folder, MailboxFlags flags, bool saved, byte[] body, IWriter store)
		{
			Id = id;
			Flags = flags;
			BackupBodyStream = body == null ? new MemoryStream() : new MemoryStream(body);
			BodyStream = new MultiStream(new[] { BackupBodyStream }, true);
			IsSaved = saved;
			Store = store;
			Mailbox = mailbox;
			Folder = folder;
		}

		public MockMailboxItemReference(string id, string mailbox, string folder, MailboxFlags flags, bool saved, string body, IWriter store)
			: this(id, mailbox, folder, flags, saved, Encoding.ASCII.GetBytes(body), store)
		{
		}

		public string Mailbox { get; }
		public string Folder { get; set; }
		public string Id { get; }
		public MailboxFlags Flags { get; set; }
		public bool IsSaved { get; set; }

		public void Dispose()
		{
			BodyStream?.Dispose();
			BackupBodyStream?.Dispose();
		}

		public Stream BodyStream { get; }
		public IWriter Store { get; }

		public Stream BackupBodyStream { get; }
	}
}