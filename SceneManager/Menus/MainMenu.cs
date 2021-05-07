using RAGENativeUI;
using RAGENativeUI.Elements;
using System.Collections.Generic;
using System.Drawing;
using SceneManager.Utils;
using Rage;
using SceneManager.Managers;
using System.Linq;

namespace SceneManager.Menus
{
    // The only reason this class should change is to modify the main menu
    class MainMenu
    {
        internal static UIMenu Menu { get; } = new UIMenu("Scene Manager", "~o~Main Menu");

        internal static void Initialize()
        {
            MenuManager.MenuPool.Add(Menu);

            Menu.OnMenuOpen += MainMenu_OnMenuOpen;
        }

        internal static void BuildMainMenu()
        {
            Menu.Clear();

            var navigateToPathMenu = new UIMenuItem("Manage Paths");
            Menu.AddItem(navigateToPathMenu);
            navigateToPathMenu.ForeColor = Color.Gold;
            Menu.BindMenuToItem(PathMainMenu.Menu, navigateToPathMenu);

            var navigateToDriverMenu = new UIMenuItem("Manage Drivers", "After you create a path, you will be able to direct drivers using this menu.");
            Menu.AddItem(navigateToDriverMenu);
            navigateToDriverMenu.ForeColor = Color.Gold;
            Menu.BindMenuToItem(DriverMenu.Menu, navigateToDriverMenu);
            navigateToDriverMenu.Enabled = PathManager.Paths.Count() > 0;

            var navigateToBarrierMenu = new UIMenuItem("Manage Barriers");
            Menu.AddItem(navigateToBarrierMenu);
            navigateToBarrierMenu.ForeColor = Color.Gold;
            Menu.BindMenuToItem(BarrierMenu.Menu, navigateToBarrierMenu);

            var navigateToSettingsMenu = new UIMenuItem("Settings");
            Menu.AddItem(navigateToSettingsMenu);
            navigateToSettingsMenu.ForeColor = Color.Gold;
            Menu.BindMenuToItem(SettingsMenu.Menu, navigateToSettingsMenu);

            Menu.RefreshIndex();
        }

        private static void MainMenu_OnMenuOpen(UIMenu menu)
        {
            var scrollerItems = new List<UIMenuScrollerItem> { };
            GameFiber.StartNew(() => UserInput.InitializeMenuMouseControl(menu, scrollerItems), "RNUI Mouse Input Fiber");
        }

        internal static void DisplayMenu()
        {
            Menu.Visible = !Menu.Visible;
        }
    }
}
