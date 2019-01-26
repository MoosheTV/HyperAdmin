using System;
using CitizenFX.Core;
using HyperAdmin.Client.Menus;
using HyperAdmin.Shared;
using Newtonsoft.Json;

namespace HyperAdmin.Client.Admin
{
	internal class AdminController : ClientAccessor
	{
		internal AdminMenu Menu { get; }
		internal GlobalConfigModel Config { get; set; } = new GlobalConfigModel();

		public AdminController( Client client ) : base( client ) {
			Menu = new AdminMenu( client );

			client.RegisterEventHandler( "HyperAdmin.Config", new Action<string>( OnConfig ) );

			BaseScript.TriggerServerEvent( "HyperAdmin.Ready" );
		}

		public void OnConfig( string data ) {
			try {
				Config = JsonConvert.DeserializeObject<GlobalConfigModel>( data );

				Client.Menu.RegisterMenuHotkey( (Control)Config.MenuHotKey, Menu );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}
	}

	internal class AdminMenu : Menu
	{
		private readonly Client _client;
		public override bool Enabled => _client.HasPermission( Constants.AceAdmin );

		public AdminMenu( Client client ) : base( "HyperAdmin" ) {
			_client = client;
			var players = new PlayerListMenu( this, client );
			Add( new MenuItemSubMenu( client, this, players, ace: Constants.AceAdminMonitor ) );

			// TODO: Fix Overhead Names (Mooshe)
			//var hudMenu = new HudMenu( client, this );
			//Add( new MenuItemSubMenu( client, this, hudMenu, ace: Constants.AceAdminMonitor ) );

			var serverMenu = new ServerMenu( client, this );
			Add( new MenuItemSubMenu( client, this, serverMenu, ace: Constants.AceAdminMonitor ) );
		}
	}
}
