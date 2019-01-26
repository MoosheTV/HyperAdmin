using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using HyperAdmin.Client.Admin;
using HyperAdmin.Client.Helper;
using HyperAdmin.Client.Menus;
using HyperAdmin.Shared;
using Newtonsoft.Json;

namespace HyperAdmin.Client
{
	public class Client : BaseScript
	{
		internal MenuController Menu { get; }
		internal AdminController Admin { get; }
		internal SpectateController Spectate { get; }
		internal List<string> Permissions { get; set; } = new List<string>();

		public Client() {
			RegisterEventHandler( "UI.ShowNotification", new Action<string>( UiHelper.ShowNotification ) );
			RegisterEventHandler( "HyperAdmin.Permissions", new Action<string>( OnPermissions ) );

			Menu = new MenuController( this );
			Admin = new AdminController( this );
			Spectate = new SpectateController( this );
		}

		public bool HasPermission( string perm ) {
			return Permissions.Any( p => p.Equals( perm, StringComparison.InvariantCultureIgnoreCase ) || p.StartsWith( $"{perm}.", StringComparison.InvariantCultureIgnoreCase ) );
		}

		private void OnPermissions( string perms ) {
			try {
				Permissions = JsonConvert.DeserializeObject<List<string>>( perms );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
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

		public void AddChatMessage( string title, string message, int r = 255, int b = 255, int g = 255 ) {
			var msg = new Dictionary<string, object> {
				["color"] = new[] { r, g, b },
				["args"] = new[] { title, message }
			};
			TriggerEvent( "chat:addMessage", msg );
		}
	}

	public class ClientAccessor
	{
		protected Client Client { get; }

		public ClientAccessor( Client client ) {
			Client = client;
		}
	}
}
