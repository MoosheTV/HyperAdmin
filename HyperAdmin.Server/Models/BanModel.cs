using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HyperAdmin.Server.Models
{
	public class BanModel
	{
		public List<string> Identifiers { get; set; } = new List<string>();
		public string BanReason { get; set; } = "No Reason Given";
		public DateTime Expires { get; set; } = DateTime.MinValue;
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;
		public string BannedBy { get; set; }

		[JsonIgnore]
		public string BanLength
		{
			get {
				var delta = Expires == DateTime.MaxValue ? -1f : (Expires - DateTime.UtcNow).TotalSeconds;
				string expires;
				if( Expires == DateTime.MaxValue ) {
					expires = "permanently";
				} else if( delta > 86400 ) {
					expires = $"for {delta / 86400:n1} days";
				} else if( delta > 3600 ) {
					expires = $"for {delta / 3600:n1} hours";
				} else if( delta > 60 ) {
					expires = $"for {delta / 60:n1} minutes";
				} else {
					expires = $"for {delta:n1} seconds";
				}
				return expires;
			}
		}
	}
}
