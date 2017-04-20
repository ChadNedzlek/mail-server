namespace Vaettir.Mail.Server
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
