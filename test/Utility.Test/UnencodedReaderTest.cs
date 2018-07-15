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
			var testString = "";
			MemoryStream inputStream = new MemoryStream(Encoding.ASCII.GetBytes(testString));
			UnencodedStreamReader reader = new UnencodedStreamReader(inputStream);
			var line = await reader.ReadLineAsync(CancellationToken.None);
			Assert.Null(line);
		}

		[Fact]
		public async Task NoLinesStreamReader()
		{
			var testString = "Test String";
			MemoryStream inputStream = new MemoryStream(Encoding.ASCII.GetBytes(testString));
			UnencodedStreamReader reader = new UnencodedStreamReader(inputStream);
			var line = await reader.ReadLineAsync(CancellationToken.None);
			Assert.Equal(testString, Encoding.ASCII.GetString(line));
		}

		[Fact]
		public async Task ReallyLongLine()
		{
			var testString = "A" + new string('_', 5000) + "B";
			MemoryStream inputStream = new MemoryStream(Encoding.ASCII.GetBytes(testString));
			UnencodedStreamReader reader = new UnencodedStreamReader(inputStream);
			var line = await reader.ReadLineAsync(CancellationToken.None);
			Assert.Equal(testString, Encoding.ASCII.GetString(line));
		}

		[Fact]
		public async Task TwoLines()
		{
			var testString = "ABC\r\nDEF\r\nGHI\r\n";
			MemoryStream inputStream = new MemoryStream(Encoding.ASCII.GetBytes(testString));
			UnencodedStreamReader reader = new UnencodedStreamReader(inputStream);
			var line = await reader.ReadLineAsync(CancellationToken.None);
			Assert.Equal("ABC", Encoding.ASCII.GetString(line));
			line = await reader.ReadLineAsync(CancellationToken.None);
			Assert.Equal("DEF", Encoding.ASCII.GetString(line));
			line = await reader.ReadLineAsync(CancellationToken.None);
			Assert.Equal("GHI", Encoding.ASCII.GetString(line));
			line = await reader.ReadLineAsync(CancellationToken.None);
			Assert.Null(line);
		}
	}
}
