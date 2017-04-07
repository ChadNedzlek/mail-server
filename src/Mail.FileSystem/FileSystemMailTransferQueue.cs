using System;
using System.Collections.Generic;
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

		public Task<IMailWriteReference> NewMailAsync(IEnumerable<string> recipients, string sender, CancellationToken token)
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

		public IEnumerable<string> GetMailsByDomain()
		{
			return Directory.GetDirectories(_settings.MailOutgoingQueuePath).Select(Path.GetFileName);
		}

		public IEnumerable<IMailReference> GetAllMailForDomain(string domain)
		{
			return Directory.GetFiles(Path.Combine(_settings.MailOutgoingQueuePath, domain), "*", SearchOption.TopDirectoryOnly)
				.Select(path => new Reference(Path.GetFileNameWithoutExtension(path), path));
		}
	}
}