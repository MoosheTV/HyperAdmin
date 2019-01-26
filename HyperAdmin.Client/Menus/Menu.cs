using System;
using System.Collections.Generic;
using System.Linq;

namespace HyperAdmin.Client.Menus
{
	public class Menu : List<MenuItem>
	{
		private static readonly List<MenuItem> DefaultList = new List<MenuItem> {
			new MenuItem(null, null, "Populating Menu...")
		};

		public virtual string Title { get; protected set; }
		public Menu Parent { get; }

		private int _index;
		public int CurrentIndex
		{
			get => Math.Max( 0, Math.Min( _index, VisibleCount - 1 ) );
			set {
				if( value < 0 ) {
					value = VisibleCount - 1;
				}
				_index = VisibleCount < 1 ? 0 : Math.Max( 0, value % VisibleCount );

				if( VisibleCount > 0 ) {
					var item = GetVisibleItems().ElementAt( _index );
					item.Select.Invoke();
				}
			}
		}

		public int VisibleCount => GetVisibleItems().Count();

		public MenuItem CurrentItem => GetVisibleItems().ElementAt( CurrentIndex );

		public virtual bool Enabled { get; set; } = true;

		public Menu( string title, Menu parent = null ) {
			Title = title;
			Parent = parent;
		}

		public virtual void OnEnter() {

		}

		public virtual void OnExit() {
		}

		public new bool Remove( MenuItem i ) {
			var remove = base.Remove( i );
			Refresh();
			return remove;
		}

		public new void Add( MenuItem item ) {
			base.Add( item );
			Refresh();
		}

		public new void AddRange( IEnumerable<MenuItem> items ) {
			base.AddRange( items );
			Refresh();
		}

		private void Refresh() {
			Sort( ( a, b ) => b.Priority - a.Priority );
			CurrentIndex = _index;
		}

		public IEnumerable<MenuItem> GetVisibleItems() {
			var list = this.Where( i => i.IsVisible ).ToList();
			return list.Any() ? list : DefaultList;
		}
	}
}
