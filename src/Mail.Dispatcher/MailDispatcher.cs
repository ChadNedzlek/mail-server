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

namespace Vaettir.Mail.Dispatcher
{
	[UsedImplicitly]
	public sealed class MailDispatcher : IDisposable
	{
		private readonly IDomainSettingResolver _domainResolver;
		private readonly IMailQueue _incoming;
		private readonly ILogger _log;
		private readonly IMailBoxStore _mailBox;
		private readonly IVolatile<SmtpSettings> _settings;
		private readonly IMailTransferQueue _transfer;

		private Dictionary<string, Lazy<IVolatile<DomainSettings>>> _domainSettings;

		public MailDispatcher(
			IMailQueue incoming,
			IMailBoxStore mailBox,
			IMailTransferQueue transfer,
			ILogger log,
			IDomainSettingResolver domainResolver,
			IVolatile<SmtpSettings> settings)
		{
			_settings = settings;
			_incoming = incoming;
			_mailBox = mailBox;
			_transfer = transfer;
			_log = log;
			_domainResolver = domainResolver;

			_settings.ValueChanged += UpdateDomains;
			UpdateDomains(null, _settings.Value, null);
		}

		public void Dispose()
		{
			foreach (Lazy<IVolatile<DomainSettings>> s in _domainSettings.Values)
			{
				if (s.IsValueCreated)
				{
					s.Value?.Dispose();
				}
			}
		}

		private void UpdateDomains(object sender, SmtpSettings newvalue, SmtpSettings oldvalue)
		{
			try
			{
				var newSettings = new Dictionary<string, Lazy<IVolatile<DomainSettings>>>();
				foreach (SmtpAcceptDomain d in newvalue.LocalDomains)
				{
					newSettings.Add(d.Name, new Lazy<IVolatile<DomainSettings>>(() => GetDomainSettings(d)));
				}

				Dictionary<string, Lazy<IVolatile<DomainSettings>>> oldSettings =
					Interlocked.Exchange(ref _domainSettings, newSettings);

				foreach (Lazy<IVolatile<DomainSettings>> s in oldSettings.Values)
				{
					if (s.IsValueCreated)
					{
						s.Value?.Dispose();
					}
				}
			}
			catch (Exception e)
			{
				_log.Error($"Failed to load domain settings files for {newvalue.DomainName}: {e}");
			}
		}

		private IVolatile<DomainSettings> GetDomainSettings(SmtpAcceptDomain smtpAcceptDomain)
		{
			try
			{
				return _domainResolver.GetDomainSettings(smtpAcceptDomain.Name);
			}
			catch (Exception e)
			{
				_log.Error($"Failed to load domain settings file for {smtpAcceptDomain.Name}: {e}");
				return null;
			}
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

			foreach (IMailReference reference in mailReferences)
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
					using (Stream bodyStream = readReference.BodyStream)
					{
						IDictionary<string, IEnumerable<string>> headers = await MailUtilities.ParseHeadersAsync(bodyStream, token);
						ISet<string> recipients = AugmentRecipients(readReference.Sender, readReference.Recipients, headers);
						bodyStream.Seek(0, SeekOrigin.Begin);
						IMailWriteReference[] dispatchReferenecs = await CreateDispatchesAsync(readReference.Id, recipients, readReference.Sender, token);

						using (var targetStream = new MultiStream(dispatchReferenecs.Select(r => r.BodyStream)))
						{
							await bodyStream.CopyToAsync(targetStream);
						}

						await Task.WhenAll(dispatchReferenecs.Select(r => r.Store.SaveAsync(r, token)));
					}

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

		public Task<IMailWriteReference[]> CreateDispatchesAsync(
			string mailId,
			IEnumerable<string> recipients,
			string sender,
			CancellationToken token)
		{
			string senderDomain = MailUtilities.GetDomainFromMailbox(sender);
			return Task.WhenAll(
				recipients
					.GroupBy(MailUtilities.GetDomainFromMailbox)
					.SelectMany(
						g =>
						{
							if (_settings.Value.DomainName == g.Key)
							{
								return g.Select(r => _mailBox.NewMailAsync(r, token));
							}

							if (_settings.Value.RelayDomains.Any(d => string.Equals(d.Name, g.Key, StringComparison.OrdinalIgnoreCase)) ||
								_settings.Value.DomainName == senderDomain)
							{
								return _transfer.NewMailAsync(mailId, sender, g.ToImmutableList(), token).ToEnumerable();
							}

							_log.Error($"Invalid domain {g.Key}");
							return Enumerable.Empty<Task<IMailWriteReference>>();
						}
					)
			);
		}

		public ISet<string> AugmentRecipients(
			string from,
			IEnumerable<string> originalRecipients,
			IDictionary<string, IEnumerable<string>> headers)
		{
			var excludedFromExpansion = new HashSet<string>();
			string[] alreadOnThreadHeaders =
			{
				"To",
				"From",
				"Sender",
				"Reply-To",
				"Cc",
				"Bcc"
			};

			foreach (string header in alreadOnThreadHeaders)
			{
				if (headers.TryGetValue(header, out var targets))
				{
					foreach (string mbox in ParseMailboxListHeader(targets))
					{
						excludedFromExpansion.Add(mbox);
					}
				}
			}

			excludedFromExpansion.Add(from);
			ISet<string> expandedRecipients = ExpandDistributionLists(from, originalRecipients, excludedFromExpansion);
			expandedRecipients.Remove(from);
			return expandedRecipients;
		}

		public ISet<string> ExpandDistributionLists(
			string sender,
			IEnumerable<string> originalRecipients,
			HashSet<string> excludedFromExpansion)
		{
			var to = new HashSet<string>();
			foreach (string recipient in originalRecipients)
			{
				DomainSettings domain = null;
				if (_domainSettings.TryGetValue(MailUtilities.GetDomainFromMailbox(recipient), out var settings))
				{
					domain = settings.Value.Value;
				}

				DistributionList distributionList = domain?.DistributionLists.FirstOrDefault(dl => dl.Mailbox == recipient);
				if (distributionList != null)
				{
					if (!CheckValidSender(sender, distributionList))
					{
						_log.Warning($"Attempt by invalid sender {sender} to send to DL {distributionList.Mailbox}");
						continue;
					}

					IEnumerable<string> nonExcludedMembers = distributionList.Members.Where(m => !excludedFromExpansion.Contains(m));
					foreach (string member in nonExcludedMembers)
					{
						to.Add(member);
					}
					continue;
				}

				{
					if (domain != null && domain.Aliases.TryGetValue(recipient, out var newRecipient))
					{
						if (excludedFromExpansion.Contains(newRecipient))
						{
							continue;
						}
						to.Add(newRecipient);
						continue;
					}
				}

				to.Add(recipient);
			}

			return to;
		}

		public static bool CheckValidSender(string sender, DistributionList distributionList)
		{
			if (!distributionList.Enabled)
			{
				return false;
			}

			if (!distributionList.AllowExternalSenders &&
				MailUtilities.GetDomainFromMailbox(sender) != MailUtilities.GetDomainFromMailbox(distributionList.Mailbox))
			{
				return false;
			}

			return true;
		}


		public IEnumerable<string> ParseMailboxListHeader(IEnumerable<string> header)
		{
			return header
				.SelectMany(h => h.SplitQuoted(',', '"', '\\', StringSplitOptions.None))
				.Select(address => MailUtilities.GetMailboxFromAddress(address, _log))
				.Where(m => !string.IsNullOrEmpty(m));
		}
	}
}
