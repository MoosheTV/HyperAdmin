using System.Linq;
using System.Threading.Tasks;

namespace HyperAdmin.Client.Menus
{
	public class MenuItemSubMenu : MenuItem
	{
		public Menu Child { get; }

		public MenuItemSubMenu( Client client, Menu owner, Menu child, string label = "", string ace="", int priority = -1 ) : base( client, owner, label, ace, priority ) {
			Label = label.Any() ? label : child.Title;
			Child = child;
		}

		protected override Task OnActivate() {
			if( Child != null ) {
				Client.Menu.CurrentMenu = Child;
			}
			return Task.FromResult( 0 );
		}
	}
}
