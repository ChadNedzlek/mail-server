using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Utility;

namespace Vaettir.Mail.Server
{
	[Injected]
	public sealed class MailDispatcher : IDisposable
	{
		private readonly IDomainSettingResolver _domainResolver;
		private readonly SpamAssassin _spamAssassin;
		private readonly IMailQueue _incoming;
		private readonly ILogger _log;
		private readonly IMailboxDeliveryStore _delivery;
		private readonly IVolatile<AgentSettings> _settings;
		private readonly IMailTransferQueue _transfer;

		private Dictionary<string, Lazy<IVolatile<DomainSettings>>> _domainSettings;

		public MailDispatcher(
			IMailQueue incoming,
			IMailboxDeliveryStore delivery,
			IMailTransferQueue transfer,
			ILogger log,
			IDomainSettingResolver domainResolver,
			SpamAssassin spamAssassin,
			IVolatile<AgentSettings> settings)
		{
			_settings = settings;
			_incoming = incoming;
			_delivery = delivery;
			_transfer = transfer;
			_log = log;
			_domainResolver = domainResolver;
			_spamAssassin = spamAssassin;

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

		private void UpdateDomains(object sender, AgentSettings newvalue, AgentSettings oldvalue)
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

				if (oldSettings != null)
				{
					foreach (Lazy<IVolatile<DomainSettings>> s in oldSettings.Values)
					{
						if (s.IsValueCreated)
						{
							s.Value?.Dispose();
						}
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
				catch (Exception e)
				{
					_log.Warning($"Failed to get open read reference, deleting message: {e}");
					try
					{
						await _incoming.DeleteAsync(reference);
					}
					catch (Exception deleteException)
					{
						_log.Error($"Failed to delete message: {deleteException}");
					}

					continue;
				}

				try
				{
					_log.Verbose($"Processing mail {readReference.Id} from {readReference.Sender}");


					Stream bodyStream;
					using (readReference)
					using (bodyStream = readReference.BodyStream)
					{
						var currentSettings = _settings.Value;
						if (currentSettings.IncomingScan?.SpamAssassin != null && _spamAssassin != null)
						{
							bodyStream = await _spamAssassin.ScanAsync(readReference, bodyStream);
						}

						if (bodyStream == null)
						{
							// Something told us not to deliver this mail
							_log.Information($"Mail {readReference.Id} was rejected by incoming scan, deleting");
						}
						else
						{
							await DispatchSingleMailReferenceAsync(readReference, bodyStream, token);
						}
					}

					_log.Verbose($"Processing mail {readReference.Id} complete. Deleting incoming item...");

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

		private async Task DispatchSingleMailReferenceAsync(
			IMailReadReference readReference,
			Stream bodyStream,
			CancellationToken token)
		{
			IDictionary<string, IEnumerable<string>> headers =
				await MailUtilities.ParseHeadersAsync(bodyStream);
			ISet<string> recipients =
				AugmentRecipients(readReference.Sender, readReference.Recipients, headers);

			if (!recipients.Any())
			{
				_log.Warning($"{readReference.Id} had no recipients");
			}

			bodyStream.Seek(0, SeekOrigin.Begin);
			IWritable[] dispatchReferences = await CreateDispatchesAsync(
				readReference.Id,
				recipients,
				readReference.Sender,
				token);

			if (!dispatchReferences.Any())
			{
				_log.Warning($"Failed to locate any processor for {readReference.Id}");
			}

			using (var targetStream = new MultiStream(dispatchReferences.Select(r => r.BodyStream)))
			{
				await bodyStream.CopyToAsync(targetStream, token);
			}

			await Task.WhenAll(dispatchReferences.Select(r => r.Store.SaveAsync(r, token)));
		}

		private Task<IWritable[]> CreateDispatchesAsync(
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
								_log.Information($"{mailId} is local mail found");
								return g.Select(r => _delivery.NewMailAsync(mailId, r, token).Cast<IMailboxItemWriteReference, IWritable>());
							}

							if (_settings.Value.RelayDomains.Any(d => string.Equals(d.Name, g.Key, StringComparison.OrdinalIgnoreCase)) ||
								_settings.Value.DomainName == senderDomain)
							{
								_log.Information($"{mailId} is related to {g.Key}");
								return _transfer.NewMailAsync(mailId, sender, g.ToImmutableList(), token)
									.Cast<IMailWriteReference, IWritable>()
									.ToEnumerable();
							}

							_log.Error($"Invalid domain {g.Key}");
							return Enumerable.Empty<Task<IWritable>>();
						}
					)
			);
		}

		private ISet<string> AugmentRecipients(
			string from,
			IEnumerable<string> originalRecipients,
			IDictionary<string, IEnumerable<string>> headers)
		{
			var excludedFromExpansion = new HashSet<string>();
			string[] alreadyOnThreadHeaders =
			{
				"To",
				"Cc",
				"Bcc"
			};

			foreach (string header in alreadyOnThreadHeaders)
			{
				if (headers.TryGetValue(header, out IEnumerable<string> targets))
				{
					foreach (string mbox in ParseMailboxListHeader(targets))
					{
						excludedFromExpansion.Add(mbox);
					}
				}
			}

			excludedFromExpansion.Add(from);
			ISet<string> expandedRecipients = ExpandDistributionLists(from, originalRecipients, excludedFromExpansion);
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
				if (_domainSettings.TryGetValue(
					MailUtilities.GetDomainFromMailbox(recipient),
					out Lazy<IVolatile<DomainSettings>> settings))
				{
					domain = settings.Value.Value;
				}

				DistributionList distributionList = domain?.DistributionLists?.FirstOrDefault(dl => dl.Mailbox == recipient);
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
					// C# compiler mistake that prevents collapsing the domain.Alias null check
					if (domain?.Aliases != null && domain.Aliases.TryGetValue(recipient, out string newRecipient))
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
