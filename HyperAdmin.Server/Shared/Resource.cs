using System;
using CitizenFX.Core.Native;

namespace HyperAdmin.Shared
{
	public class Resource
	{

		public string Name { get; set; }

		public string Status => API.GetResourceState( Name );
		public bool Exists => !Status.Equals( "missing", StringComparison.InvariantCultureIgnoreCase );

		public Resource() {

		}

		public Resource( string name ) {
			Name = name;
		}

#if SERVER
		public void Start() {
			API.StartResource( Name );
		}

		public void Stop() {
			API.StopResource( Name );
		}

		public void Restart() {
			Stop();
			Start();
		}
#endif
	}
}
