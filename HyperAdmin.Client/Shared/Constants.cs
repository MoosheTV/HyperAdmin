using System.Collections.Generic;

namespace HyperAdmin.Shared
{
	public class Constants
	{
		public const string AceAdmin = "hyperadmin";
		public const string AceAdminBan = "hyperadmin.ban";
		public const string AceAdminKick = "hyperadmin.kick";
		public const string AceAdminSpec = "hyperadmin.spectate";
		public const string AceAdminFreeze = "hyperadmin.freeze";
		public const string AceAdminMonitor = "hyperadmin.monitor";
		public const string AceAdminTp = "hyperadmin.tp";
		public const string AceAdminBring = "hyperadmin.bring";
		public const string AceResourceStart = "hyperadmin.resourcestart";
		public const string AceResourceStop = "hyperadmin.resourcestop";
		public const string AceResourceRestart = "hyperadmin.resourcerestart";
		public const string AceGameType = "hyperadmin.gametype";
		public const string AceMapName = "hyperadmin.mapname";

		public static readonly List<string> Aces = new List<string> {
			AceAdmin, AceAdminBan, AceAdminKick, AceAdminSpec, AceAdminFreeze,
			AceAdminMonitor, AceAdminTp, AceAdminBring, AceResourceStart,
			AceResourceStop, AceResourceRestart, AceGameType, AceMapName
		};
	}
}
