using RAGENativeUI;
using RAGENativeUI.Elements;
using System.Collections.Generic;
using System.Drawing;
using SceneManager.Utils;

namespace SceneManager
{
    class MainMenu
    {
        internal static UIMenu mainMenu { get; private set; }
        private static UIMenuItem navigateToPathMenu, navigateToBarrierMenu, navigateToSettingsMenu;

        internal static void InstantiateMenu()
        {
            mainMenu = new UIMenu("Scene Manager", "~o~Main Menu");
            MenuManager.menuPool.Add(mainMenu);
        }

        internal static void BuildMainMenu()
        {
            mainMenu.AddItem(navigateToPathMenu = new UIMenuItem("Path Menu"));
            navigateToPathMenu.ForeColor = Color.Gold;
            mainMenu.BindMenuToItem(PathMainMenu.pathMainMenu, navigateToPathMenu);

            mainMenu.AddItem(navigateToBarrierMenu = new UIMenuItem("Barrier Menu"));
            navigateToBarrierMenu.ForeColor = Color.Gold;
            mainMenu.BindMenuToItem(BarrierMenu.barrierMenu, navigateToBarrierMenu);

            mainMenu.AddItem(navigateToSettingsMenu = new UIMenuItem("Settings"));
            navigateToSettingsMenu.ForeColor = Color.Gold;
            mainMenu.BindMenuToItem(SettingsMenu.settingsMenu, navigateToSettingsMenu);

            mainMenu.RefreshIndex();
            mainMenu.OnMenuOpen += MainMenu_OnMenuOpen;
        }

        private static void MainMenu_OnMenuOpen(UIMenu menu)
        {
            var scrollerItems = new List<UIMenuScrollerItem> { };
            RNUIMouseInputHandler.Initialize(menu, scrollerItems);
        }
    }
}
