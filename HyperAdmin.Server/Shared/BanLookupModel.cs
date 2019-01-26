namespace HyperAdmin.Shared
{
	public class BanLookupModel
	{
		public string Identifier { get; set; }
		public string BannedBy { get; set; }
		public string Reason { get; set; }
		public long Timestamp { get; set; }
		public long ExpiryTicks { get; set; }
	}
}
