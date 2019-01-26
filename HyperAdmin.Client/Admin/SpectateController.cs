using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using HyperAdmin.Client.Helper;
using HyperAdmin.Shared;

namespace HyperAdmin.Client.Admin
{
	internal class SpectateController : ClientAccessor
	{
		#region Constants
		private const Font RenderedFont = Font.ChaletLondon;
		private const float PositionX = 0.05f;
		private const float PositionY = 0.3f;
		private const float TextScale = 0.26f;
		private const float MinWidth = 0.18f;
		private const float LineHeight = 0.028f;
		private const float WidthPadding = 0.025f;
		private static readonly Color BgColor = Color.FromArgb( 120, 0, 0, 0 );
		private static readonly Color TextColor = Color.FromArgb( 255, 255, 255 );
		#endregion

		internal Player CurrentPlayer { get; private set; }

		public SpectateController( Client client ) : base( client ) {
			client.RegisterTickHandler( OnTick );
		}

		private async Task OnTick() {
			try {
				if( !IsSpectating() ) {
					await BaseScript.Delay( 500 );
					return;
				}

				var data = GetData();
				var offsetY = -LineHeight;
				var width = Math.Max( MinWidth, GetMaxWidth( data, TextScale, RenderedFont ) );
				var height = data.Count * LineHeight;
				UiHelper.DrawRect( PositionX + width / 2, PositionY + height / 2, width + WidthPadding, height + LineHeight, BgColor );
				foreach( var kvp in data ) {
					offsetY += LineHeight;
					if( string.IsNullOrEmpty( kvp.Value ) ) continue;
					UiHelper.DrawText( $"{kvp.Key}: {kvp.Value}", new Vector2( PositionX, PositionY + offsetY ),
						TextColor, TextScale, font: RenderedFont );
				}
			}
			catch( Exception ex ) {
				Log.Error( ex );
			}
		}

		private float GetMaxWidth( Dictionary<string, string> data, float scale, Font font ) {
			var width = 0f;
			foreach( var kvp in data ) {
				if( string.IsNullOrEmpty( kvp.Value ) ) continue;

				var w = UiHelper.GetStringWidth( $"{kvp.Key}: {kvp.Value}", scale, font );
				if( width < w )
					width = w;
			}
			return width;
		}

		private Dictionary<string, string> GetData() {
			var ped = CurrentPlayer.Character;
			var data = new Dictionary<string, string> {
				["Name"] = CurrentPlayer.Name,
				["Net ID"] = $"{CurrentPlayer.ServerId}",
				["Health"] = $"{ped.Health}/{Math.Max( ped.Health, ped.MaxHealth )}",
				["Armor"] = $"{ped.Armor}/100",
				["Is Talking"] = $"{(API.NetworkIsPlayerTalking( CurrentPlayer.Handle ) ? "~g~True" : "~r~False")}",
				["Is Visible"] = $"{(ped.IsVisible ? "~g~True" : "~r~False")}",
				["-------"] = "",
			};

			var veh = ped.CurrentVehicle;
			if( veh != null ) {
				data.Add( "Engine Health", $"{veh.EngineHealth:n0}/1000" );
				data.Add( "Body Health", $"{veh.BodyHealth:n0}/1000" );
				data.Add( "Speed", $"{veh.Speed / 0.621371f:n1} MP/H" );
				data.Add( "RPM", $"{veh.CurrentRPM:n2}" );
				data.Add( "Current Gear", $"{veh.CurrentGear}" );
				data.Add( "------------", "" );
			}

			data.Add( "Position", $"{ped.Position}" );
			data.Add( "Heading", $"{ped.Heading:n3}" );
			return data;
		}

		public bool IsSpectating() {
			return CurrentPlayer != null && API.NetworkIsInSpectatorMode();
		}

		public void Start( Player player ) {
			if( IsSpectating() ) {
				Stop();
			}
			if( player == Game.Player ) return;

			API.NetworkSetInSpectatorMode( true, player.Character.Handle );
			API.NetworkOverrideReceiveRestrictions( player.Handle, true );
			CurrentPlayer = player;
		}

		public void Stop() {
			API.NetworkSetInSpectatorMode( false, Game.PlayerPed.Handle );
			if( CurrentPlayer != null )
				API.NetworkOverrideReceiveRestrictions( CurrentPlayer.Handle, false );
			CurrentPlayer = null;
		}
	}
}
