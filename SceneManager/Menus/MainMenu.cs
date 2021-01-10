using RAGENativeUI;
using RAGENativeUI.Elements;
using System.Collections.Generic;
using System.Drawing;
using SceneManager.Utils;
using Rage;

namespace SceneManager.Menus
{
    // The only reason this class should change is to modify the main menu
    class MainMenu
    {
        internal static UIMenu Menu { get; } = new UIMenu("Scene Manager", "~o~Main Menu");

        internal static void Initialize()
        {
            MenuManager.AddToMenuPool(Menu);

            Menu.OnMenuOpen += MainMenu_OnMenuOpen;
        }

        internal static void BuildMainMenu()
        {
            var navigateToPathMenu = new UIMenuItem("Path Menu");
            Menu.AddItem(navigateToPathMenu);
            navigateToPathMenu.ForeColor = Color.Gold;
            Menu.BindMenuToItem(PathMainMenu.Menu, navigateToPathMenu);

            var navigateToBarrierMenu = new UIMenuItem("Barrier Menu");
            Menu.AddItem(navigateToBarrierMenu);
            navigateToBarrierMenu.ForeColor = Color.Gold;
            Menu.BindMenuToItem(BarrierMenu.Menu, navigateToBarrierMenu);

            var navigateToSettingsMenu = new UIMenuItem("Settings Menu");
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
