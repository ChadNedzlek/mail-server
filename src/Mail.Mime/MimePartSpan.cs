using System.Collections.Immutable;
using System.Diagnostics;

namespace Vaettir.Mail.Mime
{
	[DebuggerDisplay("{Span,nq} ({HeaderSpan,nq} + {ContentSpan,nq})")]
	public class MimePartSpan
	{
		public MimePartSpan(MessageSpan span, MessageSpan headerSpan, MessageSpan contentSpan) : this(
			span,
			headerSpan,
			contentSpan,
			ImmutableArray<MimePartSpan>.Empty)
		{
		}

		public MimePartSpan(
			MessageSpan span,
			MessageSpan headerSpan,
			MessageSpan contentSpan,
			ImmutableArray<MimePartSpan> parts)
		{
			Span = span;
			HeaderSpan = headerSpan;
			ContentSpan = contentSpan;
			Parts = parts;
		}

		public MessageSpan Span { get; }
		public MessageSpan HeaderSpan { get; }
		public MessageSpan ContentSpan { get; }

		public bool IsMultiPart => !Parts.IsEmpty;
		public ImmutableArray<MimePartSpan> Parts { get; }
	}
}
