using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using HyperAdmin.Shared;
using Newtonsoft.Json;

namespace HyperAdmin.Server
{
	internal class PlayerController : ServerAccessor
	{
		public PlayerController( Server server ) : base( server ) {
			server.RegisterEventHandler( "HyperAdmin.TpTo", new Action<Player, int>( OnTeleportTo ) );
			server.RegisterEventHandler( "HyperAdmin.Bring", new Action<Player, int, string>( OnBring ) );
			server.RegisterEventHandler( "HyperAdmin.Kick", new Action<Player, int, string>( OnKick ) );
			server.RegisterEventHandler( "HyperAdmin.TpToResponse", new Action<Player, int, string>( OnTeleportToResponse ) );
			server.RegisterEventHandler( "HyperAdmin.Freeze", new Action<Player, int>( OnFreeze ) );

			server.RegisterEventHandler( "playerConnecting", new Action<Player, string, CallbackDelegate>( OnPreLoad ) );
		}

		private void OnPreLoad( [FromSource] Player source, string name, CallbackDelegate kick ) {
			try {
				var user = Server.GetUser( source );
				if( user == null ) {
					kick( $"Missing {Server.Config.PrimaryIdentifier} -- Make sure you are logged into all required accounts!" );
					API.CancelEvent();
					return;
				}
				var newIdentifiers = new List<string>();
				foreach( var id in source.Identifiers ) {
					if( !user.Identifiers.Contains( id ) ) {
						user.Identifiers.Add( id );
						newIdentifiers.Add( id );
					}
				}
				if( !user.Names.Contains( source.Name ) ) {
					user.Names.Add( source.Name );
				}

				var ban = Server.Bans.ActiveBans.FirstOrDefault( b => b.Expires > DateTime.UtcNow && b.Identifiers.Any( i => user.Identifiers.Contains( i ) ) );
				if( ban != null ) {
					kick( $"You are banned {ban.BanLength}. Reason: {ban.BanReason}" );
					API.CancelEvent();

					if( newIdentifiers.Any() ) {
						ban.Identifiers.AddRange( newIdentifiers );
						Server.Bans.SaveBanList();
					}

					Server.SaveUser( user );
					return;
				}

				user.LastLogin = DateTime.UtcNow;
				Server.SaveUser( user );
			}
			catch( Exception ex ) {
				Log.Error( ex );

				Log.Error( $"Failed to load user {source.Name} ({string.Join( ", ", source.Identifiers )} -- Dropping client." );
				kick( "Failed to load user data" );
				API.CancelEvent();
			}
		}

		private void OnKick( [FromSource] Player source, int targetId, string reason ) {
			try {
				if( !API.IsPlayerAceAllowed( source.Handle, Constants.AceAdminKick ) ) {
					Log.Warn( $"Player {source.Name} (net:{source.Handle}) attempted to kick #{targetId} without Ace: {Constants.AceAdminKick}" );
					return;
				}

				var target = new PlayerList().FirstOrDefault( p => int.Parse( p.Handle ) == targetId );
				if( target == null ) return;

				reason = string.IsNullOrEmpty( reason ) ? Server.Config.DefaultKickReason : reason;

				Server.TriggerEventForAce( Constants.AceAdminKick, $"~r~{target.Name}~s~ was kicked by ~g~~s~.~n~~g~Reason~s: {reason}" );

				target.Drop( reason );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnFreeze( [FromSource] Player source, int targetId ) {
			try {
				if( !API.IsPlayerAceAllowed( source.Handle, Constants.AceAdminFreeze ) ) {
					Log.Warn( $"Player {source.Name} (net:{source.Handle}) attempted to toggle freeze on #{targetId} without Ace: {Constants.AceAdminFreeze}" );
					return;
				}

				var plist = new PlayerList();
				var target = plist.FirstOrDefault( p => int.Parse( p.Handle ) == targetId );
				if( target == null ) return;

				target.TriggerEvent( "HyperAdmin.Freeze" );

				Log.Warn( $"Player {source.Name} (net:{source.Handle}) toggled freeze on {target.Name} (net:{targetId})." );
				foreach( var player in plist ) {
					if( API.IsPlayerAceAllowed( player.Handle, Constants.AceAdmin ) ) {
						player.TriggerEvent( "UI.ShowNotification", $"~g~{source.Name}~s~ toggled freeze on ~g~{target.Name}~s~." );
						return;
					}
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}

		}

		private void OnBring( [FromSource] Player source, int targetId, string posData ) {
			try {
				if( !API.IsPlayerAceAllowed( source.Handle, Constants.AceAdminBring ) ) {
					Log.Warn( $"Player {source.Name} (net:{source.Handle}) attempted to bring #{targetId} without Ace: {Constants.AceAdminBring}" );
					return;
				}

				var plist = new PlayerList();
				var target = plist.FirstOrDefault( p => int.Parse( p.Handle ) == targetId );
				if( target == null ) return;

				JsonConvert.DeserializeObject<Vector3>( posData );
				target.TriggerEvent( "HyperAdmin.Bring", posData );

				Log.Warn( $"Player {source.Name} (net:{source.Handle}) brought {target.Name} (net:{targetId}) to them." );
				foreach( var player in plist ) {
					if( API.IsPlayerAceAllowed( player.Handle, Constants.AceAdmin ) ) {
						player.TriggerEvent( "UI.ShowNotification", $"~g~{source.Name}~s~ has brought ~g~{target.Name}~s~ to them." );
						return;
					}
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnTeleportTo( [FromSource] Player source, int targetId ) {
			try {
				if( !API.IsPlayerAceAllowed( source.Handle, Constants.AceAdminTp ) ) {
					Log.Warn( $"Player {source.Name} (net:{source.Handle}) attempted to teleport without Ace: {Constants.AceAdminTp}" );
					return;
				}

				var target = new PlayerList().FirstOrDefault( p => int.Parse( p.Handle ) == targetId );
				target?.TriggerEvent( "HyperAdmin.TpTo", int.Parse( source.Handle ) );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnTeleportToResponse( [FromSource] Player source, int targetId, string posData ) {
			try {
				var target = new PlayerList().FirstOrDefault( p => int.Parse( p.Handle ) == targetId );
				if( target == null ) return;

				var pos = JsonConvert.DeserializeObject<Vector3>( posData );
				Log.Verbose( $"Teleporting {target.Name} (net:{target.Handle}) to {source.Name} (net:{target.Handle}): {pos}" );
				target.TriggerEvent( "HyperAdmin.TpToResponse", int.Parse(source.Handle), JsonConvert.SerializeObject( pos ) );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}
	}
}
