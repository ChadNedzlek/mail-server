using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.FileSystem
{
	[Injected]
	public class FileSystemMailQueue : FileSystemMailQueueBase, IMailQueue
	{
		public FileSystemMailQueue(AgentSettings settings) : base(settings)
		{
			Directory.CreateDirectory(Path.Combine(Settings.MailIncomingQueuePath, "cur"));
			Directory.CreateDirectory(Path.Combine(Settings.MailIncomingQueuePath, "tmp"));
		}

		public Task<IMailWriteReference> NewMailAsync(
			string sender,
			IImmutableList<string> recipients,
			CancellationToken token)
		{
			string GetPathFromName(string name)
			{
				return Path.Combine(GetRootPath("cur"), name);
			}

			string GetTempPathFromName(string name)
			{
				return Path.Combine(GetRootPath("tmp"), name);
			}

			return CreateWriteReference(
				sender,
				token,
				recipients,
				GetTempPathFromName,
				GetPathFromName
			);
		}

		public IEnumerable<IMailReference> GetAllMailReferences()
		{
			return Directory.GetFiles(GetRootPath("cur"), "*", SearchOption.TopDirectoryOnly)
				.Select(path => new Reference(Path.GetFileNameWithoutExtension(path), path));
		}
		
		public Stream GetTemporaryMailStream(IMailReadReference reference)
		{
			if (reference == null)
			{
				throw new ArgumentNullException(nameof(reference));
			}

			if (!(reference is ReadReference concrete))
			{
				throw new ArgumentException($"Reference must be returned from {GetType().Name}.", nameof(reference));
			}

			return File.Create(Path.Combine(GetRootPath("tmp"), $"{concrete.Id}-{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ss-ffff}"), 1024, FileOptions.DeleteOnClose);
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

			// Check for spam and other income checks at this point

			File.Move(writeReference.TempPath, writeReference.Path);
			writeReference.Saved = true;
			return Task.CompletedTask;
		}

		private string GetRootPath(string type)
		{
			return Path.Combine(Settings.MailIncomingQueuePath, type);
		}
	}
}
