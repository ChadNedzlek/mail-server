using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
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
				MimePartBuilder topLevelBuilder = new MimePartBuilder();
				var currentBuilder = topLevelBuilder;

				StringBuilder currentHeader = new StringBuilder();
				while ((await ReadLine(reader, cancellationToken)).TryGet(out Memory<byte> line))
				{

					long position = reader.BytePositition;
					if (currentBuilder.Parent != null &&
						currentBuilder.Parent.EndBoundary.Span.SequenceEqual(line.Span))
					{
						currentBuilder = currentBuilder.Parent;
						continue;
					}

					if (currentBuilder.Parent != null &&
						currentBuilder.Parent.Boundary.Span.SequenceEqual(line.Span))
					{
						currentBuilder = new MimePartBuilder(currentBuilder.Parent, position);
						continue;
					}

					if (!currentBuilder.Boundary.IsEmpty &&
						currentBuilder.Children == null &&
						currentBuilder.Boundary.Span.SequenceEqual(line.Span))
					{
						currentBuilder.Children = new List<MimePartBuilder>();
						currentBuilder = new MimePartBuilder(currentBuilder, position);
						continue;
					}

					currentBuilder.End = position;

					if (!currentBuilder.HeaderComplete)
					{
						void ProcessPendingHeader()
						{
							if (currentHeader.Length == 0)
								return;

							string headerString = currentHeader.ToString();
							currentHeader.Clear();
							if (TestRegex(ContentTypeRegex, headerString, out var match))
							{
								string type = match.Groups["type"].Value;
								string subtype = match.Groups["subtype"].Value;
								if (type == "multipart")
								{
									CaptureCollection paramCaptures = match.Groups["param"].Captures;
									CaptureCollection valueCaptures = match.Groups["value"].Captures;
									for (int i = 0; i < paramCaptures.Count; i++)
									{
										switch (paramCaptures[i].Value.ToLowerInvariant())
										{
											case "charset":
												break;
											case "boundary":
												ReadOnlySpan<char> value = valueCaptures[i].Value.AsSpan();
												if (value.Length > 1 &&
													value[0] == '"' &&
													value[value.Length - 1] == '"')
												{
													value = value.Slice(1, value.Length - 2);
													if (value.Contains("\\", StringComparison.Ordinal))
													{
														value = Regex.Replace(value.ToString(), @"\\.", m => m.Value[1].ToString());
													}
												}

												currentBuilder.EndBoundary =
													Encoding.ASCII.GetBytes("--" + value.ToString() + "--");
												break;
										}
									}
								}
							}
						}

						if (line.Length == 0)
						{
							// header's over, process pending ones
							ProcessPendingHeader();
							currentBuilder.ContentStart = position;
							continue;
						}

						if (IsImportantHeader(line))
						{
							currentHeader.Append(Encoding.ASCII.GetString(line.Span));
							continue;
						}

						if (currentHeader.Length > 0)
						{
							byte first = line.Span[0];
							if (first == ' ' || first == '\t')
							{
								currentHeader.Append(Encoding.ASCII.GetString(line.Span));
								continue;
							}

							// We found another header, process the current one.
							ProcessPendingHeader();

							if (IsImportantHeader(line))
							{
								currentHeader.Append(Encoding.ASCII.GetString(line.Span));
							}
						}
					}
				}

				currentBuilder.End = reader.BytePositition;

				return topLevelBuilder.ToMimePart();
			}
		}

		private class MimePartBuilder
		{
			private ReadOnlyMemory<byte> _endBoundary;

			public MimePartBuilder(MimePartBuilder parent, long position)
			{
				Parent = parent;
				Start = position;
				parent?.Children.Add(this);
			}

			public MimePartBuilder() : this(null, 0)
			{
			}

			public long Start { get; }
			public long ContentStart { get; set; }
			public long End { get; set; }

			public bool HeaderComplete => ContentStart != 0;

			public ReadOnlyMemory<byte> Boundary { get; private set; }

			public ReadOnlyMemory<byte> EndBoundary
			{
				get => _endBoundary;
				set
				{
					_endBoundary = value;
					Boundary = value.Slice(0, value.Length - 2);
				}
			}

			public List<MimePartBuilder> Children { get; set; }
			public MimePartBuilder Parent { get; set; }

			public MimePartSpan ToMimePart()
			{
				return new MimePartSpan(
					new MessageSpan(Start, End - Start),
					new MessageSpan(Start, ContentStart - 2 - Start), // Remove the CRLF between header and content
					new MessageSpan(ContentStart, End - ContentStart - 2),
					Children?.Select(c => c.ToMimePart()).ToImmutableArray() ?? ImmutableArray<MimePartSpan>.Empty
				);
			}
		}

		private readonly char[] _headerBuffer = new char[50];

		private bool IsImportantHeader(Memory<byte> line)
		{
			var colonIndex = line.Span.IndexOf((byte) ':');
			if (colonIndex == -1)
				return false;
			if (colonIndex >= 50)
				return false;

			var charCount = Encoding.ASCII.GetChars(line.Span.Slice(0, colonIndex), _headerBuffer);
			while (charCount > 0 && Char.IsWhiteSpace(_headerBuffer[charCount]))
			{
				charCount--;
			}

			ReadOnlySpan<char> headerSpan = _headerBuffer.AsSpan(0, charCount);
			return headerSpan.Equals("Content-Type".AsSpan(), StringComparison.OrdinalIgnoreCase);
		}

		private static readonly Regex ContentTypeRegex = new Regex(@"
^
	Content-Type\s*:\s*
	(?<type>[^/]+)
	/
	(?<subtype>[^;]+)
	\s*
	(?:	
		;\s*(?<param>[^=]+)\s*=\s*(?<value>""(?:[^""]|\\.)*""|[^ ()<>@,;:\\""/\[\]?=]+)\s*
	)*
	;?
$", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);

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
