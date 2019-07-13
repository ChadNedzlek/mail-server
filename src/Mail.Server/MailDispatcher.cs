using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Mime;
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
			string sender = readReference.Sender;

			IDictionary<string, IEnumerable<string>> headers =
				await MailUtilities.ParseHeadersAsync(bodyStream);
			ISet<string> recipients =
				AugmentRecipients(sender, readReference.Recipients, headers);

			if (!recipients.Any())
			{
				_log.Warning($"{readReference.Id} had no recipients");
			}

			(bodyStream, sender) = await ReplaceSenderAsync(headers, bodyStream, sender, token);

			IWritable[] dispatchReferences = await CreateDispatchesAsync(
				readReference.Id,
				recipients,
				sender,
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

		private static readonly Regex ReferenceOriginalSender = new Regex(@"<vaettir\.net:original-sender:([^>]+)>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex ReplaceFrom = new Regex(@"(?<=^|\n)(From:\s*[^<]*)<[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex ReplaceReferences = new Regex(@"
References:                            # 'References:' header
(?<pre>(?:.|\ |\r\n\ )*?)              # text, or cfws that comes before the original-sender bit, captured
(?<preSpace>(?:\ |\r\n\ )*)            # cfws before our target header to replace
<vaettir\.net:original-sender:[^>]+>   # our target to replace
(?<postSpace>(?:\ |\r\n\ )*)           # cfws before our target header to replace
(?<post>                               # capture the...
	(?:.|\ |\r\n\ )*                   # text, or cfws that comes after the original-sender bit
	\r\n                               # and also the new line that ends our header
)
", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);

		/// <summary>
		/// If "me@example.com" is replying to something sent to "me-other@example.com",
		/// replace the "me@example" in the mail
		/// </summary>
		public static async Task<(Stream, string)> ReplaceSenderAsync(
			IDictionary<string, IEnumerable<string>> headers,
			Stream bodyStream,
			string sender,
			CancellationToken token)
		{
			bodyStream.Seek(0, SeekOrigin.Begin);

			if (!headers.TryGetValue("References", out IEnumerable<string> referenes))
			{
				return (bodyStream, sender);
			}

			foreach (var r in referenes)
			{
				Match match = ReferenceOriginalSender.Match(r);
				if (match.Success)
				{
					var senderParts = sender.Split('@', 2);
					if (senderParts.Length != 2)
					{
						// Not a useful email address...
						continue;
					}

					var headerSender = match.Groups[1].Value;
					var headerParts = headerSender.Split('@', 2);
					if (headerParts.Length != 2)
					{
						// Not a useful email address...
						continue;
					}

					if (senderParts[1] != headerParts[1])
					{
						// Wrong domain...
						continue;
					}

					if (!headerParts[0].StartsWith(senderParts[0] + "-"))
					{
						// Doesn't start with correct extension root
						continue;
					}

					MemoryStream replacedStream = null;
					try
					{
						replacedStream = new MemoryStream();

						MimeReader reader = new MimeReader();
						var structure = await reader.ReadStructureAsync(bodyStream, token);

						// Copy the preamble
						bodyStream.Seek(0, SeekOrigin.Begin);
						BoundedStream pre = new BoundedStream(bodyStream, structure.HeaderSpan.Start);
						await pre.CopyToAsync(replacedStream, token);

						// Replace stuff in headers
						bodyStream.Seek(structure.HeaderSpan.Start, SeekOrigin.Begin);
						var headerBytes = await bodyStream.ReadExactlyAsync((int) structure.HeaderSpan.Length, token);
						string header = Encoding.ASCII.GetString(headerBytes);
						header = ReplaceFrom.Replace(header, $"$1<{headerSender}>");
						header = ReplaceReferences.Replace(header,
							m =>
							{
								if (String.IsNullOrWhiteSpace(m.Groups["pre"].Value) &&
									String.IsNullOrWhiteSpace(m.Groups["post"].Value))
								{
									// We were the ONLY references header, just remove the whole thing
									return "";
								}

								string space = "";
								if (!String.IsNullOrEmpty(m.Groups["preSpace"].Value) &&
									!String.IsNullOrEmpty(m.Groups["postSpace"].Value))
								{
									space = m.Groups["preSpace"].Value;
								}

								return $"References:{m.Groups["pre"].Value}{space}{m.Groups["post"].Value}";
							});

						using (StreamWriter writer = new StreamWriter(replacedStream, Encoding.ASCII, 1024, true))
						{
							writer.Write(header);
						}

						// Copy message content
						bodyStream.Seek(structure.HeaderSpan.End, SeekOrigin.Begin);
						await bodyStream.CopyToAsync(replacedStream, token);
						bodyStream.Dispose();
						replacedStream.Seek(0, SeekOrigin.Begin);

						return (replacedStream, headerSender);
					}
					catch
					{
						replacedStream?.Dispose();
						throw;
					}
				}
			}

			return (bodyStream, sender);
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
