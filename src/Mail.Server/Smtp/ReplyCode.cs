namespace Vaettir.Mail.Server.Smtp
{
	public enum ReplyCode
	{
		Status = 211,
		Help = 214,
		Greeting = 220,
		Closing = 221,
		AuthenticationComplete = 235,
		Okay = 250,
		NotLocal = 251,
		CannotVerify = 252,
		AuthenticationFragment = 334,
		StartMail = 354,
		ServiceNotAvailable = 421,
		MailboxUnavailableBusy = 450,
		LocalError = 451,
		InsufficentStorage = 452,
		SyntaxError = 500,
		InvalidArguments = 501,
		CommandNotImplemented = 502,
		BadSequence = 503,
		ParameterNotImplemented = 504,
		MailboxUnavailable = 550,
		UserNotLocal = 551,
		ExceededQuota = 552,
		NameNotAllowed = 553,
		Failed = 554,
	}
}