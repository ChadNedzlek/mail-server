using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Test.Utilities;
using Vaettir.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Vaettir.Mail.Dispatcher.Test
{
	public class MailDispatcherTests
	{
		public MailDispatcherTests(ITestOutputHelper output)
		{
			_queue = new MockMailQueue();
			_transfer = new MockMailTransferQueue();
			_mailbox = new MockMailboxStore();
			_domainSettings = new DomainSettings(
				new[]
				{
					new DistributionList(
						"int-dl@example.com",
						"Internal only list",
						new[] {"int-dl-owner@example.com"},
						new[] {"int-dl-member1@example.com", "int-dl-member2@example.com"},
						false,
						true
					),
					new DistributionList(
						"disabled-dl@example.com",
						"Disabled list",
						new[] {"disabled-dl-owner@example.com"},
						new[] {"disabled-dl-member1@example.com", "disabled-dl-member2@example.com"},
						false,
						false
					),
					new DistributionList(
						"ext-dl@example.com",
						"External list",
						new[] {"ext-dl-owner@example.com"},
						new[]
						{
							"int-dl-member1@example.com",
							"ext-dl-member1@example.com",
							"ext-dl-member2@example.com",
							"ext-dl-member@example.net"
						},
						true,
						true
					)
				},
				new Dictionary<string, string>
				{
					{"alias-1@example.com", "box@example.com"}
				});
			_settings = TestHelpers.MakeSettings(
				domainName: "example.com",
				localDomains: new[] {new SmtpAcceptDomain("example.com")},
				relayDomains: new[] {new SmtpRelayDomain("relay.example.com", "relay.example.com")},
				idleDelay: 1);
			_dispatcher = new MailDispatcher(
				_queue,
				_mailbox,
				_transfer,
				new TestOutputLogger(output),
				new MockDomainResolver(_domainSettings),
				new MockVolatile<AgentSettings>(_settings));
		}

		private readonly MailDispatcher _dispatcher;
		private readonly DomainSettings _domainSettings;
		private readonly MockMailQueue _queue;
		private readonly AgentSettings _settings;
		private readonly MockMailTransferQueue _transfer;
		private readonly MockMailboxStore _mailbox;

		[Fact]
		public async Task IgnoresNonRelayDomain()
		{
			_queue.References.Add(
				new MockMailReference(
					"ignored",
					"box@external.example.com",
					new[] {"box@external.example.com"}.ToImmutableList(),
					true,
					"My body\nNext Line",
					_queue));

			await _dispatcher.ProcessAllMailReferencesAsync(CancellationToken.None);

			Assert.Empty(_queue.References);
			Assert.Equal(1, _queue.DeletedReferences.Count);
			Assert.Empty(_transfer.References);
			Assert.Empty(_mailbox.References);
		}

		[Fact]
		public async Task InternalSenderCanSendAnywhere()
		{
			var body = "My body\nNext Line";
			_queue.References.Add(
				new MockMailReference(
					"ext-mail",
					"box@example.com",
					new[] {"box@external.example.com"}.ToImmutableList(),
					true,
					body,
					_queue));

			await _dispatcher.ProcessAllMailReferencesAsync(CancellationToken.None);

			Assert.Empty(_queue.References);
			Assert.Equal(1, _queue.DeletedReferences.Count);
			Assert.Equal(1, _transfer.References.Count(r => r.IsSaved));
			string newBody;
			using (MockMailReference reference = _transfer.References.FirstOrDefault(r => r.IsSaved))
			{
				Assert.Equal("box@example.com", reference.Sender);
				SequenceAssert.SameSet(new[] {"box@external.example.com"}, reference.Recipients);
				newBody = await StreamUtility.ReadAllFromStreamAsync(reference.BackupBodyStream);
			}
			Assert.Equal(body, newBody);
			Assert.Empty(_mailbox.References);
		}

		[Fact]
		public void ParseMailboxListHeader_HeaderParsing_Single()
		{
			SequenceAssert.SameSet(
				new[] {"box@example.com"},
				_dispatcher.ParseMailboxListHeader("box@example.com".ToEnumerable()));
		}

		[Fact]
		public void ParseMailboxListHeader_MultiMulti()
		{
			SequenceAssert.SameSet(
				new[] {"box@example.com", "otherguy@example.com", "a@example.com", "b@example.com"},
				_dispatcher.ParseMailboxListHeader(
					new[]
					{
						"\"Box, The\" <box@example.com>, \"Guy, Other\" <otherguy@example.com>",
						"a@example.com, b@example.com"
					}));
		}

		[Fact]
		public void ParseMailboxListHeader_MultiSingleQuoted()
		{
			SequenceAssert.SameSet(
				new[] {"box@example.com", "otherguy@example.com"},
				_dispatcher.ParseMailboxListHeader(
					"\"Box, The\" <box@example.com>, \"Guy, Other\" <otherguy@example.com>".ToEnumerable()));
		}

		[Fact]
		public void ResolveDomains_Alias()
		{
			SequenceAssert.SameSet(
				new[] {"box@example.com"},
				_dispatcher.ExpandDistributionLists("sender@example.com", new[] {"alias-1@example.com"}, new HashSet<string>()));
		}

		[Fact]
		public void ResolveDomains_Expands()
		{
			SequenceAssert.SameSet(
				new[] {"int-dl-member1@example.com", "int-dl-member2@example.com"},
				_dispatcher.ExpandDistributionLists("sender@example.com", new[] {"int-dl@example.com"}, new HashSet<string>()));
		}

		[Fact]
		public void ResolveDomains_ExpandsKeepsOthers()
		{
			SequenceAssert.SameSet(
				new[] {"int-dl-member1@example.com", "int-dl-member2@example.com", "single@example.com"},
				_dispatcher.ExpandDistributionLists(
					"sender@example.com",
					new[] {"int-dl@example.com", "single@example.com"},
					new HashSet<string>()));
		}

		[Fact]
		public void ResolveDomains_NoResolve()
		{
			SequenceAssert.SameSet(
				new[] {"single@example.com"},
				_dispatcher.ExpandDistributionLists("sender@example.com", new[] {"single@example.com"}, new HashSet<string>()));
		}

		[Fact]
		public void ResolveDomains_RespectExclusion()
		{
			SequenceAssert.SameSet(
				new[] {"int-dl-member2@example.com"},
				_dispatcher.ExpandDistributionLists(
					"sender@example.com",
					new[] {"int-dl@example.com"},
					new HashSet<string> {"int-dl-member1@example.com"}));
		}

		[Fact]
		public async Task SendsSingleMailToRelay()
		{
			var body = "My body\nNext Line";
			_queue.References.Add(
				new MockMailReference(
					"ext-mail",
					"box@external.example.com",
					new[] {"box@relay.example.com", "other@relay.example.com"}.ToImmutableList(),
					true,
					body,
					_queue));

			await _dispatcher.ProcessAllMailReferencesAsync(CancellationToken.None);

			Assert.Empty(_queue.References);
			Assert.Equal(1, _queue.DeletedReferences.Count);
			Assert.Equal(1, _transfer.References.Count(r => r.IsSaved));
			string newBody;
			using (MockMailReference reference = _transfer.References.FirstOrDefault(r => r.IsSaved))
			{
				Assert.Equal("box@external.example.com", reference.Sender);
				SequenceAssert.SameSet(new[] {"box@relay.example.com", "other@relay.example.com"}, reference.Recipients);
				newBody = await StreamUtility.ReadAllFromStreamAsync(reference.BackupBodyStream);
			}
			Assert.Equal(body, newBody);
			Assert.Empty(_mailbox.References);
		}

		[Fact]
		public async Task SplitIntoMailboxes()
		{
			var body = "My body\nNext Line";
			_queue.References.Add(
				new MockMailReference(
					"int-mail",
					"senderbox@external.example.com",
					new[] {"box@example.com", "other@example.com"}.ToImmutableList(),
					true,
					body,
					_queue));

			await _dispatcher.ProcessAllMailReferencesAsync(CancellationToken.None);

			Assert.Empty(_queue.References);
			Assert.Empty(_transfer.References);
			Assert.Equal(2, _mailbox.SavedReferences.Count());
			var expected = new HashSet<string> {"box@example.com", "other@example.com"};
			foreach (MockMailboxItemReference r in _mailbox.SavedReferences)
			{
				Assert.True(expected.Contains(r.Mailbox));
				expected.Remove(r.Mailbox);
				Assert.Equal(body, await StreamUtility.ReadAllFromStreamAsync(r.BackupBodyStream));
				r.Dispose();
			}
		}

		[Fact]
		public void ValidateSender_ExternalSenderExternalList()
		{
			Assert.True(
				MailDispatcher.CheckValidSender(
					"box@example.net",
					_domainSettings.DistributionLists.First(dl => dl.Mailbox == "ext-dl@example.com")));
		}

		[Fact]
		public void ValidateSender_ExternalSenderInternalList()
		{
			Assert.False(
				MailDispatcher.CheckValidSender(
					"box@example.net",
					_domainSettings.DistributionLists.First(dl => dl.Mailbox == "int-dl@example.com")));
		}

		[Fact]
		public void ValidateSender_InternalSenderExternalList()
		{
			Assert.True(
				MailDispatcher.CheckValidSender(
					"box@example.com",
					_domainSettings.DistributionLists.First(dl => dl.Mailbox == "ext-dl@example.com")));
		}

		[Fact]
		public void ValidateSender_InternalSenderInternalList()
		{
			Assert.True(
				MailDispatcher.CheckValidSender(
					"box@example.com",
					_domainSettings.DistributionLists.First(dl => dl.Mailbox == "int-dl@example.com")));
		}
	}
}
