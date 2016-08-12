namespace Vaettir.Mail.Server.Smtp
{
	public class SmtpSettings
	{
		public int[] DefaultPorts { get; set; }
		public string DomainName { get; set; }
		public string MailStorePath { get; set; }
	}
}