using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using HyperAdmin.Shared;

namespace HyperAdmin.Client.Util
{
	public static class MathExtents
	{

		public static float ProduceDot( this Vector3 source, Vector3 target, Vector3 forward ) {
			var vec = target - source;
			vec.Normalize();
			return Vector3.Dot( vec, forward );
		}


		public static Vector3 GameplayCameraForwardVec() {
			try {
				var rot = (float)(Math.PI / 180f) * API.GetGameplayCamRot( 2 );
				return Vector3.Normalize( new Vector3( (float)-Math.Sin( rot.Z ) * (float)Math.Abs( Math.Cos( rot.X ) ), (float)Math.Cos( rot.Z ) * (float)Math.Abs( Math.Cos( rot.X ) ), (float)Math.Sin( rot.X ) ) );
			}
			catch( Exception ex ) {
				Log.Error( $"{ex}" );
			}
			return default( Vector3 );
		}
	}
}
