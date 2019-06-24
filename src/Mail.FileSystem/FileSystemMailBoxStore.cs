using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.FileSystem
{
	[Injected]
	public class FileSystemMailboxStore : IMailboxStore, IImapMailStore
	{
		private const string NewMailStatus = "new";
		private const string CurrentMailStatus = "cur";
		private const string TempMailStatus = "tmp";

		private static readonly Regex s_maildirPattern = new Regex(@"^(.*);2,(.*)$");

		private readonly AgentSettings _settings;

		public FileSystemMailboxStore(AgentSettings settings)
		{
			_settings = settings;
		}

		public Task<Mailbox> GetMailBoxAsync(
			UserData authenticatedUser,
			string mailbox,
			bool isExamine,
			CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task<Mailbox> CreateMailboxAsync(
			UserData authenticatedUser,
			string mailbox,
			CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task DeleteMailboxAsync(UserData authenticatedUser, string mailbox, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task RenameMailboxAsync(
			UserData authenticatedUser,
			string oldMailbox,
			string newMailbox,
			CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task MarkMailboxSubscribedAsync(
			UserData authenticatedUser,
			string mailbox,
			bool subscribed,
			CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<Mailbox>> ListMailboxesAsync(
			UserData authenticatedUser,
			string pattern,
			CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task SaveAsync(MailMessage message)
		{
			throw new NotImplementedException();
		}

		public Task RefreshAsync(MailMessage message)
		{
			throw new NotImplementedException();
		}

		public Task<Stream> OpenBinaryAsync(
			string mailbox,
			DateTime dateTime,
			IEnumerable<string> flags,
			CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task<IMailboxItemReadReference> OpenReadAsync(IMailboxItemReference reference, CancellationToken token)
		{
			if (!(reference is MBoxReference mbox))
			{
				throw new ArgumentException("reference of incorrect type", nameof(reference));
			}

			return Task.FromResult(
				(IMailboxItemReadReference) new MBoxReadReference(
					mbox.Mailbox,
					mbox.Folder,
					mbox.CurrentFileName,
					File.OpenRead(mbox.CurrentFileName)));
		}

		public Task SaveAsync(IWritable reference, CancellationToken token)
		{
			if (!(reference is MBoxWriteReference mbox))
			{
				throw new ArgumentException("reference of incorrect type", nameof(reference));
			}

			// We need to somehow add a "References:" entry to the mail...

			string newPath = GetPath(mbox, mbox.IsNew ? NewMailStatus : CurrentMailStatus);
			EnsureDirectoryFor(newPath);
			File.Move(mbox.TempPath, newPath);
			return Task.CompletedTask;
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

		public Task<IEnumerable<IMailboxItemReference>> GetMails(string mailbox, string folder, CancellationToken token)
		{
			return Task.FromResult(
				(IEnumerable<IMailboxItemReference>) Directory
					.GetFiles(
						GetFolderPath(mailbox, folder, CurrentMailStatus),
						"*.mbox",
						SearchOption.TopDirectoryOnly)
					.Select(file => new MBoxReference(mailbox, folder, file)));
		}

		public Task<IEnumerable<string>> GetFolders(string mailbox, string folder, CancellationToken token)
		{
			return Task.FromResult(
				Directory.GetDirectories(
						GetFolderPath(mailbox, folder, CurrentMailStatus),
						".*",
						SearchOption.TopDirectoryOnly)
					.Select(s => s.Substring(1)) // Strip off the leading .
			);
		}

		public Task<IMailboxItemWriteReference> NewMailAsync(
			string id,
			string mailbox,
			string folder,
			CancellationToken token)
		{
			if (string.Equals(folder, "inbox", StringComparison.OrdinalIgnoreCase))
			{
				folder = "";
			}

			if (string.IsNullOrEmpty(id) || id[0] == '.' || Path.GetInvalidFileNameChars().Any(id.Contains))
			{
				throw new ArgumentException("Invalid mail id. Must be a valid file name and cannot start with .");
			}
			
			string tempFileName = Path.Combine(GetFolderPath(mailbox, folder, TempMailStatus), $"{id}.mbox");
			EnsureDirectoryFor(tempFileName);
			return Task.FromResult(
				(IMailboxItemWriteReference) new MBoxWriteReference(
					mailbox,
					folder,
					id,
					this,
					File.Create(tempFileName),
					tempFileName,
					isNew: true));
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

		private static void EnsureDirectoryFor(string newPath)
		{
			// ReSharper disable once AssignNullToNotNullAttribute
			Directory.CreateDirectory(Path.GetDirectoryName(newPath));
		}

		private static (string id, MailboxFlags) CalculateFlagsFromFileName(string fileName)
		{
			fileName = Path.GetFileName(fileName);
			// ReSharper disable once AssignNullToNotNullAttribute
			Match match = s_maildirPattern.Match(fileName);
			if (!match.Success)
			{
				return (fileName, MailboxFlags.None);
			}

			return (match.Groups[1].Value, ImapHelper.GetFlagsFromMailDir(match.Groups[2].Value));
		}

		private static string CalculateFilenameFromFlags(string id, MailboxFlags flags)
		{
			return $"{id};2,{ImapHelper.GetMailDirFromFlags(flags)}";
		}

		private string GetPath(MboxReferenceBase mbox, string status)
		{
			return Path.Combine(
				GetFolderPath(mbox.Mailbox, mbox.Folder, status),
				CalculateFilenameFromFlags(mbox.Id, mbox.Flags) + ".mbox");
		}

		private static readonly char[] FolderSeparator = new char['/'];
		private string GetFolderPath(string mailbox, string folder, string status)
		{
			// Turn foo/bar into .foo.bar, because that's what maildir says
			string folderPart = string.Join(
				"",
				folder.Split(FolderSeparator,StringSplitOptions.RemoveEmptyEntries)
					.Select(s => "." + s)
					.ToArray()
			);

			var nameFromMailbox = MailUtilities.GetNameFromMailbox(mailbox);


			return Path.Combine(
				_settings.MailLocalPath,
				MailUtilities.GetDomainFromMailbox(mailbox),
				StripExtension(nameFromMailbox),
				folderPart,
				status);
		}

		private string StripExtension(string name)
		{
			int extensionIndex = name.IndexOf('-');
			if (extensionIndex == -1)
			{
				return name;
			}

			return name.Substring(0, extensionIndex);
		}

		private void Relocate(MBoxReference mbox)
		{
			string newPath = GetPath(mbox, CurrentMailStatus);
			EnsureDirectoryFor(newPath);
			File.Move(mbox.CurrentFileName, newPath);
			mbox.CurrentFileName = newPath;
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

			public MBoxReference(string mailbox, string folder, string currentFileName) : base(
				mailbox,
				folder,
				currentFileName)
			{
				CurrentFileName = currentFileName;
			}
		}

		private class MBoxReadReference : MboxReferenceBase, IMailboxItemReadReference
		{
			public MBoxReadReference(string mailbox, string folder, string currentFileName, Stream bodyStream) : base(
				mailbox,
				folder,
				currentFileName)
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
			public MBoxWriteReference(
				string mailbox,
				string folder,
				string id,
				IWriter store,
				Stream bodyStream,
				string tempPath,
				bool isNew) : base(mailbox, folder, id, MailboxFlags.None)
			{
				Store = store;
				BodyStream = bodyStream;
				TempPath = tempPath;
				IsNew = isNew;
			}

			public string TempPath { get; }
			public bool IsNew { get; }
			public Stream BodyStream { get; }
			public IWriter Store { get; }

			public void Dispose()
			{
				BodyStream?.Dispose();
			}
		}
	}
}
