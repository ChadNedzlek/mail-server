using System.Collections.Generic;
using System.Linq;
using Mail.Smtp.Test;
using Utility.Test;
using Vaettir.Mail.Dispatcher;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Mail.Dispatcher.Test
{
	public class MailDispatcherTests
	{
		public MailDispatcherTests(ITestOutputHelper output)
		{
			_domainSettings = new DomainSettings(
				distributionLists: new [] {
					new DistributionList(
						mailbox: "int-dl@example.com",
						description: "Internal only list",
						owners: new []{"int-dl-owner@example.com"},
						members: new []{"int-dl-member1@example.com", "int-dl-member2@example.com"},
						allowExternalSenders: false,
						enabled: true
					),
					new DistributionList(
						mailbox: "disabled-dl@example.com",
						description: "Disabled list",
						owners: new []{"disabled-dl-owner@example.com"},
						members: new []{ "disabled-dl-member1@example.com", "disabled-dl-member2@example.com"},
						allowExternalSenders: false,
						enabled: false
					),
					new DistributionList(
						mailbox: "ext-dl@example.com",
						description: "External list",
						owners: new []{"ext-dl-owner@example.com"},
						members: new []{ "int-dl-member1@example.com", "ext-dl-member1@example.com", "ext-dl-member2@example.com", "ext-dl-member@example.net"},
						allowExternalSenders: true,
						enabled: true
					),
				},
				aliases: new Dictionary<string, string>{
					{"alias-1@example.com", "box@example.com"},
				});
			_dispatcher = new MailDispatcher(
				null,
				null,
				null,
				new TestOutputLogger(output),
				new MockDomainResolver(_domainSettings),
				new MockVolatile<SmtpSettings>(new SmtpSettings()));
		}

		private readonly MailDispatcher _dispatcher;
		private DomainSettings _domainSettings;

		[Fact]
		public void ParseMailboxListHeader_HeaderParsing_Single()
		{
			SequenceAssert.SameSet(
				new[] { "box@example.com" },
				_dispatcher.ParseMailboxListHeader("box@example.com".ToEnumerable()));
		}

		[Fact]
		public void ParseMailboxListHeader_MultiSingleQuoted()
		{
			SequenceAssert.SameSet(
				new[] { "box@example.com", "otherguy@example.com" },
				_dispatcher.ParseMailboxListHeader("\"Box, The\" <box@example.com>, \"Guy, Other\" <otherguy@example.com>".ToEnumerable()));
		}

		[Fact]
		public void ParseMailboxListHeader_MultiMulti()
		{
			SequenceAssert.SameSet(
				new[] { "box@example.com", "otherguy@example.com", "a@example.com", "b@example.com" },
				_dispatcher.ParseMailboxListHeader(new []{
					"\"Box, The\" <box@example.com>, \"Guy, Other\" <otherguy@example.com>",
					"a@example.com, b@example.com",
				}));
		}

		[Fact]
		public void ResolveDomains_NoResolve()
		{
			SequenceAssert.SameSet(
				new[] { "single@example.com" },
				_dispatcher.ExpandDistributionLists("sender@example.com", new[] { "single@example.com" }, new HashSet<string>()));
		}

		[Fact]
		public void ResolveDomains_Expands()
		{
			SequenceAssert.SameSet(
				new[] { "int-dl-member1@example.com", "int-dl-member2@example.com" },
				_dispatcher.ExpandDistributionLists("sender@example.com", new[] { "int-dl@example.com" }, new HashSet<string>()));
		}

		[Fact]
		public void ResolveDomains_ExpandsKeepsOthers()
		{
			SequenceAssert.SameSet(
				new[] { "int-dl-member1@example.com", "int-dl-member2@example.com", "single@example.com" },
				_dispatcher.ExpandDistributionLists("sender@example.com", new[] { "int-dl@example.com", "single@example.com" }, new HashSet<string>()));
		}

		[Fact]
		public void ResolveDomains_Alias()
		{
			SequenceAssert.SameSet(
				new[] { "box@example.com" },
				_dispatcher.ExpandDistributionLists("sender@example.com", new[] { "alias-1@example.com" }, new HashSet<string>()));
		}

		[Fact]
		public void ResolveDomains_RespectExclusion()
		{
			SequenceAssert.SameSet(
				new[] { "int-dl-member2@example.com" },
				_dispatcher.ExpandDistributionLists("sender@example.com", new[] { "int-dl@example.com" }, new HashSet<string>{ "int-dl-member1@example.com" }));
		}

		[Fact]
		public void ValidateSender_InternalSenderInternalList()
		{
			Assert.True(MailDispatcher.CheckValidSender(
				"box@example.com",
				_domainSettings.DistributionLists.First(dl => dl.Mailbox == "int-dl@example.com")));
		}

		[Fact]
		public void ValidateSender_InternalSenderExternalList()
		{
			Assert.True(MailDispatcher.CheckValidSender(
				"box@example.com",
				_domainSettings.DistributionLists.First(dl => dl.Mailbox == "ext-dl@example.com")));
		}

		[Fact]
		public void ValidateSender_ExternalSenderInternalList()
		{
			Assert.False(MailDispatcher.CheckValidSender(
				"box@example.net",
				_domainSettings.DistributionLists.First(dl => dl.Mailbox == "int-dl@example.com")));
		}

		[Fact]
		public void ValidateSender_ExternalSenderExternalList()
		{
			Assert.True(MailDispatcher.CheckValidSender(
				"box@example.net",
				_domainSettings.DistributionLists.First(dl => dl.Mailbox == "ext-dl@example.com")));
		}
	}
}