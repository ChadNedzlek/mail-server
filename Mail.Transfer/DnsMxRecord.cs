using DnsClient;

namespace Vaettir.Mail.Transfer
{
	public struct DnsMxRecord
	{
		public DnsMxRecord(DnsString exchange, int preference)
		{
			Exchange = exchange;
			Preference = preference;
		}

		public DnsString Exchange { get; }
		public int Preference { get; }
	}
}