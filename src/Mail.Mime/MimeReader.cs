using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;

namespace Vaettir.Mail.Mime
{
	public class MimeReader
	{
		private byte[] _buffer = new byte[4096];

		public async Task<MimePartSpan> ReadStructureAsync(Stream mailStream, CancellationToken cancellationToken)
		{
			using (UnencodedStreamReader reader = new UnencodedStreamReader(mailStream, leaveOpen: true))
			{
				return await ReadDelimitedPartAsync(reader, null, cancellationToken);
			}
		}

		private async Task<MimePartSpan> ReadDelimitedPartAsync(
			UnencodedStreamReader reader,
			Predicate<ReadOnlyMemory<byte>> endReached,
			CancellationToken cancellationToken)
		{
			long start = reader.BytePositition;
			long headerEnd = start;
			long end = start;
			bool inHeader = true;
			string multipartBoundary = null;
			StringBuilder parsingHeader = new StringBuilder();
			while ((await ReadLine(reader, cancellationToken)).TryGet(out Memory<byte> line)
				&& !endReached(line))
			{
				end = reader.BytePositition;

				if (inHeader)
				{
					if (line.Length == 0)
					{
						headerEnd = end - 2;
						inHeader = false;
						continue;
					}

					if (IsImportantHeader(line))
					{
						parsingHeader.Append(Encoding.ASCII.GetString(line.Span));
						continue;
					}

					if (parsingHeader.Length > 0)
					{
						byte first = line.Span[0];
						if (first == ' ' || first == '\t')
						{
							parsingHeader.Append(Encoding.ASCII.GetString(line.Span));
							continue;
						}

						string headerString = parsingHeader.ToString();
						if (TestRegex(ContentTypeRegex, headerString, out var match))
						{
							string type = match.Groups["type"].Value;
							string subtype = match.Groups["subtype"].Value;
							CaptureCollection paramCaptures = match.Groups["param"].Captures;
							CaptureCollection valueCaptures = match.Groups["value"].Captures;
							for (int i = 0; i < paramCaptures.Count; i++)
							{
								switch (paramCaptures[i].Value.ToLowerInvariant())
								{
									case "charset":
										break;
									case "boundary":
										multipartBoundary = valueCaptures[i].Value;
										break;
								}
							}
						}
						parsingHeader.Clear();
						if (IsImportantHeader(line))
						{
							parsingHeader.Append(Encoding.ASCII.GetString(line.Span));
						}
					}
				}
			}

			long contentStart = headerEnd + 2; // CRLF after header

			return new MimePartSpan(
				new MessageSpan(start, end - start),
				new MessageSpan(
					start,
					headerEnd - start),
				new MessageSpan(contentStart, end - contentStart)
			);
		}

		private static bool IsImportantHeader(Memory<byte> line)
		{
			return Strings.ContentType.Starts(line.Span);
		}

		private static readonly Regex ContentTypeRegex = new Regex(@"
^
	Content-Type\s*:\s*
	(?<type>[^/]+)
	/
	(?<subtype>[^;]+)
	\s*
	(?:	
		;\s*(?<param>[^=]+)\s*=\s*""(?<value>[^""]*)""\s*
	)*
	;?
$", RegexOptions.IgnorePatternWhitespace);

		private bool TestRegex(Regex pattern, string input, out Match match)
		{
			match = pattern.Match(input);
			return match.Success;
		}

		private async Task<LinePull> ReadLine(
			UnencodedStreamReader reader,
			CancellationToken cancellationToken)
		{
			int? readCount = await reader.TryReadLineAsync(_buffer, cancellationToken);
			return new LinePull(_buffer.AsMemory(0, readCount.GetValueOrDefault()), !readCount.HasValue);
		}
	}

	public struct LinePull
	{
		public LinePull(Memory<byte> line, bool endOfStream)
		{
			EndOfStream = endOfStream;
			Line = line;
		}

		public Memory<byte> Line { get; }
		public bool EndOfStream { get; }

		public bool TryGet(out Memory<byte> value)
		{
			value = Line;
			return !EndOfStream;
		}
	}
}
