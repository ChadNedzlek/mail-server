using System.Collections.Immutable;

namespace Vaettir.Mail.Mime
{
	public class MimePartSpan
	{
		public MimePartSpan(MessageSpan span, MessageSpan headerSpan, MessageSpan contentSpan) : this(
			span,
			headerSpan,
			contentSpan,
			null,
			ImmutableArray<MimePartSpan>.Empty)
		{
		}

		public MimePartSpan(
			MessageSpan span,
			MessageSpan headerSpan,
			MessageSpan contentSpan,
			string boundary,
			ImmutableArray<MimePartSpan> parts)
		{
			Span = span;
			HeaderSpan = headerSpan;
			ContentSpan = contentSpan;
			Boundary = boundary;
			Parts = parts;
		}

		public MessageSpan Span { get; }
		public MessageSpan HeaderSpan { get; }
		public MessageSpan ContentSpan { get; }

		public bool IsMultiPart => Boundary != null;
		public string Boundary { get; }
		public ImmutableArray<MimePartSpan> Parts { get; }
	}
}
