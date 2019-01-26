using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using HyperAdmin.Shared;
using HyperAdmin.Server.Models;
using Newtonsoft.Json;

namespace HyperAdmin.Server
{
	public class Server : BaseScript
	{
		internal ServerConfigModel Config { get; private set; }
		internal HttpManager Http { get; }
		internal BanManager Bans { get; }
		internal PlayerController PlayerContext { get; }
		internal ServerManager ServerContext { get; }
		internal PermissionsManager Permissions { get; }

		public Server() {
			RegisterEventHandler( "HyperAdmin.Ready", new Action<Player>( OnReady ) );

			LoadConfig();
			Http = new HttpManager( this );
			Bans = new BanManager( this );
			PlayerContext = new PlayerController( this );
			ServerContext = new ServerManager( this );
			Permissions = new PermissionsManager( this );
			new VersionCheck( this );
		}

		private void OnReady( [FromSource] Player source ) {
			try {
				source.TriggerEvent( "HyperAdmin.Config", JsonConvert.SerializeObject( Config.GlobalConfig ) );
				TriggerClientEvent( "HyperAdmin.Connect", int.Parse( source.Handle ) );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		internal void LoadConfig() {
			try {
				var path = $"{API.GetResourcePath( API.GetCurrentResourceName() )}/config.json";
				if( !File.Exists( path ) ) {
					Log.Verbose( $"Could not find configuration file at {path} -- Creating New" );
					Config = new ServerConfigModel();
					using( var o = File.CreateText( path ) ) {
						o.Write( JsonConvert.SerializeObject( Config, Formatting.Indented ) );
						o.Flush();
					}
				}
				else {
					Config = JsonConvert.DeserializeObject<ServerConfigModel>( File.ReadAllText( path ) );
					Log.Info( "Loaded HyperAdmin Configuration" );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
				API.StopResource( API.GetCurrentResourceName() );
			}
		}

		internal UserModel GetUser( string primaryIdentifier ) {
			var path = $"{API.GetResourcePath( API.GetCurrentResourceName() )}/data/players/{primaryIdentifier}.json";
			if( !File.Exists( path ) ) return null;

			var user = JsonConvert.DeserializeObject<UserModel>( File.ReadAllText( path ) );
			return user;
		}

		internal UserModel GetUser( Player source ) {
			var identifier = GetPrimaryIdentifier( source.Identifiers.ToList() );
			if( string.IsNullOrEmpty( identifier ) ) {
				return null;
			}

			var path = $"{API.GetResourcePath( API.GetCurrentResourceName() )}/data/players/{identifier}.json";
			if( !File.Exists( path ) ) {
				using( var o = File.CreateText( path ) ) {
					o.Write( JsonConvert.SerializeObject( new UserModel {
						Identifiers = source.Identifiers.ToList(),
						LastLogin = DateTime.UtcNow,
						Names = { source.Name }
					} ) );
				}
			}

			var user = JsonConvert.DeserializeObject<UserModel>( File.ReadAllText( path ) );
			return user;
		}

		internal void SaveUser( UserModel user ) {
			var identifier = GetPrimaryIdentifier( user.Identifiers );
			var path = $"{API.GetResourcePath( API.GetCurrentResourceName() )}/data/players/{identifier}.json";
			File.WriteAllText( path, JsonConvert.SerializeObject( user, Formatting.Indented ) );
		}

		public void RegisterEventHandler( string eventName, Delegate action ) {
			EventHandlers[eventName] += action;
		}

		public void RegisterTickHandler( Func<Task> tick ) {
			Tick += tick;
		}

		public void DeregisterTickHandler( Func<Task> tick ) {
			Tick -= tick;
		}

		public void TriggerEventForAce( string ace, string eventName, params object[] data ) {
			foreach( var player in new PlayerList() ) {
				if( API.IsPlayerAceAllowed( player.Handle, ace ) ) {
					player.TriggerEvent( eventName, data );
				}
			}
		}

		public string GetPrimaryIdentifier( IEnumerable<string> ids ) {
			var id = ids.FirstOrDefault( p => p.Split( ':' )[0].StartsWith( Config.PrimaryIdentifier ) );
			return string.IsNullOrEmpty( id ) ? "" : id.Substring( id.IndexOf( ":", StringComparison.Ordinal ) + 1 );
		}
	}

	public class ServerAccessor
	{
		protected Server Server { get; }

		public ServerAccessor( Server server ) {
			Server = server;
		}
	}

	public class ServerConfigModel
	{
		public string PrimaryIdentifier { get; set; } = "steam";
		public string DefaultBanReason { get; set; } = "No reason given.";
		public string DefaultKickReason { get; set; } = "No reason given.";
		public bool CheckForUpdates { get; set; } = false;
		public GlobalConfigModel GlobalConfig { get; set; } = new GlobalConfigModel();
	}
}
