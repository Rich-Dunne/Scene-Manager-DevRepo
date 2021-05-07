using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using SceneManager.CollectedPeds;
using SceneManager.Managers;
using SceneManager.Utils;
using SceneManager.Waypoints;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SceneManager.Menus
{
    internal class DriverMenu
    {
        internal static UIMenu Menu = new UIMenu("Scene Manager", "~o~Driver Menu");
        private static string[] DismissOptions { get; } = new string[] { "From path", "From waypoint", "From world" };
        internal static UIMenuListScrollerItem<string> DirectOptions { get; } = new UIMenuListScrollerItem<string>("Direct driver to path's", "", new[] { "First waypoint", "Nearest waypoint" });
        internal static UIMenuListScrollerItem<string> DirectDriver { get; private set; }
        internal static UIMenuListScrollerItem<string> DismissDriver { get; } = new UIMenuListScrollerItem<string>("Dismiss nearest driver", $"~b~From path: ~w~Driver will be released from the path\n~b~From waypoint: ~w~Driver will skip their current waypoint task\n~b~From world: ~w~Driver will be removed from the world.", DismissOptions);

        internal static void Initialize()
        {
            Menu.ParentMenu = PathMainMenu.Menu;
            MenuManager.MenuPool.Add(Menu);

            Menu.OnItemSelect += DriverMenu_OnItemSelected;
            Menu.OnMenuOpen += DriverMenu_OnMenuOpen;
        }

        internal static void Build()
        {
            Menu.Clear();

            Menu.AddItem(DirectOptions);
            DirectOptions.Enabled = true;
            Menu.AddItem(DirectDriver = new UIMenuListScrollerItem<string>("Direct nearest driver to path", "", PathManager.Paths.Select(x => x.Name))); // This must instantiate here because the Paths change
            DirectDriver.ForeColor = Color.Gold;
            DirectDriver.Enabled = true;
            Menu.AddItem(DismissDriver);
            DismissDriver.ForeColor = Color.Gold;

            if (PathManager.Paths.Count == 0)
            {
                DirectOptions.Enabled = false;
                DirectDriver.Enabled = false;
            }

            Menu.RefreshIndex();
        }

        private static void DriverMenu_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == DirectDriver)
            {
                if (Utils.DirectDriver.ValidateOptions(DirectOptions, PathManager.Paths[DirectDriver.Index], out Vehicle nearbyVehicle, out Waypoint targetWaypoint))
                {
                    Utils.DirectDriver.Direct(nearbyVehicle, PathManager.Paths[DirectDriver.Index], targetWaypoint);
                }
            }

            if (selectedItem == DismissDriver)
            {
                Utils.DismissDriver.Dismiss(DismissDriver.Index);
            }
        }

        private static void DriverMenu_OnMenuOpen(UIMenu menu)
        {
            var scrollerItems = new List<UIMenuScrollerItem> { DirectOptions, DirectDriver, DismissDriver };
            GameFiber.StartNew(() => UserInput.InitializeMenuMouseControl(menu, scrollerItems), "RNUI Mouse Input Fiber");
        }
    }
}
