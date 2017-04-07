using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.FileSystem
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
	public class FileSystemMailQueue : FileSystemMailQueueBase, IMailQueue
	{
		public FileSystemMailQueue(SmtpSettings settings) : base(settings)
		{
		}

		public Task<IMailWriteReference> NewMailAsync(
			string sender,
			IEnumerable<string> recipients,
			CancellationToken token)
		{
			string GetPathFromName(string name)
			{
				return Path.Combine(_settings.MailOutgoingQueuePath, name);
			}

			return CreateWriteReference(
				sender,
				token,
				recipients,
				GetPathFromName
			);
		}

		public IEnumerable<IMailReference> GetAllMailReferences()
		{
			return Directory.GetFiles(_settings.MailIncomingQueuePath, "*", SearchOption.TopDirectoryOnly).Select(path => new Reference(Path.GetFileNameWithoutExtension(path), path));
		}
	}
}
