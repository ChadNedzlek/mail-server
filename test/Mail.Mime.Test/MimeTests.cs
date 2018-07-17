using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Mime;
using Xunit;

namespace Vaettir.Mime.Test
{
	public static class MimeTests
	{
		[Fact]
		public static async Task SimpleMimeTest()
		{
			var stream = typeof(MimeTests).GetResource("testdata.simple.eml");
			MimeReader reader = new MimeReader();
			var structure = await reader.ReadStructureAsync(stream, CancellationToken.None);
			string header = await GetPiece(stream, structure.HeaderSpan);

			Assert.Equal("Return-Path: <test@example.com>\r\nFrom: \"Sender Name\" <test@example.com>\r\nTo: \"Recipient Name\" <test@test.com>,\r\n\t\"Other Name\" <other@test.com>\r\nSubject: Sample subject\r\nMIME-Version: 1.0\r\nContent-Type: text/plain;\r\n\tcharset=\"us-ascii\"\r\nContent-Transfer-Encoding: 7bit\r\n", header);
			string content = await GetPiece(stream, structure.ContentSpan);
			Assert.Equal("Test body\r\n\r\nTest paragraph\r\n\r\n", content);
		}

		private static async Task<string> GetPiece(Stream stream, MessageSpan span)
		{
			stream.Seek(span.Start, SeekOrigin.Begin);
			byte[] chunk = new byte[span.Length];
			var read = await stream.ReadAsync(chunk.AsMemory(0, chunk.Length));
			Assert.Equal(chunk.Length, read);
			return Encoding.ASCII.GetString(chunk);
		}
	}
}
