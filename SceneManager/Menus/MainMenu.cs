using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    class MainMenu
    {
        private static UIMenuItem navigateToPathMenu, navigateToBarrierMenu, navigateToSettingsMenu;

        public static void BuildMainMenu()
        {
            MenuManager.mainMenu.AddItem(navigateToPathMenu = new UIMenuItem("~o~Path Menu"));
            MenuManager.mainMenu.BindMenuToItem(MenuManager.pathMenu, navigateToPathMenu);
            MenuManager.mainMenu.AddItem(navigateToBarrierMenu = new UIMenuItem("~o~Barrier Menu"));
            MenuManager.mainMenu.BindMenuToItem(MenuManager.barrierMenu, navigateToBarrierMenu);
            MenuManager.mainMenu.AddItem(navigateToSettingsMenu = new UIMenuItem("~o~Settings"));
            MenuManager.mainMenu.BindMenuToItem(MenuManager.settingsMenu, navigateToSettingsMenu);

            MenuManager.mainMenu.RefreshIndex();
            MenuManager.mainMenu.OnItemSelect += MainMenu_OnItemSelected;
        }

        private static void MainMenu_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == navigateToBarrierMenu)
            {
                BarrierMenu.CreateShadowBarrier(MenuManager.barrierMenu);
            }
        }
    }
}
