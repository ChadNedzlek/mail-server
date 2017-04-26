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
				var headerLength = BitConverter.ToInt32(await stream.Peek().ReadExactlyAsync(4, token), 0);
				using (var reader = new StreamReader(new OffsetStream(stream.Peek(), 4, headerLength), Encoding.UTF8, false, 1, true))
				{
					string fromLine = await reader.ReadLineAsync();
					if (!fromLine.StartsWith("FROM:"))
					{
						throw new FormatException("Invalid mail file format, expected FROM line");
					}

					sender = fromLine.Substring(5);

					string line = null;
					while (await reader.TryReadLineAsync(l => line = l, token))
					{
						if (line.StartsWith("TO:"))
						{
							recipients.Add(line.Substring(3));
							continue;
						}

						throw new FormatException("Invalid mail file format, expected TO: line or Begin Message");
					}
				}

				return new ReadReference(mailReference.Id, sender, recipients, mailReference.Path, new OffsetStream(stream.TakeValue()), this);
			}
		}

		public virtual Task DeleteAsync(IMailReference reference)
		{
			var mailReference = reference as IReference;
			if (mailReference != null)
			{
				if (File.Exists(mailReference.Path))
				{
					File.Delete(mailReference.Path);
				}
			}

			return Task.FromResult((object) null);
		}

		public abstract Task SaveAsync(IWritable item, CancellationToken token);

		protected async Task<IMailWriteReference> CreateWriteReference(
			string sender,
			CancellationToken token,
			IEnumerable<string> recipients,
			Func<string, string> getTempPathFromName,
			Func<string, string> getPathFromName)
		{
			IEnumerable<string> targetRecipients = recipients;

			string mailName = Guid.NewGuid().ToString("D");

			string tempPath = getTempPathFromName(mailName);
			string targetPath = getPathFromName(mailName);

			Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
			Directory.CreateDirectory(Path.GetDirectoryName(tempPath));

			using (Sharable<FileStream> shared = Sharable.Create(File.Create(tempPath)))
			{
				FileStream stream = shared.Peek();
				IEnumerable<string> enumerable = targetRecipients.ToList();
				// Save room for header length
				await stream.WriteAsync(new byte[4], 0, 4, token);
				using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
				{
					await writer.WriteLineAsync($"FROM:{sender}");
					foreach (string recipient in enumerable)
					{
						token.ThrowIfCancellationRequested();
						await writer.WriteLineAsync($"TO:{recipient}");
					}
				}
				// Figure out where the headers end
				int location = (int) stream.Position;
				// Rewind
				stream.Seek(0, SeekOrigin.Begin);
				// Replace the 0's we saved with the real length
				await stream.WriteAsync(BitConverter.GetBytes(location), 0, 4, token);
				// Go back to the right place
				stream.Seek(location, SeekOrigin.Begin);

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

		protected class ReadReference : IMailReadReference, IReference
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
