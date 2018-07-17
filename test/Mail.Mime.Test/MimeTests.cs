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
		[Fact]
		public static async Task TwoChildrenTest()
		{
			var stream = typeof(MimeTests).GetResource("testdata.two-children.eml");
			MimeReader reader = new MimeReader();
			var structure = await reader.ReadStructureAsync(stream, CancellationToken.None);
			string header = await GetPiece(stream, structure.HeaderSpan);
			Assert.Equal(2, structure.Parts.Length);
			Assert.Equal("From: From Name <fn@example.com>\r\nTo:  To Name<tn@example.com>\r\nSubject: Sample message\r\nMIME-Version: 1.0\r\nContent-type: multipart/mixed; boundary=\"simple boundary\"\r\n", header);
			
			header = await GetPiece(stream, structure.Parts[0].HeaderSpan);
			Assert.Equal("", header);
			string content = await GetPiece(stream, structure.Parts[0].ContentSpan);
			Assert.Equal("This is implicitly typed plain ASCII text.\r\nIt does NOT end with a linebreak.\r\n", content);

			header = await GetPiece(stream, structure.Parts[1].HeaderSpan);
			Assert.Equal("Content-type: text/plain; charset=us-ascii\r\n", header);
			content = await GetPiece(stream, structure.Parts[1].ContentSpan);
			Assert.Equal("This is explicitly typed plain ASCII text.\r\nIt DOES end with a linebreak.\r\n\r\n", content);
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
