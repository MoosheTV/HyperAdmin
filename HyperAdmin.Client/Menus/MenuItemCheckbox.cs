using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperAdmin.Client.Menus
{
	public class MenuItemCheckbox : MenuItem
	{
		private bool _isChecked;
		internal Func<bool> IsChecked;

		public MenuItemCheckbox( Client client, Menus.Menu owner, string label, bool isChecked = false, string ace="", int priority = -1 ) : base( client, owner, label, ace, priority ) {
			_isChecked = isChecked;
			IsChecked = () => _isChecked;
		}

		protected override Task OnActivate() {
			_isChecked = !_isChecked;
			return base.OnActivate();
		}
	}
}
