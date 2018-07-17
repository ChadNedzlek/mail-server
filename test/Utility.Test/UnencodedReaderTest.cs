using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Xunit;

namespace Vaettir.Utility.Test
{
	public class UnencodedReaderTest
	{
		[Fact]
		public async Task EmptyStreamReader()
		{
			byte [] buffer = new byte[1000];
			var testString = "";
			MemoryStream inputStream = new MemoryStream(Encoding.ASCII.GetBytes(testString));
			UnencodedStreamReader reader = new UnencodedStreamReader(inputStream);
			var read = await reader.TryReadLineAsync(buffer, CancellationToken.None);
			Assert.False(read.HasValue);
			Assert.Equal(0, buffer[0]);
		}

		[Fact]
		public async Task NoLinesStreamReader()
		{
			byte[] buffer = new byte[1000];
			var testString = "Test String";
			MemoryStream inputStream = new MemoryStream(Encoding.ASCII.GetBytes(testString));
			UnencodedStreamReader reader = new UnencodedStreamReader(inputStream);
			var read = await reader.TryReadLineAsync(buffer, CancellationToken.None);
			Assert.Equal(testString.Length, read);
			Assert.Equal(testString, Encoding.ASCII.GetString(buffer, 0, read.Value));
		}

		[Fact]
		public async Task ReallyLongLine()
		{
			byte[] buffer = new byte[10000];
			var testString = "A" + new string('_', 5000) + "B";
			MemoryStream inputStream = new MemoryStream(Encoding.ASCII.GetBytes(testString));
			UnencodedStreamReader reader = new UnencodedStreamReader(inputStream);
			var read = await reader.TryReadLineAsync(buffer, CancellationToken.None);
			Assert.Equal(testString.Length, read);
			Assert.Equal(testString, Encoding.ASCII.GetString(buffer, 0, read.Value));
		}

		[Fact]
		public async Task MultipleLines()
		{
			byte[] buffer = new byte[1000];
			var testString = "ABC\r\nDEF\r\nGHI\r\n";
			MemoryStream inputStream = new MemoryStream(Encoding.ASCII.GetBytes(testString));
			UnencodedStreamReader reader = new UnencodedStreamReader(inputStream);
			var read = await reader.TryReadLineAsync(buffer, CancellationToken.None);
			Assert.Equal(3, read);
			Assert.Equal("ABC", Encoding.ASCII.GetString(buffer, 0, read.Value));
			read = await reader.TryReadLineAsync(buffer, CancellationToken.None);
			Assert.Equal(3, read);
			Assert.Equal("DEF", Encoding.ASCII.GetString(buffer, 0, read.Value));
			read = await reader.TryReadLineAsync(buffer, CancellationToken.None);
			Assert.Equal(3, read);
			Assert.Equal("GHI", Encoding.ASCII.GetString(buffer, 0, read.Value));
			read = await reader.TryReadLineAsync(buffer, CancellationToken.None);
			Assert.False(read.HasValue);
		}
	}
}
