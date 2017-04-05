using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Utility;

namespace Vaettir.Mail.Transfer
{
	[UsedImplicitly]
	public sealed class MailTransferDispatcher
	{
		private readonly IVolatile<SmtpSettings> _settings;
		private readonly IMailTransferQueue _incoming;
		private readonly ILogger _log;

		public MailTransferDispatcher(
			IMailTransferQueue incoming,
			IMailBoxStore mailBox,
			ILogger log,
			IVolatile<SmtpSettings> settings)
		{
			_settings = settings;
			_incoming = incoming;
			_log = log;
		}

		public async Task RunAsync(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				await ProcessAllMailReferencesAsync(token);
			}
		}

		public async Task ProcessAllMailReferencesAsync(CancellationToken token)
		{
			List<IMailReference> mailReferences = _incoming.GetAllMailReferences().ToList();

			if (mailReferences.Count == 0)
			{
				int msSleep = _settings.Value.IdleDelay ?? 5000;
				_log.Verbose($"No mail found, sleeping for {msSleep}ms");
				await Task.Delay(msSleep, token);
			}
			token.ThrowIfCancellationRequested();

			foreach (var reference in mailReferences)
			{
				token.ThrowIfCancellationRequested();
				IMailReadReference readReference;

				try
				{
					readReference = await _incoming.OpenReadAsync(reference, token);
				}
				catch (IOException e)
				{
					// It's probably a sharing violation, just try again later.
					_log.Warning($"Failed to get read reference, exception: {e}");
					continue;
				}

				try
				{
					_log.Verbose($"Processing mail {readReference.Id} from {readReference.Sender}");

					using (readReference)
					


					_log.Verbose($"Processing mamil {readReference.Id} complete. Deleting incoming item...");

					await _incoming.DeleteAsync(reference);
				}
				catch (TaskCanceledException)
				{
					throw;
				}
				catch (Exception e)
				{
					_log.Error("Failed to process mail", e);
				}
			}
		}
	}
}