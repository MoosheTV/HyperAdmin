using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using HyperAdmin.Shared;

namespace HyperAdmin.Server
{
	internal class ServerManager : ServerAccessor
	{
		public ServerManager( Server server ) : base( server ) {
			server.RegisterEventHandler( "HyperAdmin.ResourceStart", new Action<Player, string>( OnResourceStartRequest ) );
			server.RegisterEventHandler( "HyperAdmin.ResourceStop", new Action<Player, string>( OnResourceStopRequest ) );
			server.RegisterEventHandler( "HyperAdmin.ResourceRestart", new Action<Player, string>( OnResourceRestartRequest ) );
			server.RegisterEventHandler( "HyperAdmin.SetGameType", new Action<Player, string>( OnSetGameType ) );
			server.RegisterEventHandler( "HyperAdmin.SetMapName", new Action<Player, string>( OnSetMapName ) );
		}

		private void OnSetMapName( [FromSource] Player source, string mapName ) {
			try {
				if( !API.IsPlayerAceAllowed( source.Handle, Constants.AceMapName ) ) {
					Log.Warn( $"Player {source.Name} tried illegally changing the map name to {mapName}" );
					return;
				}

				if( string.IsNullOrEmpty( mapName ) ) return;

				API.SetMapName( mapName );
				Server.TriggerEventForAce( Constants.AceMapName, "UI.ShowNotification", $"~g~{source.Name}~s~ has set the map name to ~y~{mapName}~s~." );
				Log.Info( $"Player {source.Name} has set the map name to {mapName}." );
			} catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnSetGameType( [FromSource] Player source, string gameType ) {
			try {
				if( !API.IsPlayerAceAllowed( source.Handle, Constants.AceGameType ) ) {
					Log.Warn( $"Player {source.Name} tried illegally changing the game type to {gameType}" );
					return;
				}

				if( string.IsNullOrEmpty( gameType ) ) return;

				API.SetGameType( gameType );
				Server.TriggerEventForAce( Constants.AceGameType, "UI.ShowNotification", $"~g~{source.Name}~s~ has set the game type to ~y~{gameType}~s~." );
				Log.Info( $"Player {source.Name} has set the game type to {gameType}." );
			} catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnResourceStartRequest( [FromSource] Player source, string resName ) {
			try {
				if( !API.IsPlayerAceAllowed( source.Handle, Constants.AceResourceStart ) ) {
					Log.Warn( $"Player {source.Name} tried illegally starting resource {resName}" );
					return;
				}

				var resource = new Resource( resName );
				if( !resource.Exists ) return;

				resource.Start();
			} catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnResourceStopRequest( [FromSource] Player source, string resName ) {
			try {
				if( !API.IsPlayerAceAllowed( source.Handle, Constants.AceResourceStop ) ) {
					Log.Warn( $"Player {source.Name} tried illegally stopping resource {resName}" );
					return;
				}
				var resource = new Resource( resName );
				if( !resource.Exists ) return;

				resource.Stop();
			} catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnResourceRestartRequest( [FromSource] Player source, string resName ) {
			try {
				if( !API.IsPlayerAceAllowed( source.Handle, Constants.AceResourceRestart ) ) {
					Log.Warn( $"Player {source.Name} tried illegally restarting resource {resName}" );
					return;
				}

				var resource = new Resource( resName );
				if( !resource.Exists ) return;

				resource.Start();
			} catch( Exception ex ) {
				Log.Error( ex );
			}
		}

	}
}
