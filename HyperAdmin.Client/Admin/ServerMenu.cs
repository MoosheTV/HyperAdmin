using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using HyperAdmin.Client.Helper;
using HyperAdmin.Client.Menus;
using HyperAdmin.Shared;
using Newtonsoft.Json;

namespace HyperAdmin.Client.Admin
{
	internal class ServerMenu : Menu
	{
		private string _lastLookup;

		public ServerMenu( Client client, Menu parent = null ) : base( "Manage Server", parent ) {
			var gameType = new MenuItem( client, this, "Set Game Type", Constants.AceGameType );
			gameType.Activate += async () => {
				var text = await UiHelper.PromptTextInput( controller: client.Menu );
				if( string.IsNullOrEmpty( text ) ) return;
				BaseScript.TriggerServerEvent( "HyperAdmin.SetGameType", text );
			};
			Add( gameType );

			var mapName = new MenuItem( client, this, "Set Map Name", Constants.AceMapName );
			mapName.Activate += async () => {
				var text = await UiHelper.PromptTextInput( controller: client.Menu );
				if( string.IsNullOrEmpty( text ) ) return;
				BaseScript.TriggerServerEvent( "HyperAdmin.SetMapName", text );
			};
			Add( mapName );

			Add( new MenuItemSubMenu( client, this, new ResourceListMenu( client, this ), "Manage Resources" ) );

			var lookup = new MenuItem( client, this, "Lookup Ban by Identifier", Constants.AceAdminBan );
			lookup.Activate += async () => {
				var inp = await UiHelper.PromptTextInput( _lastLookup, controller: client.Menu );
				if( string.IsNullOrEmpty( inp ) ) return;
				_lastLookup = inp;
				BaseScript.TriggerServerEvent( "HyperAdmin.LookupBan", inp );
			};
			Add( lookup );

			new BanLookupMenu( client, this );
		}
	}

	internal class ResourceListMenu : Menu
	{
		public ResourceListMenu( Client client, ServerMenu parent ) : base( "Resources", parent ) {
			foreach( var resource in new ResourceList() ) {
				Add( new MenuItemSubMenu( client, this, new ResourceMenu( client, resource, this ) ) );
			}
		}
	}

	internal class ResourceMenu : Menu
	{
		public ResourceMenu( Client client, Resource resource, ResourceListMenu parent ) : base( resource.Name, parent ) {
			var status = new ResourceStatusMenuItem( client, this, resource );
			Add( status );

			var start = new MenuItem( client, this, "Start Resource", Constants.AceResourceStart );
			start.Activate += () => {
				BaseScript.TriggerServerEvent( "HyperAdmin.ResourceStart", resource.Name );
				return Task.FromResult( 0 );
			};
			Add( start );

			var stop = new MenuItem( client, this, "Stop Resource", Constants.AceResourceStop );
			stop.Activate += () => {
				BaseScript.TriggerServerEvent( "HyperAdmin.ResourceStop", resource.Name );
				return Task.FromResult( 0 );
			};
			Add( stop );

			var restart = new MenuItem( client, this, "Restart Resource", Constants.AceResourceRestart );
			restart.Activate += () => {
				BaseScript.TriggerServerEvent( "HyperAdmin.ResourceRestart", resource.Name );
				return Task.FromResult( 0 );
			};
			Add( restart );
		}
	}

	internal class ResourceStatusMenuItem : MenuItem
	{
		private Resource Resource { get; }

		public override string SubLabel => Resource.Status;

		public ResourceStatusMenuItem( Client client, Menu owner, Resource resource ) : base( client, owner, "Resource Status" ) {
			Resource = resource;
		}
	}

	internal class BanLookupMenu : Menu
	{
		protected Client Client { get; }

		private string _identifier;
		private string _reason;

		private MenuItem Reason { get; }
		private MenuItem BannedBy { get; }
		private MenuItem Timestamp { get; }
		private MenuItem Expires { get; }
		private MenuItem Unban { get; }

		public BanLookupMenu( Client client, Menu parent ) : base( "Ban Lookup", parent ) {
			Client = client;
			Client.RegisterEventHandler( "HyperAdmin.LookupBan", new Action<string>( OnLookup ) );

			Reason = new MenuItem( client, this, "Reason" );
			Reason.Activate += () => {
				Log.Info( $"Ban Reason: {_reason}" );
				Client.AddChatMessage( "Ban Reason", _reason, 255, 0, 0 );
				return Task.FromResult( 0 );
			};
			Add( Reason );

			BannedBy = new MenuItem( client, this, "Banned By" );
			Add( BannedBy );

			Timestamp = new MenuItem( client, this, "Timestamp" );
			Add( Timestamp );

			Expires = new MenuItem( client, this, "Expires" );
			Add( Expires );

			Unban = new MenuItem( client, this, "Unban Player" );
			Unban.Activate += () => {
				BaseScript.TriggerServerEvent( "HyperAdmin.RemoveBan", _identifier );
				return Task.FromResult( 0 );
			};
			Add( Unban );
		}

		private void OnLookup( string data ) {
			try {
				var model = JsonConvert.DeserializeObject<BanLookupModel>( data );
				_identifier = model.Identifier;

				var expiry = new DateTime( model.ExpiryTicks, DateTimeKind.Utc );
				Expires.SubLabel = expiry.GetTimeLeft();

				var time = new DateTime( model.Timestamp, DateTimeKind.Utc );
				Timestamp.SubLabel = $"{time:MM/dd/yyyy HH:mm:ss} UTC";

				_reason = model.Reason;
				Reason.SubLabel = _reason.Length > 24 ? $"{_reason.Substring( 0, 21 )}..." : _reason;

				BannedBy.SubLabel = model.BannedBy.Length > 24 ? $"{model.BannedBy.Substring( 0, 21 )}..." : model.BannedBy;

				Unban.IsVisible = DateTime.UtcNow < time;

				Client.Menu.CurrentMenu = this;
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}
	}
}
