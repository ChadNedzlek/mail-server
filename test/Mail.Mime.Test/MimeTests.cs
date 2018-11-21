using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

			await TestContent(structure,
				stream,
				new ExpectedPart(
					new Dictionary<string, string>
					{
						{"Return-Path", " <test@example.com>"},
						{"From", " \"Sender Name\" <test@example.com>"},
						{"To", " \"Recipient Name\" <test@test.com>,\r\n\t\"Other Name\" <other@test.com>"},
						{"Subject"," Sample subject"},
						{"MIME-Version"," 1.0"},
						{"Content-Type"," text/plain;\r\n\tcharset=\"us-ascii\""},
						{"Content-Transfer-Encoding"," 7bit"},
					},
					"Test body\r\n\r\nTest paragraph\r\n"
				)
			);
		}
		[Fact]
		public static async Task TwoChildrenTest()
		{
			var stream = typeof(MimeTests).GetResource("testdata.two-children.eml");
			MimeReader reader = new MimeReader();
			var structure = await reader.ReadStructureAsync(stream, CancellationToken.None);
			await TestContent(structure,
				stream,
				new ExpectedPart(
					new Dictionary<string, string>
					{
						{"From", " From Name <fn@example.com>"},
						{"To", " To Name<tn@example.com>"},
						{"Subject", " Sample message"},
						{"MIME-Version", " 1.0"},
						{"Content-Type", " multipart/mixed; boundary=\"simple boundary\""},
					},
					"This is the preamble.  It is to be ignored, though it\r\nis a handy place for mail composers to include an\r\nexplanatory note to non-MIME compliant readers.",
					"This is the epilogue.  It is also to be ignored.\r\n",
					new ExpectedPart(
						new Dictionary<string, string>(),
						"This is implicitly typed plain ASCII text.\r\nIt does NOT end with a linebreak."),
					new ExpectedPart(
						new Dictionary<string, string>
						{
							{"Content-type", " text/plain; charset=us-ascii"},
						},
						"This is explicitly typed plain ASCII text.\r\nIt DOES end with a linebreak.\r\n"))
			);
		}

		[Fact]
		public static async Task Nested()
		{
			var stream = typeof(MimeTests).GetResource("testdata.nested.eml");
			MimeReader reader = new MimeReader();
			var structure = await reader.ReadStructureAsync(stream, CancellationToken.None);
			await TestContent(structure,
				stream,
				new ExpectedPart(
					new Dictionary<string, string>
					{
						{"MIME-Version", " 1.0"}, {"From", " Nathaniel Borenstein <nsb@bellcore.com>"},
						{"To", " Ned Freed <ned@innosoft.com>"}, {"Subject", " A multipart example"},
						{"Content-Type", " multipart/mixed;\r\n     boundary=unique-boundary-1"},
					},
					"This is the preamble area of a multipart message.\r\nMail readers that understand multipart format\r\nshould ignore this preamble.\r\nIf you are reading this text, you might want to\r\nconsider changing to a mail reader that understands\r\nhow to properly display multipart messages.",
					"",
					new ExpectedPart(
						new Dictionary<string, string>(),
						"\r\n...Some text appears here...\r\n\r\n\r\n[Note that the preceding blank line means\r\nno header fields were given and this is text,\r\nwith charset US ASCII.  It could have been\r\ndone with explicit typing as in the next part.]\r\n"),
					new ExpectedPart(new Dictionary<string, string>
						{
							{"Content-type", " text/plain; charset=US-ASCII"},
						},
						"This could have been part of the previous part,\r\nbut illustrates explicit versus implicit\r\ntyping of body parts.\r\n"),
					new ExpectedPart(new Dictionary<string, string>
						{
							{"Content-Type", " multipart/parallel;\r\n     boundary=\"unique-\\\"boundary-2\\\"\""},
						},
						"",
						"",
						new ExpectedPart(new Dictionary<string, string>
							{
								{"Content-Type", " audio/basic"}, {"Content-Transfer-Encoding", " base64"},
							},
							"\r\n... base64-encoded 8000 Hz single-channel\r\n mu-law-format audio data goes here....\r\n"),
						new ExpectedPart(new Dictionary<string, string>
							{
								{"Content-Type", " image/gif"}, {"Content-Transfer-Encoding", " base64"},
							},
							"\r\n... base64-encoded image data goes here....\r\n\r\n")
					),
					new ExpectedPart(new Dictionary<string, string>
						{
							{"Content-type", " text/richtext"},
						},
						"This is <bold><italic>richtext.</italic></bold>\r\n<smaller>as defined in RFC 1341</smaller>\r\n<nl><nl>Isn\'t it\r\n<bigger><bigger>cool?</bigger></bigger>\r\n"),
					new ExpectedPart(new Dictionary<string, string> {{"Content-Type", " message/rfc822"},},
						"From: (mailbox in US-ASCII)\r\nTo: (address in US-ASCII)\r\nSubject: (subject in US-ASCII)\r\nContent-Type: Text/plain; charset=ISO-8859-1\r\nContent-Transfer-Encoding: Quoted-printable\r\n\r\n\r\n... Additional text in ISO-8859-1 goes here ...\r\n\r\n")
				)
			);
		}

		private static async Task TestContent(MimePartSpan mime, Stream source, ExpectedPart expected)
		{
			var headerText = await GetPiece(source, mime.HeaderSpan);
			var foundHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var chunks = headerText.Split("\r\n");
			string lastKey = null;
			bool foundEmpty = false;
			foreach (var chunk in chunks)
			{
				Assert.False(foundEmpty, "End of headers reached");
				if (chunk == "")
				{
					foundEmpty = true;
					continue;
				}

				if (chunk.Length > 0 && Char.IsWhiteSpace(chunk[0]))
				{
					foundHeaders[lastKey] += "\r\n" + chunk;
				}
				else
				{
					var parts = chunk.Split(new[]{':'}, 2);
					lastKey = parts[0];
					foundHeaders[lastKey] = parts[1];
				}
			}

			foreach (var (name, value) in expected.Headers)
			{
				Assert.Contains(name, foundHeaders.Keys, StringComparer.OrdinalIgnoreCase);
				Assert.Equal(value, foundHeaders[name]);
				foundHeaders.Remove(name);
			}

			Assert.Empty(foundHeaders);

			if (expected.Children != null)
			{
				Assert.Equal(expected.Children.Count, mime.Parts.Length);
				for (var index = 0; index < expected.Children.Count; index++)
				{
					await TestContent(mime.Parts[index], source, expected.Children[index]);
				}

				var childrenStart = mime.Parts[0].Span.Start;
				var childrenEnd = mime.Parts.Last().Span.Start;
				Assert.StartsWith(expected.Preamble, await GetPiece(
					source,
					new MessageSpan(mime.ContentSpan.Start, childrenStart - mime.ContentSpan.Start)));

				Assert.EndsWith(expected.Epilogue, await GetPiece(
					source,
					new MessageSpan(childrenEnd, mime.ContentSpan.End - childrenEnd)));
			}
			else
			{
				Assert.Equal(expected.Body, await GetPiece(source, mime.ContentSpan));
			}
		}

		private class ExpectedPart
		{
			public Dictionary<string, string> Headers { get; set; }
			public string Body { get; set; }

			public string Preamble { get; set; }
			public IList<ExpectedPart> Children { get; set; }
			public string Epilogue { get; set; }

			public ExpectedPart(Dictionary<string, string> headers, string body)
			{
				Headers = headers;
				Body = body;
			}

			public ExpectedPart(
				Dictionary<string, string> headers,
				string preamble,
				string epilogue,
				IList<ExpectedPart> children)
			{
				Headers = headers;
				Preamble = preamble;
				Epilogue = epilogue;
				Children = children;
			}

			public ExpectedPart(
				Dictionary<string, string> headers,
				string preamble,
				string epilogue,
				params ExpectedPart[] children)
				: this(headers, preamble, epilogue, (IList<ExpectedPart>) children)
			{
			}
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
