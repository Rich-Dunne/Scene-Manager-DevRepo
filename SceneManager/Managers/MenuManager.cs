using RAGENativeUI;
using SceneManager.Menus;
using Rage;
using System.Linq;
using RAGENativeUI.Elements;
using System.Drawing;

namespace SceneManager.Managers
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
            DriverMenu.Initialize();
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

        internal static void BuildMenus()
        {
            MainMenu.BuildMainMenu();
            SettingsMenu.BuildSettingsMenu();
            DriverMenu.Build();
            PathMainMenu.Build();
            PathCreationMenu.BuildPathCreationMenu();
            ImportPathMenu.BuildImportMenu();
            EditPathMenu.BuildEditPathMenu();
            BarrierMenu.BuildMenu();
        }

        internal static void ColorMenuItems()
        {
            foreach(UIMenuItem menuItem in MenuPool.SelectMany(x => x.MenuItems))
            {
                if (menuItem.Enabled)
                {
                    menuItem.HighlightedBackColor = menuItem.ForeColor;
                }
                if(!menuItem.Enabled)
                {
                    menuItem.HighlightedBackColor = Color.DarkGray;
                    menuItem.DisabledForeColor = Color.Gray;
                }
            }
        }

        internal static void ProcessMenus()
        {
            while (MenuPool.Any(x => x.Visible))
            {
                MenuPool.ProcessMenus();
                ColorMenuItems();
                GameFiber.Yield();
            }
        }
    }
}
