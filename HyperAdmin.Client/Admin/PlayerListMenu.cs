using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using HyperAdmin.Client.Helper;
using HyperAdmin.Client.Menus;
using HyperAdmin.Shared;
using Newtonsoft.Json;

namespace HyperAdmin.Client.Admin
{
	public class PlayerListMenu : Menu
	{
		protected Client Client { get; }

		internal int LastTpTarget = -1;

		private bool _isFrozen;
		private Vector3 _frozenPos = Vector3.Zero;

		public PlayerListMenu( Menu parent, Client client ) : base( "Online Players", parent ) {
			Client = client;

			API.DecorRegister( "Player.Frozen", 2 );

			client.RegisterTickHandler( OnTick );
			client.RegisterTickHandler( UpdatePlayerList );
			client.RegisterEventHandler( "HyperAdmin.Bring", new Action<string>( OnBring ) );
			client.RegisterEventHandler( "HyperAdmin.TpTo", new Action<int>( OnTeleportTo ) );
			client.RegisterEventHandler( "HyperAdmin.TpToResponse", new Action<int, string>( OnTeleportResponse ) );
			client.RegisterEventHandler( "HyperAdmin.Freeze", new Action( OnFreeze ) );
		}

		private async Task UpdatePlayerList() {
			try {
				var list = new PlayerList();
				var children = new List<int>();
				foreach( var player in list ) {
					children.Add( player.ServerId );
					if( !this.Any( m => m is MenuItemSubMenu s && s.Child is PlayerMenu p && p.ServerId == player.ServerId ) ) {
						OnConnect( player.ServerId );
					}
				}

				RemoveAll( m => m is MenuItemSubMenu s && s.Child is PlayerMenu p && !children.Contains( p.ServerId ) );
				await BaseScript.Delay( 1000 );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private async Task OnTick() {
			try {
				if( !_isFrozen ) {
					await BaseScript.Delay( 100 );
					return;
				}

				Game.Player.CanControlCharacter = false;
				Game.PlayerPed.IsPositionFrozen = true;
				Game.PlayerPed.Position = _frozenPos;
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}

		}

		private void OnFreeze() {
			try {
				_frozenPos = Game.PlayerPed.Position;

				_isFrozen = !_isFrozen;
				API.DecorSetBool( Game.PlayerPed.Handle, "Player.Frozen", _isFrozen );
				Game.Player.CanControlCharacter = !_isFrozen;
				Game.PlayerPed.IsPositionFrozen = _isFrozen;
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnBring( string posData ) {
			try {
				var pos = JsonConvert.DeserializeObject<Vector3>( posData );
				_frozenPos = pos;
				Game.PlayerPed.Task.ClearAllImmediately();
				Game.PlayerPed.Position = pos;
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnTeleportTo( int targetId ) {
			try {
				var pos = Game.PlayerPed.IsInVehicle()
					? Game.PlayerPed.CurrentVehicle.GetOffsetPosition( new Vector3( 0f, 0f, 1f ) ) : Game.PlayerPed.Position;
				BaseScript.TriggerServerEvent( "HyperAdmin.TpToResponse", targetId, JsonConvert.SerializeObject( pos ) );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnTeleportResponse( int targetId, string posData ) {
			try {
				if( LastTpTarget != targetId ) return;

				var target = new PlayerList().FirstOrDefault( p => p.ServerId == targetId );
				if( target == null ) return;

				var pos = JsonConvert.DeserializeObject<Vector3>( posData );

				var position = target.Character.Position.DistanceToSquared( pos ) > 10000f ? target.Character.Position : pos;
				Game.PlayerPed.Position = position;

				LastTpTarget = -1;
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private void OnConnect( int serverId ) {
			try {
				if( this.Any( m => m is MenuItemSubMenu men && men.Child is PlayerMenu p && p.ServerId == serverId ) ) return;

				var playerMenu = new PlayerMenu( serverId, this, Client );
				Add( new MenuItemSubMenu( Client, this, playerMenu, $"[{serverId}] {playerMenu.Title}", Constants.AceAdminMonitor, serverId ) );
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}
	}

	public class PlayerMenu : Menu
	{
		internal int ServerId { get; set; }
		public Player Player => new Player( API.GetPlayerFromServerId( ServerId ) );
		public override string Title => $"{Player.Name}";

		public PlayerMenu( int serverId, PlayerListMenu parent, Client client ) : base( "Player", parent ) {
			ServerId = serverId;

			var spec = new MenuItemCheckbox( client, this, "Spectate Player", false, Constants.AceAdminSpec ) {
				IsChecked = () => client.Spectate.CurrentPlayer?.ServerId == ServerId
			};
			spec.Activate += () => {
				if( client.Spectate.CurrentPlayer == null || client.Spectate.CurrentPlayer.ServerId != ServerId )
					client.Spectate.Start( Player );
				else {
					client.Spectate.Stop();
				}
				return Task.FromResult( 0 );
			};
			Add( spec );

			var tpTo = new MenuItem( client, this, "Teleport To Player", Constants.AceAdminTp );
			tpTo.Activate += () => {
				parent.LastTpTarget = ServerId;
				BaseScript.TriggerServerEvent( "HyperAdmin.TpTo", ServerId );
				return Task.FromResult( 0 );
			};
			Add( tpTo );

			var bring = new MenuItem( client, this, "Bring Player", Constants.AceAdminBring );
			bring.Activate += () => {
				BaseScript.TriggerServerEvent( "HyperAdmin.Bring", ServerId, JsonConvert.SerializeObject( Game.PlayerPed.Position ) );
				return Task.FromResult( 0 );
			};
			Add( bring );

			var freeze = new MenuItemCheckbox( client, this, "Freeze Player", Player.Character.IsPositionFrozen, Constants.AceAdminFreeze ) {
				IsChecked = () => API.DecorGetBool( Player.Character.Handle, "Player.Frozen" )
			};
			freeze.Activate += () => {
				BaseScript.TriggerServerEvent( "HyperAdmin.Freeze", ServerId );
				return Task.FromResult( 0 );
			};
			Add( freeze );

			var kick = new MenuItemSubMenu( client, this, new KickMenu( client, this ), ace: Constants.AceAdminKick );
			Add( kick );

			var ban = new MenuItemSubMenu( client, this, new BanMenu( client, this ), ace: Constants.AceAdminBan );
			Add( ban );
		}

		internal void Ban( string reason, int until ) {
			BaseScript.TriggerServerEvent( "HyperAdmin.AddBan", $"net:{ServerId}", reason.Trim(), until );
		}

		internal void Kick( string reason ) {
			BaseScript.TriggerServerEvent( "HyperAdmin.Kick", ServerId, reason.Trim() );
		}
	}

	internal class KickMenu : Menu
	{
		private string _reason = "";

		public KickMenu( Client client, PlayerMenu parent ) : base( $"Kick {parent.Player.Name}", parent ) {
			var reason = new MenuItem( client, this, "Kick Reason" ) {
				SubLabel = "None Specified"
			};
			reason.Activate += async () => {
				_reason = await UiHelper.PromptTextInput( "", controller: client.Menu );
				reason.SubLabel = _reason.Length > 16 ? $"{_reason.Substring( 0, 16 )}..." : _reason;
			};
			Add( reason );

			var submit = new MenuItem( client, this, "Kick Player" );
			submit.Activate += () => {
				client.Menu.CurrentMenu = parent.Parent;
				parent.Kick( _reason );
				return Task.FromResult( 0 );
			};
			Add( submit );
		}
	}

	internal class BanMenu : Menu
	{
		private string _reason = "";
		private bool _perma;

		private static readonly Dictionary<string, int> Units = new Dictionary<string, int> {
			["minutes"] = 60,
			["hours"] = 3600,
			["days"] = 86400
		};

		public BanMenu( Client client, PlayerMenu parent ) : base( $"Ban {parent.Player.Name}", parent ) {
			var reason = new MenuItem( client, this, "Ban Reason" ) {
				SubLabel = "None Specified"
			};
			reason.Activate += async () => {
				_reason = await UiHelper.PromptTextInput( "", controller: client.Menu );
				reason.SubLabel = _reason.Length > 16 ? $"{_reason.Substring( 0, 16 )}..." : _reason;
			};
			Add( reason );

			var length = new MenuItemSpinnerInt( client, this, "Ban Duration", 60, 1, 8192, 1, true );
			Add( length );

			var lengthUnit = new MenuItemSpinnerList<string>( client, this, "Ban Duration (Units)", Units.Keys.ToList(), 0, true );
			Add( lengthUnit );

			var perma = new MenuItemCheckbox( client, this, "Permanent Ban" );
			perma.Activate += () => {
				_perma = !_perma;
				lengthUnit.IsVisible = length.IsVisible = _perma;
				return Task.FromResult( 0 );
			};
			Add( perma );

			var submit = new MenuItem( client, this, "Execute Ban" );
			submit.Activate += () => {
				var time = _perma ? int.MinValue : Units.ElementAt( (int)lengthUnit.Value ).Value * (int)length.Value;
				client.Menu.CurrentMenu = parent.Parent;
				parent.Ban( _reason, time );
				return Task.FromResult( 0 );
			};
			Add( submit );
		}
	}

}
