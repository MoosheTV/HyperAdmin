using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using HyperAdmin.Server.Models;
using HyperAdmin.Shared;
using Newtonsoft.Json;

namespace HyperAdmin.Server
{
	internal class BanManager : ServerAccessor
	{
		internal List<BanModel> ActiveBans { get; set; }
		internal List<BanModel> AllBans { get; set; }

		public BanManager( Server server ) : base( server ) {
			LoadBanList();
			server.RegisterEventHandler( "HyperAdmin.AddBan", new Action<Player, string, string, int>( OnAddBan ) );
			server.RegisterEventHandler( "HyperAdmin.RemoveBan", new Action<Player, string>( OnRemoveBan ) );
			server.RegisterEventHandler( "HyperAdmin.LookupBan", new Action<Player, string>( OnLookup ) );
		}

		private void OnLookup( [FromSource] Player source, string identifier ) {
			try {
				if( !API.IsPlayerAceAllowed( source.Handle, Constants.AceAdminBan ) ) {
					Log.Warn( $"Player {source.Name} (net:{source.Handle}) tried to illegally lookup bans tied to {identifier}" );
					return;
				}

				var ban = ActiveBans.FirstOrDefault( b => b.Identifiers.Any( i => i.Contains( identifier ) ) );
				if( ban == null ) {
					source.TriggerEvent( "UI.ShowNotification", "~r~Error~s~: No active bans are tied to that identifier." );
					return;
				}

				var id = Server.GetPrimaryIdentifier( ban.Identifiers );
				if( string.IsNullOrEmpty( id ) )
					id = identifier;

				var bannedBy = ban.BannedBy.Substring( 0,
					ban.BannedBy.Contains( "(" ) ? ban.BannedBy.LastIndexOf( "(", StringComparison.InvariantCultureIgnoreCase ) : ban.BannedBy.Length )
					.Trim();
				var model = new BanLookupModel {
					Identifier = id,
					BannedBy = bannedBy,
					Reason = ban.BanReason,
					ExpiryTicks = ban.Expires.Ticks,
					Timestamp = ban.Timestamp.Ticks
				};
				source.TriggerEvent( "HyperAdmin.LookupBan", JsonConvert.SerializeObject( model ) );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnRemoveBan( [FromSource] Player source, string identifier ) {
			try {
				if( !API.IsPlayerAceAllowed( source.Handle, Constants.AceAdminBan ) ) {
					Log.Warn( $"Player {source.Name} (net:{source.Handle}) tried to illegally unban {identifier}" );
					return;
				}

				var amt = RemoveBan( $"{source.Name} ({Server.Config.PrimaryIdentifier}:{Server.GetPrimaryIdentifier( source.Identifiers.ToList() )})", identifier );
				source.TriggerEvent( "UI.ShowNotification", $"Removed ~y~{amt}~s~ ban{(amt == 1 ? "" : "s")} correlating with ~g~{identifier}~s~" );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		internal int RemoveBan( string actor, string identifier ) {
			var removed = 0;
			foreach( var ban in ActiveBans ) {
				if( !ban.Identifiers.Any( i => i.Contains( identifier ) ) ) continue;

				Log.Warn( $"Ban (Lasted {ban.BanLength}, reason: {ban.BanReason}) was removed by {actor}." );
				ban.Expires = DateTime.UtcNow.Subtract( new TimeSpan( 0, 0, 1 ) );
				removed++;
			}

			if( removed > 0 ) {
				SaveBanList();
			}

			return removed;
		}

		private void OnAddBan( [FromSource] Player source, string targetIdentifier, string reason, int expiry ) {
			try {
				if( !API.IsPlayerAceAllowed( source.Handle, Constants.AceAdminBan ) ) {
					Log.Warn( $"Player {source.Name} (net:{source.Handle}) tried to illegally ban {targetIdentifier} for reason {reason}" );
					return;
				}
				var primaryId = $"{Server.Config.PrimaryIdentifier}:{Server.GetPrimaryIdentifier( source.Identifiers.ToList() )}";

				UserModel user = null;
				if( targetIdentifier.StartsWith( "net:" ) ) {
					var target = new PlayerList().FirstOrDefault( p => int.Parse( p.Handle ) == int.Parse( targetIdentifier.Split( ':' )[1] ) );
					user = Server.GetUser( target );
				}
				else if( targetIdentifier.StartsWith( $"{Server.Config.PrimaryIdentifier}:" ) ) {
					user = Server.GetUser( targetIdentifier.Substring( targetIdentifier.IndexOf( ':' ) + 1 ).Split( ' ' )[0] );
				}

				if( user == null ) {
					user = new UserModel {
						Identifiers = { targetIdentifier }
					};
				}

				AddBan( user.Identifiers, $"{source.Name} ({primaryId})", reason, expiry == int.MinValue ? DateTime.MaxValue : DateTime.UtcNow.AddSeconds( expiry ) );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		internal void AddBan( List<string> identifiers, string bannedBy = "", string reason = "", DateTime? expires = null ) {
			var expiry = expires ?? DateTime.MaxValue;
			var ban = new BanModel {
				BannedBy = string.IsNullOrEmpty( bannedBy ) ? "System" : bannedBy,
				BanReason = string.IsNullOrEmpty( reason ) ? Server.Config.DefaultBanReason : reason,
				Expires = expiry,
				Identifiers = identifiers
			};
			if( expiry > DateTime.UtcNow )
				ActiveBans.Add( ban );
			AllBans.Add( ban );

			Log.Warn( $"{ban.BannedBy} Added ban {ban.BanLength} to identifiers ({string.Join( ", ", identifiers )}). Reason: {ban.BanReason}" );
			SaveBanList();

			var target = new PlayerList().FirstOrDefault( p => p.Identifiers.Any( identifiers.Contains ) );
			var id = target != null ? target.Name : identifiers.FirstOrDefault();

			target?.Drop( $"You are banned {ban.BanLength}. Reason: {ban.BanReason}" );

			Server.TriggerEventForAce( Constants.AceAdmin, "UI.ShowNotification",
				$"~r~{id}~s~ has been banned {ban.BanLength} by ~g~{ban.BannedBy}~s~.~n~~y~Reason~s~: {ban.BanReason}" );
		}

		internal void SaveBanList() {
			try {
				var path = $"{API.GetResourcePath( API.GetCurrentResourceName() )}/bans.json";
				File.WriteAllText( path, JsonConvert.SerializeObject( AllBans, Formatting.Indented ) );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		internal void LoadBanList() {
			try {
				var path = $"{API.GetResourcePath( API.GetCurrentResourceName() )}/bans.json";
				if( !File.Exists( path ) ) {
					Log.Verbose( $"Could not find ban list at {path}" );
				}
				else {
					AllBans = JsonConvert.DeserializeObject<List<BanModel>>( File.ReadAllText( path ) );
					ActiveBans = AllBans.Where( b => b.Expires > DateTime.UtcNow ).ToList();
					Log.Info( $"Loaded {ActiveBans.Count} Active Bans from {path}" );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}
	}
}
