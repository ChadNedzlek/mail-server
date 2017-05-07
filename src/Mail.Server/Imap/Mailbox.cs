using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Vaettir.Mail.Server.Imap
{
	public class Mailbox
	{
		public bool IsReadOnly { get; }
		public bool IsSelectable { get; private set; }
		public ObservableCollection<MailMessage> Messages { get; private set; }
		public ObservableCollection<MailMessage> Recent { get; private set; }
		public string FullName { get; set; }
		public int NextUid { get; private set; }
		public int UidValidity { get; private set; }
		public int FirstUnseen { get; private set; }
		public ImmutableList<string> Flags { get; private set; }
	}
}