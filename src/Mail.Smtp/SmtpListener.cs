using System.Collections.Generic;
using System.Net;
using System.Text;
using MailServer;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.Smtp
{
	public class SmtpListener : ProtocolListener<SmtpSession>
	{
		public SmtpImplementationFactory ImplementationFactory { get; private set; }

		public SmtpListener() : this(Settings.Get<SmtpSettings>().DefaultPorts)
		{
		}

		public SmtpListener(string ports) : base(ports)
		{
			Init();
		}

		public SmtpListener(IEnumerable<int> ports) : base(ports)
		{
			Init();
		}

		private void Init()
		{
			ImplementationFactory = SmtpImplementationFactory.Default;
			DomainName = Settings.Get<SmtpSettings>().DomainName;
		}

		public string DomainName { get; set; }
		public IMailStore MailStore { get; set; }
		public IUserStore UserStore { get; set; }

		protected override SmtpSession InitiateSession(SecurableConnection connection, EndPoint local, EndPoint remote)
		{
			return new SmtpSession(
				connection,
				ImplementationFactory,
				DomainName,
				((IPEndPoint) local).Address.ToString(),
				((IPEndPoint) remote).Address.ToString(),
				MailStore,
				UserStore);
		}
	}
}
