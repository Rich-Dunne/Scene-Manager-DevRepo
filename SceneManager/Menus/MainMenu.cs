using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
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

        public static void BuildMainMenu()
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
            mainMenu.OnItemSelect += MainMenu_OnItemSelected;
            mainMenu.OnMenuOpen += MainMenu_OnMouseDown;
        }

        private static void MainMenu_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == navigateToBarrierMenu)
            {
                BarrierMenu.CreateShadowBarrier(BarrierMenu.barrierMenu);
            }
        }

        private static void MainMenu_OnMouseDown(UIMenu menu)
        {
            GameFiber.StartNew(() =>
            {
                while (menu.Visible)
                {
                    if (Game.IsKeyDown(Keys.LButton))
                    {
                        menu.Visible = false;
                        OnMenuItemClicked();
                    }
                    GameFiber.Yield();
                }
            });

            void OnMenuItemClicked()
            {
                if (navigateToPathMenu.Selected)
                {
                    PathMainMenu.pathMainMenu.Visible = true;
                }
                else if (navigateToBarrierMenu.Selected)
                {
                    BarrierMenu.barrierMenu.Visible = true;
                    BarrierMenu.CreateShadowBarrier(BarrierMenu.barrierMenu);
                }
                else if (navigateToSettingsMenu.Selected)
                {
                    SettingsMenu.settingsMenu.Visible = true;
                }
            }
        }
    }
}
