using System;

namespace HyperAdmin.Client.Helper
{
	public abstract class CachedValue<T>
	{

		public float Expiration { get; }
		protected DateTime LastUpdate = DateTime.MinValue;
		protected T CachedVal;

		public T Value
		{
			get {
				if( !((DateTime.UtcNow - LastUpdate).TotalMilliseconds > Expiration) ) return CachedVal;

				CachedVal = Update();
				LastUpdate = DateTime.UtcNow;
				return CachedVal;
			}
		}

		protected CachedValue( float expirationMs ) {
			Expiration = expirationMs;
		}

		protected abstract T Update();
	}
}
