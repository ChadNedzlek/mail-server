using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.FileSystem
{
	public class FileSystemMailStore : IMailStore
	{
		private readonly string _mailDirectory;

		public FileSystemMailStore(string mailDirectory)
		{
			_mailDirectory = mailDirectory;
		}

		public async Task<Stream> GetNewMailStreamAsync(string sender, IEnumerable<string> recipients, CancellationToken token)
		{
			Guid guid = Guid.NewGuid();
			using (var sharable = Sharable.Create(File.Open(Path.Combine(_mailDirectory, guid.ToString("D")), FileMode.CreateNew, FileAccess.Write, FileShare.None)))
			{
				using (var writer = new StreamWriter(sharable.Peek(), Encoding.UTF8, 1024, true))
				{
					await writer.WriteLineAsync($"FROM: {sender}");
					foreach (var recipient in recipients)
					{
						await writer.WriteLineAsync($"TO: {recipient}");
					}
					await writer.WriteLineAsync("--- BEGIN MESSAGE ---");
				}

				return sharable.TakeValue();
			}
		}
	}
}
