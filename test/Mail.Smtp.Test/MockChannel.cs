using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Smtp;

namespace Vaettir.Mail.Smtp.Test
{
	public class MockChannel : IMessageChannel
	{
		public IList<Entry> Entries { get; } = new List<Entry>();
		public bool IsClosed { get; set; }

		public UserData AuthenticatedUser { get; set; }

		public Task SendReplyAsync(ReplyCode replyCode, bool more, string message, CancellationToken token)
		{
			Entries.Add(new Entry(replyCode, message, more));
			return Task.CompletedTask;
		}

		public Task SendReplyAsync(ReplyCode replyCode, IEnumerable<string> messages, CancellationToken cancellationToken)
		{
			List<string> list = messages.ToList();
			for (var index = 0; index < list.Count; index++)
			{
				Entries.Add(new Entry(replyCode, list[index], index != list.Count - 1));
			}
			return Task.CompletedTask;
		}

		public string ConnectedHost { get; set; }
		public bool IsAuthenticated => AuthenticatedUser != null;

		public void Close()
		{
			IsClosed = true;
		}

		public class Entry
		{
			public Entry(ReplyCode code, string message, bool more)
			{
				Code = code;
				Message = message;
				More = more;
			}

			public ReplyCode Code { get; }
			public string Message { get; }
			public bool More { get; }

			public override string ToString()
			{
				return $"{(int) Code}{(More ? "-" : " ")}{Message}";
			}
		}
	}
}
