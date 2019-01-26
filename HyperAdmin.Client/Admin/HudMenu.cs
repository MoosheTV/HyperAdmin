using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using HyperAdmin.Client.Helper;
using HyperAdmin.Client.Menus;
using HyperAdmin.Client.Util;
using HyperAdmin.Shared;

namespace HyperAdmin.Client.Admin
{
	internal class HudMenu : Menu
	{
		private static readonly Color TalkColor = Color.FromArgb( 106, 76, 147 );
		private static readonly Color SilentColor = Color.FromArgb( 255, 255, 255 );
		private static readonly Color BgColor = Color.FromArgb( 120, 0, 0, 0 );

		private readonly MenuItemCheckbox _overheadNames;

		public HudMenu( Client client, AdminMenu parent ) : base( "HUD Menu", parent ) {
			client.RegisterTickHandler( OnTick );
			_overheadNames = new MenuItemCheckbox( client, this, "Overhead Names" );
			Add( _overheadNames );
		}

		private async Task OnTick() {
			try {
				if( !_overheadNames.IsChecked.Invoke() ) {
					await BaseScript.Delay( 100 );
					return;
				}

				var playerPos = Game.PlayerPed.Position;
				var camPos = API.GetGameplayCamCoords();
				var forward = MathExtents.GameplayCameraForwardVec();
				var handle = Game.Player.Handle;
				foreach( var player in new PlayerList().Where( p => p.Handle != handle &&
																	p.Character.Position.DistanceToSquared( playerPos ) < 625 &&
																	camPos.ProduceDot( p.Character.Position, forward ) >= 0f ) ) {
					var raycast = World.Raycast( World.RenderingCamera.Position, player.Character.Position, IntersectOptions.Everything );
					var inLineOfSight = raycast.DitHitEntity && raycast.HitEntity.Handle == player.Character.Handle;
					var color = API.NetworkIsPlayerTalking( player.ServerId ) ? TalkColor : SilentColor;
					var bgColor = BgColor;
					if( !inLineOfSight ) {
						color = Color.FromArgb( 60, color );
						bgColor = Color.FromArgb( 60, BgColor );
					}

					UiHelper.Draw3DText( $"[{player.ServerId}] {player.Name}", player.Character.Bones[Bone.IK_Head] + new Vector3( 0f, 0f, 0.2f ), color,
						font: Font.ChaletComprimeCologne, isBoxed: true, boxColor: bgColor );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}
	}
}
