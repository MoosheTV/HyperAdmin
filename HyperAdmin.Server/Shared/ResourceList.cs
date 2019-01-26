using System.Collections;
using System.Collections.Generic;
using CitizenFX.Core.Native;

namespace HyperAdmin.Shared
{
	internal class ResourceList : IEnumerable<Resource>
	{
		public IEnumerator<Resource> GetEnumerator() {
			return new ResourceEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}

	internal class ResourceEnumerator : IEnumerator<Resource>
	{
		private int _current;
		private readonly int _numResources;

		public ResourceEnumerator() {
			_numResources = API.GetNumResources();
		}

		public void Dispose() {
			Reset();
		}

		public bool MoveNext() {
			return ++_current < _numResources;
		}

		public void Reset() {
			_current = 0;
		}

		public Resource Current => new Resource( API.GetResourceByFindIndex( _current ) );

		object IEnumerator.Current => Current;
	}
}
