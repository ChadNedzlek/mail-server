﻿using System;
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
	[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
	public class FileSystemMailQueue : FileSystemMailQueueBase, IMailQueue
	{
		public FileSystemMailQueue(SmtpSettings settings) : base(settings)
		{
			Directory.CreateDirectory(Settings.MailIncomingQueuePath);
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

		private string GetRootPath(string type)
		{
			return Path.Combine(Settings.MailIncomingQueuePath, type);
		}

		public IEnumerable<IMailReference> GetAllMailReferences()
		{
			return Directory.GetFiles(GetRootPath("cur"), "*", SearchOption.TopDirectoryOnly)
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

			// Check for spam and other income checks at this point

			File.Move(writeReference.TempPath, writeReference.Path);
			writeReference.Saved = true;
			return Task.CompletedTask;
		}
	}
}
