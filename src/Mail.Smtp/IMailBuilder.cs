namespace Vaettir.Mail.Server.Smtp
{
    public interface IMailBuilder
    {
        SmtpMailMessage PendingMail { get; set; }
    }
}