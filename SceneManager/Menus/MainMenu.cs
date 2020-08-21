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
        public static UIMenu mainMenu { get; private set; }
        private static UIMenuItem navigateToPathMenu, navigateToBarrierMenu, navigateToSettingsMenu;

        internal static void InstantiateMenu()
        {
            mainMenu = new UIMenu("Scene Manager", "");
            MenuManager.menuPool.Add(mainMenu);
        }

        public static void BuildMainMenu()
        {
            mainMenu.AddItem(navigateToPathMenu = new UIMenuItem("~o~Path Menu"));
            mainMenu.BindMenuToItem(PathMainMenu.pathMainMenu, navigateToPathMenu);
            mainMenu.AddItem(navigateToBarrierMenu = new UIMenuItem("~o~Barrier Menu"));
            mainMenu.BindMenuToItem(BarrierMenu.barrierMenu, navigateToBarrierMenu);
            mainMenu.AddItem(navigateToSettingsMenu = new UIMenuItem("~o~Settings"));
            mainMenu.BindMenuToItem(SettingsMenu.settingsMenu, navigateToSettingsMenu);

            mainMenu.RefreshIndex();
            mainMenu.OnItemSelect += MainMenu_OnItemSelected;
        }

        private static void MainMenu_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == navigateToBarrierMenu)
            {
                BarrierMenu.CreateShadowBarrier(BarrierMenu.barrierMenu);
            }
        }
    }
}
