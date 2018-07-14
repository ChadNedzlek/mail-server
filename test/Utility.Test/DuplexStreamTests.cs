using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Test.Utilities;
using Xunit;

namespace Vaettir.Utility.Test
{
	public class DuplexStreamTests
	{
		private static readonly byte[] s_bytes = Enumerable.Range(1, 100).Select(b => (byte) b).ToArray();

		[Fact]
		public async Task ReceiveNoData()
		{
			(Stream a, Stream b) = PairedStream.Create();
			var buffer = new byte[s_bytes.Length * 2];
			await TaskHelpers.AssertNotTriggered(a.ReadAsync(buffer, 0, buffer.Length));
			await TaskHelpers.AssertNotTriggered(b.ReadAsync(buffer, 0, buffer.Length));
		}

		[Fact]
		public async Task ReceiveNoData_Cancellable()
		{
			(Stream a, Stream b) = PairedStream.Create();
			var buffer = new byte[s_bytes.Length * 2];
			var cts = new CancellationTokenSource();
			await TaskHelpers.AssertNotTriggered(a.ReadAsync(buffer, 0, buffer.Length, cts.Token));
			await TaskHelpers.AssertNotTriggered(b.ReadAsync(buffer, 0, buffer.Length, cts.Token));
		}

		[Fact]
		public async Task ReceiveNoData_Cancelled()
		{
			(Stream a, Stream b) = PairedStream.Create();
			var buffer = new byte[s_bytes.Length * 2];
			var cts = new CancellationTokenSource();
			Task[] tasks =
			{
				a.ReadAsync(buffer, 0, buffer.Length, cts.Token),
				b.ReadAsync(buffer, 0, buffer.Length, cts.Token)
			};
			await TaskHelpers.AssertNotTriggered(tasks[0]);
			await TaskHelpers.AssertNotTriggered(tasks[1]);

			cts.Cancel();

			await TaskHelpers.AssertCancelled(tasks[0]);
			await TaskHelpers.AssertCancelled(tasks[1]);
		}

		[Fact]
		public async Task ReceiveNoData_Uncancellable()
		{
			(Stream a, Stream b) = PairedStream.Create();
			var buffer = new byte[s_bytes.Length * 2];
			await TaskHelpers.AssertNotTriggered(a.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None));
			await TaskHelpers.AssertNotTriggered(b.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None));
		}

		[Fact]
		public async Task SendBothRecieveBoth()
		{
			(Stream a, Stream b) = PairedStream.Create();
			await a.WriteAsync(s_bytes, 0, s_bytes.Length);
			await b.WriteAsync(s_bytes, 0, s_bytes.Length);
			var buffer = new byte[s_bytes.Length * 2];

			int read = await b.ReadAsync(buffer, 0, buffer.Length);
			Assert.Equal(s_bytes.Length, read);
			Assert.Equal(s_bytes, new ArraySegment<byte>(buffer, 0, read));

			read = await a.ReadAsync(buffer, 0, buffer.Length);
			Assert.Equal(s_bytes.Length, read);
			Assert.Equal(s_bytes, new ArraySegment<byte>(buffer, 0, read));
		}

		[Fact]
		public async Task SendPartialOneWay()
		{
			(Stream a, Stream b) = PairedStream.Create();
			await a.WriteAsync(s_bytes, 0, s_bytes.Length);
			var buffer = new byte[s_bytes.Length / 2];
			int read = await b.ReadAsync(buffer, 0, buffer.Length);
			Assert.InRange(read, 0, buffer.Length);
			Assert.Equal(s_bytes.Take(read), new ArraySegment<byte>(buffer, 0, read));

			buffer = new byte[s_bytes.Length - read];
			int secondRead = await b.ReadAsync(buffer, 0, buffer.Length);
			Assert.Equal(secondRead, buffer.Length);
			Assert.Equal(s_bytes.Skip(secondRead), new ArraySegment<byte>(buffer, 0, secondRead));
		}

		[Fact]
		public async Task SendRecieveOneWay()
		{
			(Stream a, Stream b) = PairedStream.Create();
			await a.WriteAsync(s_bytes, 0, s_bytes.Length);
			var buffer = new byte[s_bytes.Length * 2];
			int read = await b.ReadAsync(buffer, 0, buffer.Length);
			Assert.Equal(s_bytes.Length, read);
			Assert.Equal(s_bytes, new ArraySegment<byte>(buffer, 0, read));
		}

		[Fact]
		public async Task SendRecieveOneWay_WithCancellableToken()
		{
			(Stream a, Stream b) = PairedStream.Create();
			await a.WriteAsync(s_bytes, 0, s_bytes.Length);
			var buffer = new byte[s_bytes.Length * 2];
			var cts = new CancellationTokenSource();
			int read = await b.ReadAsync(buffer, 0, buffer.Length, cts.Token);
			Assert.Equal(s_bytes.Length, read);
			Assert.Equal(s_bytes, new ArraySegment<byte>(buffer, 0, read));
		}

		[Fact]
		public async Task SendRecieveOneWay_WithToken()
		{
			(Stream a, Stream b) = PairedStream.Create();
			await a.WriteAsync(s_bytes, 0, s_bytes.Length);
			var buffer = new byte[s_bytes.Length * 2];
			int read = await b.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);
			Assert.Equal(s_bytes.Length, read);
			Assert.Equal(s_bytes, new ArraySegment<byte>(buffer, 0, read));
		}
	}
}
