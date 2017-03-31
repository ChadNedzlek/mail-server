using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
			Tuple<PairedStream, PairedStream> tuple = PairedStream.Create();
			await tuple.Item1.WriteAsync(s_bytes, 0, s_bytes.Length);
			byte[] buffer = new byte[s_bytes.Length * 2];
			var read = await tuple.Item2.ReadAsync(buffer, 0, buffer.Length);
			Assert.Equal(s_bytes.Length, read);
			Assert.Equal(s_bytes, new ArraySegment<byte>(buffer, 0, read));
		}

		[Fact]
		public async Task SendPartialOneWay()
		{
			Tuple<PairedStream, PairedStream> tuple = PairedStream.Create();
			await tuple.Item1.WriteAsync(s_bytes, 0, s_bytes.Length);
			byte[] buffer = new byte[s_bytes.Length / 2];
			var read = await tuple.Item2.ReadAsync(buffer, 0, buffer.Length);
			Assert.InRange(read, 0, buffer.Length);
			Assert.Equal(s_bytes.Take(read), new ArraySegment<byte>(buffer, 0, read));

			buffer = new byte[s_bytes.Length - read];
			var secondRead = await tuple.Item2.ReadAsync(buffer, 0, buffer.Length);
			Assert.Equal(secondRead, buffer.Length);
			Assert.Equal(s_bytes.Skip(secondRead), new ArraySegment<byte>(buffer, 0, secondRead));
		}

		[Fact]
		public async Task SendRecieveOneWay_WithToken()
		{
			Tuple<PairedStream, PairedStream> tuple = PairedStream.Create();
			await tuple.Item1.WriteAsync(s_bytes, 0, s_bytes.Length);
			byte[] buffer = new byte[s_bytes.Length * 2];
			var read = await tuple.Item2.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);
			Assert.Equal(s_bytes.Length, read);
			Assert.Equal(s_bytes, new ArraySegment<byte>(buffer, 0, read));
		}

		[Fact]
		public async Task SendRecieveOneWay_WithCancellableToken()
		{
			Tuple<PairedStream, PairedStream> tuple = PairedStream.Create();
			await tuple.Item1.WriteAsync(s_bytes, 0, s_bytes.Length);
			byte[] buffer = new byte[s_bytes.Length * 2];
			var cts = new CancellationTokenSource();
			var read = await tuple.Item2.ReadAsync(buffer, 0, buffer.Length, cts.Token);
			Assert.Equal(s_bytes.Length, read);
		    Assert.Equal(s_bytes, new ArraySegment<byte>(buffer, 0, read));
		}

		[Fact]
		public async Task SendBothRecieveBoth()
		{
			Tuple<PairedStream, PairedStream> tuple = PairedStream.Create();
			await tuple.Item1.WriteAsync(s_bytes, 0, s_bytes.Length);
			await tuple.Item2.WriteAsync(s_bytes, 0, s_bytes.Length);
			byte[] buffer = new byte[s_bytes.Length * 2];

			var read = await tuple.Item2.ReadAsync(buffer, 0, buffer.Length);
			Assert.Equal(s_bytes.Length, read);
			Assert.Equal(s_bytes, new ArraySegment<byte>(buffer, 0, read));

			read = await tuple.Item1.ReadAsync(buffer, 0, buffer.Length);
			Assert.Equal(s_bytes.Length, read);
			Assert.Equal(s_bytes, new ArraySegment<byte>(buffer, 0, read));
		}

		[Fact]
		public async Task ReceiveNoData()
		{
			Tuple<PairedStream, PairedStream> tuple = PairedStream.Create();
			byte[] buffer = new byte[s_bytes.Length * 2];
			await TaskHelpers.AssertNotTriggered(tuple.Item1.ReadAsync(buffer, 0, buffer.Length));
			await TaskHelpers.AssertNotTriggered(tuple.Item2.ReadAsync(buffer, 0, buffer.Length));
		}

		[Fact]
		public async Task ReceiveNoData_Uncancellable()
		{
			Tuple<PairedStream, PairedStream> tuple = PairedStream.Create();
			byte[] buffer = new byte[s_bytes.Length * 2];
			await TaskHelpers.AssertNotTriggered(tuple.Item1.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None));
			await TaskHelpers.AssertNotTriggered(tuple.Item2.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None));
		}

		[Fact]
		public async Task ReceiveNoData_Cancellable()
		{
			Tuple<PairedStream, PairedStream> tuple = PairedStream.Create();
			byte[] buffer = new byte[s_bytes.Length * 2];
		    CancellationTokenSource cts = new CancellationTokenSource();
			await TaskHelpers.AssertNotTriggered(tuple.Item1.ReadAsync(buffer, 0, buffer.Length, cts.Token));
			await TaskHelpers.AssertNotTriggered(tuple.Item2.ReadAsync(buffer, 0, buffer.Length, cts.Token));
		}

		[Fact]
		public async Task ReceiveNoData_Cancelled()
		{
			Tuple<PairedStream, PairedStream> tuple = PairedStream.Create();
			byte[] buffer = new byte[s_bytes.Length * 2];
			CancellationTokenSource cts = new CancellationTokenSource();
			Task[] tasks =
		    {
		        tuple.Item1.ReadAsync(buffer, 0, buffer.Length, cts.Token),
		        tuple.Item2.ReadAsync(buffer, 0, buffer.Length, cts.Token)
		    };
			await TaskHelpers.AssertNotTriggered(tasks[0]);
			await TaskHelpers.AssertNotTriggered(tasks[1]);

		    cts.Cancel();

			await TaskHelpers.AssertCancelled(tasks[0]);
			await TaskHelpers.AssertCancelled(tasks[1]);
		}
	}
}