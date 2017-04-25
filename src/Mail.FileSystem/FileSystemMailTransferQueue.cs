using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vaettir.Mail.Server.Smtp;

namespace Vaettir.Mail.Server.FileSystem
{
	[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
	public class FileSystemMailTransferQueue : FileSystemMailQueueBase, IMailTransferQueue
	{
		public FileSystemMailTransferQueue(SmtpSettings settings) : base(settings)
		{
			Directory.CreateDirectory(Settings.MailOutgoingQueuePath);
		}

		public Task<IMailWriteReference> NewMailAsync(
			string id,
			string sender,
			IImmutableList<string> recipients,
			CancellationToken token)
		{
			List<IGrouping<string, string>> recipientsByDomain = recipients.GroupBy(MailUtilities.GetDomainFromMailbox).ToList();
			if (recipientsByDomain.Count > 1)
			{
				throw new ArgumentException("Multiple domains in recipients list", nameof(recipients));
			}

			IGrouping<string, string> firstGroup = recipientsByDomain.First();
			string domain = firstGroup.Key;

			string GetPathFromName(string name)
			{
				return Path.Combine(GetRootPath(domain, "cur"), name);
			}

			string GetTempPathFromName(string name)
			{
				return Path.Combine(GetRootPath(domain, "tmp"), name);
			}

			return CreateWriteReference(
				sender,
				token,
				firstGroup,
				GetTempPathFromName,
				GetPathFromName
			);
		}

		private string GetRootPath(string domain, string type)
		{
			return Path.Combine(Settings.MailOutgoingQueuePath, domain, type);
		}

		public IEnumerable<string> GetAllPendingDomains()
		{
			return Directory.GetDirectories(Settings.MailOutgoingQueuePath).Select(Path.GetFileName);
		}

		public IEnumerable<IMailReference> GetAllMailForDomain(string domain)
		{
			return Directory.GetFiles(GetRootPath(domain, "cur"), "*", SearchOption.TopDirectoryOnly)
				.Select(path => new Reference(Path.GetFileNameWithoutExtension(path), path));
		}

		public override Task SaveAsync(IWritable reference, CancellationToken token)
		{
			var writeReference = reference as WriteReference;
			if (writeReference == null)
			{
				throw new ArgumentException("reference must be from NewMailAsync", nameof(reference));
			}

			if (writeReference.Saved)
			{
				throw new InvalidOperationException("Already saved");
			}

			writeReference.BodyStream.Dispose();
			File.Move(writeReference.TempPath, writeReference.Path);
			writeReference.Saved = true;
			return Task.CompletedTask;
		}

		public override async Task DeleteAsync(IMailReference reference)
		{
			await base.DeleteAsync(reference);
			
			try
			{
				var mailReference = (Reference)reference;
				// root/in/domain.name/cur/mail-id-path.mbx
				// we want to remove the domain.name directory if this is the last mail going into it
				// Directory.Delete(Path.GetDirectoryName(Path.GetDirectoryName(mailReference.Path)));
			}
			catch (Exception)
			{
				// Don't care, we were just cleaning up after ourselves, after all
			}
		}
	}
}
