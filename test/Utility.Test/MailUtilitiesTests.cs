using Vaettir.Mail.Server;
using Xunit;

namespace Vaettir.Utility.Test
{
	public class MailUtilitiesTests
	{
		[Fact]
		public void NoAt()
		{
			Assert.Null(MailUtilities.GetMailboxFromAddress("box_example.com"));
		}

		[Fact]
		public void QuotedCrainess()
		{
			Assert.Equal(
				"box@example.com",
				MailUtilities.GetMailboxFromAddress("\"Target Human <wrong@other.com>\" <box@example.com>"));
		}

		[Fact]
		public void SimpleMail()
		{
			Assert.Equal("box@example.com", MailUtilities.GetMailboxFromAddress("box@example.com"));
		}

		[Fact]
		public void SimpleWithDisplayName()
		{
			Assert.Equal("box@example.com", MailUtilities.GetMailboxFromAddress("Target Human <box@example.com>"));
		}
	}
}
