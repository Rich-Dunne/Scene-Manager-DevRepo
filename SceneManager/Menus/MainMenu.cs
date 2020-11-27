using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SceneManager
{
    class MainMenu
    {
        public static UIMenu mainMenu { get; private set; }
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

        private static void ShowPathMainMenu()
        {
            PathMainMenu.pathMainMenu.Visible = true;
        }

        private static void ShowBarrierMenu()
        {
            BarrierMenu.barrierMenu.Visible = true;
        }

        private static void ShowSettingsMenu()
        {
            SettingsMenu.settingsMenu.Visible = true;
        }

        private static void MainMenu_OnMenuOpen(UIMenu menu)
        {
            var scrollerItems = new List<UIMenuScrollerItem> { };
            var checkboxItems = new Dictionary<UIMenuCheckboxItem, RNUIMouseInputHandler.Function>() { };
            var selectItems = new Dictionary<UIMenuItem, RNUIMouseInputHandler.Function>()
            {
                { navigateToPathMenu, ShowPathMainMenu },
                { navigateToBarrierMenu, ShowBarrierMenu },
                { navigateToSettingsMenu, ShowSettingsMenu }
            };

            RNUIMouseInputHandler.Initialize(menu, scrollerItems);
        }
    }
}
