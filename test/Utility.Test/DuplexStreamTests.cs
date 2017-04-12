using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Test.Utilities;
using Vaettir.Utility;
using Xunit;

namespace Utility.Test
{
    public class DuplexStreamTests
    {
        private static readonly byte[] s_bytes = Enumerable.Range(1, 100).Select(b => (byte)b).ToArray();

		[Fact]
		public async Task SendRecieveOneWay()
		{
			var (a,b) = PairedStream.Create();
			await a.WriteAsync(s_bytes, 0, s_bytes.Length);
			byte[] buffer = new byte[s_bytes.Length * 2];
			var read = await b.ReadAsync(buffer, 0, buffer.Length);
			Assert.Equal(s_bytes.Length, read);
			Assert.Equal(s_bytes, new ArraySegment<byte>(buffer, 0, read));
		}

		[Fact]
		public async Task SendPartialOneWay()
		{
			var (a, b) = PairedStream.Create();
			await a.WriteAsync(s_bytes, 0, s_bytes.Length);
			byte[] buffer = new byte[s_bytes.Length / 2];
			var read = await b.ReadAsync(buffer, 0, buffer.Length);
			Assert.InRange(read, 0, buffer.Length);
			Assert.Equal(s_bytes.Take(read), new ArraySegment<byte>(buffer, 0, read));

			buffer = new byte[s_bytes.Length - read];
			var secondRead = await b.ReadAsync(buffer, 0, buffer.Length);
			Assert.Equal(secondRead, buffer.Length);
			Assert.Equal(s_bytes.Skip(secondRead), new ArraySegment<byte>(buffer, 0, secondRead));
		}

		[Fact]
		public async Task SendRecieveOneWay_WithToken()
		{
			var (a, b) = PairedStream.Create();
			await a.WriteAsync(s_bytes, 0, s_bytes.Length);
			byte[] buffer = new byte[s_bytes.Length * 2];
			var read = await b.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);
			Assert.Equal(s_bytes.Length, read);
			Assert.Equal(s_bytes, new ArraySegment<byte>(buffer, 0, read));
		}

		[Fact]
		public async Task SendRecieveOneWay_WithCancellableToken()
		{
			var (a, b) = PairedStream.Create();
			await a.WriteAsync(s_bytes, 0, s_bytes.Length);
			byte[] buffer = new byte[s_bytes.Length * 2];
			var cts = new CancellationTokenSource();
			var read = await b.ReadAsync(buffer, 0, buffer.Length, cts.Token);
			Assert.Equal(s_bytes.Length, read);
		    Assert.Equal(s_bytes, new ArraySegment<byte>(buffer, 0, read));
		}

		[Fact]
		public async Task SendBothRecieveBoth()
		{
			var (a, b) = PairedStream.Create();
			await a.WriteAsync(s_bytes, 0, s_bytes.Length);
			await b.WriteAsync(s_bytes, 0, s_bytes.Length);
			byte[] buffer = new byte[s_bytes.Length * 2];

			var read = await b.ReadAsync(buffer, 0, buffer.Length);
			Assert.Equal(s_bytes.Length, read);
			Assert.Equal(s_bytes, new ArraySegment<byte>(buffer, 0, read));

			read = await a.ReadAsync(buffer, 0, buffer.Length);
			Assert.Equal(s_bytes.Length, read);
			Assert.Equal(s_bytes, new ArraySegment<byte>(buffer, 0, read));
		}

		[Fact]
		public async Task ReceiveNoData()
		{
			var (a, b) = PairedStream.Create();
			byte[] buffer = new byte[s_bytes.Length * 2];
			await TaskHelpers.AssertNotTriggered(a.ReadAsync(buffer, 0, buffer.Length));
			await TaskHelpers.AssertNotTriggered(b.ReadAsync(buffer, 0, buffer.Length));
		}

		[Fact]
		public async Task ReceiveNoData_Uncancellable()
		{
			var (a, b) = PairedStream.Create();
			byte[] buffer = new byte[s_bytes.Length * 2];
			await TaskHelpers.AssertNotTriggered(a.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None));
			await TaskHelpers.AssertNotTriggered(b.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None));
		}

		[Fact]
		public async Task ReceiveNoData_Cancellable()
		{
			var (a, b) = PairedStream.Create();
			byte[] buffer = new byte[s_bytes.Length * 2];
		    CancellationTokenSource cts = new CancellationTokenSource();
			await TaskHelpers.AssertNotTriggered(a.ReadAsync(buffer, 0, buffer.Length, cts.Token));
			await TaskHelpers.AssertNotTriggered(b.ReadAsync(buffer, 0, buffer.Length, cts.Token));
		}

		[Fact]
		public async Task ReceiveNoData_Cancelled()
		{
			var (a, b) = PairedStream.Create();
			byte[] buffer = new byte[s_bytes.Length * 2];
			CancellationTokenSource cts = new CancellationTokenSource();
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
	}
}