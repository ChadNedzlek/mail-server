using Vaettir.Mail.Server.Smtp;

namespace Vaettir.Mail.Server
{
	public interface IMailSendFailureManager
	{
		void SaveFailureData();
		void RemoveFailure(string mailId);
		SmtpFailureData GetFailure(string mailId, bool createIfMissing);
	}
}
