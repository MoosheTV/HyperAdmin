using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using HyperAdmin.Shared;
using Newtonsoft.Json;

namespace HyperAdmin.Server
{
	internal class PermissionsManager : ServerAccessor
	{
		private readonly Dictionary<int, List<string>> _perms = new Dictionary<int, List<string>>();

		public PermissionsManager( Server server ) : base( server ) {
			server.RegisterTickHandler( OnTick );
		}

		private async Task OnTick() {
			try {
				var list = new List<int>();
				foreach( var player in new PlayerList() ) {
					var id = int.Parse( player.Handle );
					list.Add( id );
					if( !_perms.ContainsKey( id ) ) {
						_perms[id] = new List<string>();
					}

					var needsUpdate = false;
					foreach( var ace in Constants.Aces ) {
						var hasPerm = _perms.Values.Any( v => v.Contains( ace ) );
						var hasAce = API.IsPlayerAceAllowed( player.Handle, ace );
						if( hasPerm && !hasAce ) {
							_perms[id].Remove( ace );
						} else if( !hasPerm && hasAce ) {
							_perms[id].Add( ace );
						}

						needsUpdate = needsUpdate || hasPerm ^ hasAce;
					}

					if( needsUpdate ) {
						player.TriggerEvent( "HyperAdmin.Permissions", JsonConvert.SerializeObject( _perms[id] ) );
					}

				}

				foreach( var p in new List<int>( _perms.Keys ) ) {
					if( list.Contains( p ) ) continue;
					_perms.Remove( p );
				}

				await BaseScript.Delay( 2000 );
			} catch( Exception ex ) {
				Log.Error( ex );
			}
		}
	}
}
