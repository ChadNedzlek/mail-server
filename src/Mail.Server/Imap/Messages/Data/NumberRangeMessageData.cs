namespace Vaettir.Mail.Server.Imap.Messages.Data
{
	public class NumberRangeMessageData : IMessageData
	{
		public NumberRangeMessageData(int min, int max)
		{
			Min = min;
			Max = max;
		}

		public int Min { get; }
		public int Max { get; }

		public string ToMessageString()
		{
			return Min + ":" + Max;
		}
	}
}