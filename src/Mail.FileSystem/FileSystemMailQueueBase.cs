using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.FileSystem
{
	public abstract class FileSystemMailQueueBase : IMailStore
	{
		private const string HeaderLengthHeader = "Header-Length:";
		protected readonly AgentSettings Settings;

		protected FileSystemMailQueueBase(AgentSettings settings)
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
				FileStream streamImpl = stream.Peek();
				// 23 ASCII characters, up to 1 GB in size, should be sufficient
				// Header-Legnth:000000000
				string headerSizeHeader = Encoding.ASCII.GetString(await streamImpl.ReadExactlyAsync(23, token));

				if (!headerSizeHeader.StartsWith(HeaderLengthHeader))
				{
					throw new FormatException($"Invalid mail file format, expected {HeaderLengthHeader} line");
				}

				if (!int.TryParse(headerSizeHeader.Substring(HeaderLengthHeader.Length), out int headerSize) || headerSize <= 0)
				{
					throw new FormatException($"Invalid mail file format, {HeaderLengthHeader} is not a valid number");
				}

				using (var reader = new StreamReader(new StreamSpan(streamImpl, 0, headerSize), Encoding.UTF8, false, 1, true))
				{
					// This should be the "new line" at the end of the Header-Length we've already consumed
					// so all that is left is the rest of the line (which is empty).
					// If it's not, then we didn't read what we thought we read, bail
					string blankLine = await reader.ReadLineAsync();
					if (!string.IsNullOrEmpty(blankLine))
					{
						throw new FormatException($"Invalid mail file format, {HeaderLengthHeader} is improperly formatted");
					}

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

				return new ReadReference(
					mailReference.Id,
					sender,
					recipients,
					mailReference.Path,
					new StreamSpan(stream.TakeValue()),
					this);
			}
		}

		public virtual Task DeleteAsync(IMailReference reference)
		{
			if (reference is IReference mailReference)
			{
				if (File.Exists(mailReference.Path))
				{
					File.Delete(mailReference.Path);
				}
			}

			return Task.CompletedTask;
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

			// ReSharper disable AssignNullToNotNullAttribute
			Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
			Directory.CreateDirectory(Path.GetDirectoryName(tempPath));
			// ReSharper restore AssignNullToNotNullAttribute

			using (Sharable<FileStream> shared =
				Sharable.Create(File.Open(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read)))
			{
				FileStream stream = shared.Peek();
				IEnumerable<string> enumerable = targetRecipients.ToList();
				// Save room for header length
				using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true))
				{
					await writer.WriteLineAsync($"{HeaderLengthHeader}000000000");
					await writer.WriteLineAsync($"FROM:{sender}");
					foreach (string recipient in enumerable)
					{
						token.ThrowIfCancellationRequested();
						await writer.WriteLineAsync($"TO:{recipient}");
					}

					await writer.FlushAsync();
					var location = (int) stream.Position;
					stream.Seek(0, SeekOrigin.Begin);
					await writer.FlushAsync();
					await writer.WriteLineAsync($"{HeaderLengthHeader}{location:D9}");
					await writer.FlushAsync();
					stream.Seek(location, SeekOrigin.Begin);
				}

				return new WriteReference(
					mailName,
					tempPath,
					targetPath,
					sender,
					enumerable,
					new StreamSpan(shared.TakeValue()),
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
