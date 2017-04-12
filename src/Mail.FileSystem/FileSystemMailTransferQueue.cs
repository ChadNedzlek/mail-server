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
		}

		public Task<IMailWriteReference> NewMailAsync(string id, string sender, IImmutableList<string> recipients, CancellationToken token)
		{
			var recipientsByDomain = recipients.GroupBy(MailUtilities.GetDomainFromMailbox).ToList();
			if (recipientsByDomain.Count > 1)
			{
				throw new ArgumentException("Multiple domains in recipients list", nameof(recipients));
			}

			var firstGroup = recipientsByDomain.First();
			string domain = firstGroup.Key;

			string GetPathFromName(string name)
			{
				return Path.Combine(_settings.MailOutgoingQueuePath, domain, name);
			}

			return CreateWriteReference(
				sender,
				token,
				firstGroup,
				GetPathFromName
			);
		}

		public IEnumerable<string> GetAllPendingDomains()
		{
			return Directory.GetDirectories(_settings.MailOutgoingQueuePath).Select(Path.GetFileName);
		}

		public IEnumerable<IMailReference> GetAllMailForDomain(string domain)
		{
			return Directory.GetFiles(Path.Combine(_settings.MailOutgoingQueuePath, domain), "*", SearchOption.TopDirectoryOnly)
				.Select(path => new Reference(Path.GetFileNameWithoutExtension(path), path));
		}

		public override Task SaveAsync(IMailWriteReference reference, CancellationToken token)
		{
			var writeReference = reference as WriteReference;
			if (writeReference == null)
				throw new ArgumentException("reference must be from NewMailAsync", nameof(reference));

			if (writeReference.Saved) throw new InvalidOperationException("Already saved");

			writeReference.BodyStream.Dispose();
			File.Move(writeReference.TempPath, writeReference.Path);
			writeReference.Saved = true;
			return Task.CompletedTask;
		}
	}
}