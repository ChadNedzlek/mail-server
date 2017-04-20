using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.FileSystem
{
	public abstract class FileSystemMailQueueBase : IMailStore
	{
		protected readonly SmtpSettings Settings;

		protected FileSystemMailQueueBase(SmtpSettings settings)
		{
			Settings = settings;
		}

		public async Task<IMailReadReference> OpenReadAsync(IMailReference reference, CancellationToken token)
		{
			var mailReference = reference as Reference;
			if (mailReference == null)
			{
				throw new ArgumentNullException(nameof(reference));
			}

			using (Sharable<FileStream> stream = Sharable.Create(File.OpenRead(mailReference.Path)))
			{
				string sender;
				var recipients = new List<string>();
				using (var reader = new StreamReader(stream.Peek(), Encoding.UTF8, false, 1024, true))
				{
					string fromLine = await reader.ReadLineAsync();
					if (!fromLine.StartsWith("FROM:"))
					{
						throw new FormatException("Invalid mail file format, expected FROM line");
					}

					sender = fromLine.Substring(5);

					while (true)
					{
						string line = await reader.ReadLineAsync();
						if (line.StartsWith("-----"))
						{
							break;
						}

						if (line.StartsWith("TO:"))
						{
							recipients.Add(line.Substring(3));
							continue;
						}

						throw new FormatException("Invalid mail file format, expected TO: line or Begin Message");
					}
				}

				return new ReadReference(mailReference.Id, sender, recipients, mailReference.Path, stream.TakeValue(), this);
			}
		}

		public Task DeleteAsync(IMailReference reference)
		{
			var mailReference = reference as IReference;
			if (mailReference != null)
			{
				if (File.Exists(mailReference.Path))
				{
					File.Delete(mailReference.Path);
				}

				try
				{
					Directory.Delete(Path.GetDirectoryName(mailReference.Path));
				}
				catch (Exception)
				{
					// Don't care, we were just cleaning up after ourselves, after all
				}
			}

			return Task.FromResult((object) null);
		}

		public abstract Task SaveAsync(IWritable item, CancellationToken token);

		protected async Task<IMailWriteReference> CreateWriteReference(
			string sender,
			CancellationToken token,
			IEnumerable<string> firstGroup,
			Func<string, string> getPathFromName)
		{
			IEnumerable<string> targetRecipients = firstGroup;

			string mailName = Guid.NewGuid().ToString("D");

			string targetPath = getPathFromName(mailName);

			Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

			string tempPath = Path.Combine(Path.GetTempPath(), mailName);

			using (Sharable<FileStream> shared = Sharable.Create(File.Create(tempPath)))
			{
				IEnumerable<string> enumerable = targetRecipients.ToList();
				using (var writer = new StreamWriter(shared.Peek(), Encoding.UTF8, 1024, true))
				{
					await writer.WriteLineAsync($"FROM:{sender}");
					foreach (string recipient in enumerable)
					{
						token.ThrowIfCancellationRequested();
						await writer.WriteLineAsync($"TO:{recipient}");
					}
					await writer.WriteLineAsync("----- BEGIN MESSAGE -----");
				}

				return new WriteReference(
					mailName,
					tempPath,
					targetPath,
					sender,
					enumerable,
					new OffsetStream(shared.TakeValue()),
					this);
			}
		}

		private interface IReference
		{
			string Path { get; }
		}

		protected class Reference : IMailReference, IReference
		{
			public Reference(string id, string path)
			{
				Path = path;
				Id = id;
			}

			public string Id { get; }
			public string Path { get; }
		}

		private class ReadReference : IMailReadReference, IReference
		{
			public ReadReference(
				string id,
				string sender,
				IEnumerable<string> recipients,
				string path,
				Stream bodyStream,
				IMailStore store)
			{
				Id = id;
				BodyStream = bodyStream;
				Store = store;
				Path = path;
				Sender = sender;
				Recipients = ImmutableList.CreateRange(recipients);
			}

			public Stream BodyStream { get; }

			public string Sender { get; }
			public IImmutableList<string> Recipients { get; }
			public IMailStore Store { get; }

			public string Id { get; }

			public void Dispose()
			{
				BodyStream?.Dispose();
			}

			public string Path { get; }
		}

		protected class WriteReference : MailWriteReference, IReference
		{
			public WriteReference(
				string id,
				string tempPath,
				string path,
				string sender,
				IEnumerable<string> recipients,
				Stream bodyStream,
				IMailStore store)
				: base(id, sender, recipients, store)
			{
				BodyStream = bodyStream;
				TempPath = tempPath;
				Path = path;
			}

			public override Stream BodyStream { get; }

			public string TempPath { get; }
			public bool Saved { get; set; }
			public string Path { get; }

			public override void Dispose()
			{
				if (!Saved && File.Exists(TempPath))
				{
					File.Delete(TempPath);
				}
			}
		}
	}
}
