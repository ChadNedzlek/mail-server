using DnsClient;

namespace Vaettir.Mail.Transfer
{
	public struct DnsMxRecord
	{
		public DnsMxRecord(string exchange, int preference)
		{
			Exchange = exchange;
			Preference = preference;
		}

		public string Exchange { get; }
		public int Preference { get; }
	}
}