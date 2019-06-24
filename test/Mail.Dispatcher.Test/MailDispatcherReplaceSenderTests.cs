using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Xunit;

namespace Vaettir.Mail.Dispatcher.Test
{
	public class MailDispatcherReplaceSenderTests
	{
		private static async Task AssertMessage(
			string inputMessage,
			string expectedMessage,
			string inputSender,
			string expectedSender)
		{
			expectedMessage = expectedMessage ?? inputMessage;
			expectedSender = expectedSender ?? inputSender;
			Stream newStream = null;
			try
			{
				var stream = new MemoryStream(Encoding.ASCII.GetBytes(
					inputMessage));
				var headers = await MailUtilities.ParseHeadersAsync(stream);
				string newSender;

				(newStream, newSender) =
					await MailDispatcher.ReplaceSenderAsync(headers, stream, inputSender, CancellationToken.None);

				if (inputMessage == expectedMessage && inputSender == expectedSender)
				{
					Assert.Same(stream, newStream);
				}
				else
				{
					Assert.NotSame(stream, newStream);
					Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
				}

				Assert.Equal(0, newStream.Position);
				Assert.Equal(expectedSender, newSender);
				using (var reader = new StreamReader(newStream, Encoding.ASCII, false, 1024, true))
				{
					Assert.Equal(expectedMessage, await reader.ReadToEndAsync());
				}
			}
			finally
			{
				newStream.Dispose();
			}
		}

		[Fact]
		public async Task NoMatchingReferences()
		{
			await AssertMessage(@"Pre: Value
From: A<a@example.com>
Mid: Value
References: <other>
Post: Value

Test
Message",
				null,
				"a@example.com",
				null);
		}

		[Fact]
		public async Task NoReferences()
		{
			await AssertMessage(@"Pre: Value
From: A<a@example.com>
Mid: Value
Post: Value

Test
Message",
				null,
				"a@example.com",
				null);
		}

		[Fact]
		public async Task PostMatchingNewLineReference()
		{
			await AssertMessage(@"Pre: Value
From: A<a@example.com>
Mid: Value
References: <vaettir.net:original-sender:a-ext@example.com>
  <other>
Post: Value

Test
Message",
				@"Pre: Value
From: A<a-ext@example.com>
Mid: Value
References: <other>
Post: Value

Test
Message",
				"a@example.com",
				"a-ext@example.com");
		}

		[Fact]
		public async Task PostMatchingSameLineReference()
		{
			await AssertMessage(@"Pre: Value
From: A<a@example.com>
Mid: Value
References: <vaettir.net:original-sender:a-ext@example.com> <other>
Post: Value

Test
Message",
				@"Pre: Value
From: A<a-ext@example.com>
Mid: Value
References: <other>
Post: Value

Test
Message",
				"a@example.com",
				"a-ext@example.com");
		}

		[Fact]
		public async Task PreMatchingNextLineReference()
		{
			await AssertMessage(@"Pre: Value
From: A<a@example.com>
Mid: Value
References: <other>
  <vaettir.net:original-sender:a-ext@example.com>
Post: Value

Test
Message",
				@"Pre: Value
From: A<a-ext@example.com>
Mid: Value
References: <other>
Post: Value

Test
Message",
				"a@example.com",
				"a-ext@example.com");
		}

		[Fact]
		public async Task PreMatchingSameLineReference()
		{
			await AssertMessage(@"Pre: Value
From: A<a@example.com>
Mid: Value
References: <other> <vaettir.net:original-sender:a-ext@example.com>
Post: Value

Test
Message",
				@"Pre: Value
From: A<a-ext@example.com>
Mid: Value
References: <other>
Post: Value

Test
Message",
				"a@example.com",
				"a-ext@example.com");
		}

		[Fact]
		public async Task SingleMatchingReference()
		{
			await AssertMessage(@"Pre: Value
From: A<a@example.com>
Mid: Value
References: <vaettir.net:original-sender:a-ext@example.com>
Post: Value

Test
Message",
				@"Pre: Value
From: A<a-ext@example.com>
Mid: Value
Post: Value

Test
Message",
				"a@example.com",
				"a-ext@example.com");
		}

		[Fact]
		public async Task SurroundNewLinesReference()
		{
			await AssertMessage(@"Pre: Value
From: A<a@example.com>
Mid: Value
References: <a> <b>
  <vaettir.net:original-sender:a-ext@example.com> <e>
  <f>
Post: Value

Test
Message",
				@"Pre: Value
From: A<a-ext@example.com>
Mid: Value
References: <a> <b>
  <e>
  <f>
Post: Value

Test
Message",
				"a@example.com",
				"a-ext@example.com");
		}

		[Fact]
		public async Task SurroundReference()
		{
			await AssertMessage(@"Pre: Value
From: A<a@example.com>
Mid: Value
References: <a> <b>
  <c> <vaettir.net:original-sender:a-ext@example.com> <d>
  <e> <f>
Post: Value

Test
Message",
				@"Pre: Value
From: A<a-ext@example.com>
Mid: Value
References: <a> <b>
  <c> <d>
  <e> <f>
Post: Value

Test
Message",
				"a@example.com",
				"a-ext@example.com");
		}
	}
}
