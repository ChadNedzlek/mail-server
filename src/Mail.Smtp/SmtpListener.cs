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

		public SmtpSettings Settings { get; }

		public SmtpListener() : this(Utility.Settings.Get<SmtpSettings>())
		{
		}

		public SmtpListener(SmtpSettings settings) : base(settings.DefaultPorts)
		{
			Settings = settings;
			Init();
		}

		private void Init()
		{
			ImplementationFactory = SmtpImplementationFactory.Default;
		}

		public IMailStore MailStore { get; set; }
		public IUserStore UserStore { get; set; }

		protected override SmtpSession InitiateSession(SecurableConnection connection, EndPoint local, EndPoint remote)
		{
			return new SmtpSession(
				connection,
				ImplementationFactory,
				Settings,
				((IPEndPoint) local).Address.ToString(),
				((IPEndPoint) remote).Address.ToString(),
				MailStore,
				UserStore);
		}
	}
}
