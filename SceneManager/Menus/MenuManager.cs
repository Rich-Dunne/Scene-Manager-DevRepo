using RAGENativeUI;
using SceneManager.Menus;
using Rage;
using System.Linq;
using RAGENativeUI.Elements;
using System.Drawing;

namespace SceneManager
{
    // The only reason this class should change is to modify how menus are are being handled
    internal static class MenuManager
    {
        internal static MenuPool MenuPool { get; } = new MenuPool();

        internal static void InitializeMenus()
        {
            MainMenu.Initialize();
            SettingsMenu.Initialize();
            PathMainMenu.Initialize();
            PathCreationMenu.Initialize();
            ImportPathMenu.Initialize();
            BarrierMenu.Initialize();
            EditPathMenu.Initialize();
            EditWaypointMenu.Initialize();

            BuildMenus();
            ColorMenuItems();
            DefineMenuMouseSettings();
        }

        private static void DefineMenuMouseSettings()
        {
            foreach (UIMenu menu in MenuPool)
            {
                menu.MouseControlsEnabled = false;
                menu.AllowCameraMovement = true;
            }
        }

        private static void BuildMenus()
        {
            MainMenu.BuildMainMenu();
            SettingsMenu.BuildSettingsMenu();
            PathMainMenu.BuildPathMenu();
            ImportPathMenu.BuildImportMenu();
            EditPathMenu.BuildEditPathMenu();
            BarrierMenu.BuildBarrierMenu();
        }

        private static void ColorMenuItems()
        {
            foreach(UIMenuItem menuItem in MenuPool.SelectMany(x => x.MenuItems))
            {
                if (menuItem.Enabled && menuItem.ForeColor == Color.Gold)
                {
                    menuItem.HighlightedBackColor = menuItem.ForeColor;
                }
            }

        }

        internal static bool AreMenusClosed()
        {
            if (!BarrierMenu.Menu.Visible && !PathMainMenu.Menu.Visible && !PathCreationMenu.Menu.Visible && !EditPathMenu.Menu.Visible && !EditWaypointMenu.Menu.Visible && !SettingsMenu.Menu.Visible)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static void Update()
        {
            while (AnyMenuVisible())
            {
                MenuPool.ProcessMenus();
                GameFiber.Yield();
            }
        }

        private static bool AnyMenuVisible()
        {
            if(MenuPool.Any(x => x.Visible))
            {
                return true;
            }

            return false;
        }

        internal static void AddToMenuPool(UIMenu menu)
        {
            MenuPool.Add(menu);
        }
    }
}
