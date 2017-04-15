using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Smtp;

namespace Vaettir.Mail.Server.FileSystem
{
	public class FileSystemMailboxStore : IMailboxStore
	{
		public FileSystemMailboxStore(SmtpSettings settings)
		{
			_settings = settings;
		}

		private static readonly Regex s_maildirPattern = new Regex(@"^(.);2,(.*)$");

		private static (string id, MailboxFlags) CalculateFlagsFromFileName(string fileName)
		{
			fileName = Path.GetFileName(fileName);
			Match match = s_maildirPattern.Match(fileName);
			if (!match.Success)
				return (fileName, MailboxFlags.None);

			return (match.Groups[1].Value, ImapHelper.GetFlagsFromMailDir(match.Groups[2].Value));
		}

		private static string CalculateFilnameFromFlags(string id, MailboxFlags flags)
		{
			return $"{id};2,{ImapHelper.GetMailDirFromFlags(flags)}";
		}

		private string GetPath(MboxReferenceBase mbox)
		{
			var folderPart = Path.Combine(mbox.Folder.Split('/').Select(s => "." + s).ToArray());
			return Path.Combine(
				_settings.MailLocalPath,
				MailUtilities.GetDomainFromMailbox(mbox.Mailbox),
				MailUtilities.GetNameFromMailbox(mbox.Mailbox),
				folderPart);
		}

		private abstract class MboxReferenceBase
		{
			protected MboxReferenceBase(string mailbox, string folder, string id, MailboxFlags flags)
			{
				Id = id;
				Flags = flags;
				Mailbox = mailbox;
				Folder = folder;
			}

			protected MboxReferenceBase(string mailbox, string folder, string filename)
			{
				Mailbox = mailbox;
				Folder = folder;
				(Id, Flags) = CalculateFlagsFromFileName(filename);
			}

			public string Id { get; }
			public MailboxFlags Flags { get; set; }
			public string Mailbox { get; }
			public string Folder { get; set; }
		}

		private class MBoxReference : MboxReferenceBase, IMailboxItemReference
		{
			public string CurrentFileName;

			public MBoxReference(string mailbox, string folder, string currentFileName) : base (mailbox, folder, currentFileName)
			{
				CurrentFileName = currentFileName;
			}
		}

		private class MBoxReadReference : MboxReferenceBase, IMailboxItemReadReference
		{
			public MBoxReadReference(string mailbox, string folder, string currentFileName, Stream bodyStream) : base(mailbox, folder, currentFileName)
			{
				BodyStream = bodyStream;
			}

			public void Dispose()
			{
				BodyStream?.Dispose();
			}
			
			public Stream BodyStream { get; }
		}

		private class MBoxWriteReference : MboxReferenceBase, IMailboxItemWriteReference
		{
			public string TempPath { get; }

			public MBoxWriteReference(string mailbox, string folder, string id, IWriter store, Stream bodyStream, string tempPath) : base (mailbox, folder, id, MailboxFlags.None)
			{
				Store = store;
				BodyStream = bodyStream;
				TempPath = tempPath;
			}

			public void Dispose()
			{
				BodyStream?.Dispose();
			}

			public Stream BodyStream { get; }
			public IWriter Store { get; }
		}

		private readonly SmtpSettings _settings;

		public Task<IMailboxItemReadReference> OpenReadAsync(IMailboxItemReference reference, CancellationToken token)
		{
			if (!(reference is MBoxReference mbox))
			{
				throw new ArgumentException("reference of incorrect type", nameof(reference));
			}
			
			return Task.FromResult((IMailboxItemReadReference) new MBoxReadReference(mbox.Mailbox, mbox.Folder, mbox.CurrentFileName, File.OpenRead(mbox.CurrentFileName)));
		}

		public Task SaveAsync(IWritable reference, CancellationToken token)
		{
			throw new NotImplementedException();
		}

		public Task DeleteAsync(IMailboxItemReference reference)
		{
			if (!(reference is MBoxReference mbox))
			{
				throw new ArgumentException("reference of incorrect type", nameof(reference));
			}

			File.Delete(mbox.CurrentFileName);
			return Task.CompletedTask;
		}

		public Task<IMailboxItemWriteReference> NewMailAsync(string id, string mailbox, string folder, CancellationToken token)
		{
			string tempFileName = Path.GetTempFileName();
			return Task.FromResult((IMailboxItemWriteReference) new MBoxWriteReference(
				mailbox,
				folder,
				id,
				this,
				File.Create(tempFileName),
				tempFileName));
		}

		public Task MoveAsync(IMailboxItemReference reference, string folder, CancellationToken token)
		{
			if (!(reference is MBoxReference mbox))
			{
				throw new ArgumentException("reference of incorrect type", nameof(reference));
			}

			mbox.Folder = folder;
			Relocate(mbox);
			return Task.CompletedTask;
		}

		private void Relocate(MBoxReference mbox)
		{
			string newPath = GetPath(mbox);
			File.Move(mbox.CurrentFileName, newPath);
			mbox.CurrentFileName = newPath;
		}

		public Task SetFlags(IMailboxItemReference reference, MailboxFlags flags, CancellationToken token)
		{
			if (!(reference is MBoxReference mbox))
			{
				throw new ArgumentException("reference of incorrect type", nameof(reference));
			}

			mbox.Flags = flags;
			Relocate(mbox);
			return Task.CompletedTask;
		}
	}
}