using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using HyperAdmin.Shared;

namespace HyperAdmin.Server
{
	public class VersionCheck : ServerAccessor
	{
		private const int Version = 100;

		private bool _needsUpdate;

		public VersionCheck( Server server ) : base( server ) {
			Task.Factory.StartNew( async () => {
				try {
					if( !server.Config.CheckForUpdates ) {
						Log.Verbose( "Suppressing version check for HyperAdmin. This feature is not recommended." );
						return;
					}

					if( !int.TryParse(
						await Server.Http.DownloadString( "https://raw.githubusercontent.com/MoosheTV/HyperAdmin/master/version" ),
						out var version ) ) {
						Log.Error( "\r\n***\r\nFailed to check for updates.\r\n***\r\n" );
						return;
					}

					if( Version != version ) {
						Log.Warn(
							"\r\n***\r\nA new update is available! Make sure to check it out here:\r\nhttps://github.com/MoosheTV/HyperAdmin/releases\r\n\r\n***\r\n" );
						_needsUpdate = true;
					}
					else {
						Log.Verbose( $"You have the latest version of HyperAdmin ({version})" );
					}
				}
				catch( Exception ) {
					// Ignored
				}
			} );
			Server.RegisterEventHandler( "HyperAdmin.Ready", new Action<Player>( OnClientReady ) );
		}

		private void OnClientReady( [FromSource] Player source ) {
			try {
				if( !_needsUpdate || !API.IsPlayerAceAllowed( source.Handle, Constants.AceAdminMonitor ) ) return;

				source.TriggerEvent( "UI.ShowNotification", "~y~WARNING~s~:~n~Your version of HyperAdmin is ~r~Outdated~s~." );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}
	}
}
