using System;
using System.Collections.Generic;
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
		private readonly IVolatile<SmtpSettings> _settings;
		private readonly IMailQueue _incoming;
		private readonly IMailBoxStore _mailBox;
		private readonly IMailTransferQueue _transfer;
		private readonly ILogger _log;
		private readonly IDomainSettingResolver _domainResolver;
		private IVolatile<DomainSettings> _domain;

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
			_domain = domainResolver.GetDomainSettings(settings.Value.DomainName);

			_settings.ValueChanged += UpdateDomains;
		}

		private void UpdateDomains(object sender, SmtpSettings newvalue, SmtpSettings oldvalue)
		{
			try
			{
				Interlocked.Exchange(ref _domain, _domainResolver.GetDomainSettings(newvalue.DomainName))?.Dispose();
			}
			catch(Exception e)
			{
				_log.Error($"Failed to load domain settings file for {newvalue.DomainName}: {e}");
			}
		}

		public async Task RunAsync(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				foreach (var reference in _incoming.GetAllMailReferences().ToList())
				{
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
						using (var bodyStream = readReference.BodyStream)
						{
							var headers = await MailUtilities.ParseHeadersAsync(bodyStream, token);
							var recipients = AugmentRecipients(readReference.Sender, readReference.Recipients, headers);
							bodyStream.Seek(0, SeekOrigin.Begin);
							var dispatchReferenecs = await CreateDispatchesAsync(recipients, readReference.Sender, token);

							using (var targetStream = new MultiStream(dispatchReferenecs.Select(r => r.BodyStream)))
							{
								await bodyStream.CopyToAsync(targetStream);
							}
						}

						_log.Verbose($"Processing mamil {readReference.Id} complete. Deleting incoming item...");

						await _incoming.DeleteAsync(reference);
					}
					catch (Exception e)
					{
						_log.Error("Failed to process mail", e);
					}
				}
			}
		}

		public Task<IMailWriteReference[]> CreateDispatchesAsync(IEnumerable<string> recipients, string sender, CancellationToken token)
		{
			return Task.WhenAll(recipients
				.GroupBy(MailUtilities.GetDomainFromMailbox)
				.SelectMany(
					delegate(IGrouping<string, string> g)
					{
						if (_settings.Value.DomainName == g.Key)
							return g.Select(r => _mailBox.NewMailAsync(r, token));

						if (_settings.Value.RelayDomains.Contains(g.Key))
							return _transfer.NewMailAsync(g, sender, token).ToEnumerable();

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
			HashSet<string> excludedFromExpansion = new HashSet<string>();
			string[] alreadOnThreadHeaders =
			{
				"To",
				"From",
				"Sender",
				"Reply-To",
				"Cc",
				"Bcc",
			};

			foreach (string header in alreadOnThreadHeaders)
			{
				IEnumerable<string> targets;
				if (headers.TryGetValue(header, out targets))
				{
					foreach (var mbox in ParseMailboxListHeader(targets))
					{
						excludedFromExpansion.Add(mbox);
					}
				}
			}

			excludedFromExpansion.Add(from);
			var expandedRecipients = ExpandDistributionLists(from, originalRecipients, excludedFromExpansion);
			expandedRecipients.Remove(from);
			return expandedRecipients;
		}

		public ISet<string> ExpandDistributionLists(string sender, IEnumerable<string> originalRecipients, HashSet<string> excludedFromExpansion)
		{
			HashSet<string> to = new HashSet<string>();
			var domain = _domain.Value;
			foreach (var recipient in originalRecipients)
			{
				DistributionList distributionList = domain.DistributionLists.FirstOrDefault(dl => dl.Mailbox == recipient);
				if (distributionList != null)
				{
					if (!CheckValidSender(sender, distributionList))
					{
						_log.Warning($"Attempt by invalid sender {sender} to send to DL {distributionList.Mailbox}");
						continue;
					}

					var nonExcludedMembers = distributionList.Members.Where(m => !excludedFromExpansion.Contains(m));
					foreach (var member in nonExcludedMembers)
					{
						to.Add(member);
					}
					continue;
				}

				{
					string newRecipient;
					if (domain.Aliases.TryGetValue(recipient, out newRecipient))
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
				return false;

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
				.Where(m => !String.IsNullOrEmpty(m));
		}

		public void Dispose()
		{
			_settings?.Dispose();
			_log?.Dispose();
			_domain?.Dispose();
		}
	}
}