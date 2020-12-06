using RAGENativeUI;
using RAGENativeUI.Elements;
using System.Collections.Generic;
using System.Drawing;

namespace SceneManager
{
    internal static class MenuManager
    {
        internal static MenuPool menuPool = new MenuPool();
        internal static Dictionary<UIMenu, List<UIMenuItem>> menus = new Dictionary<UIMenu, List<UIMenuItem>>();

        internal static void InstantiateMenus()
        {
            MainMenu.InstantiateMenu();
            SettingsMenu.InstantiateMenu();
            PathMainMenu.InstantiateMenu();
            PathCreationMenu.InstantiateMenu();
            BarrierMenu.InstantiateMenu();
            EditPathMenu.InstantiateMenu();
            EditWaypointMenu.InstantiateMenu();

            BuildMenus();
            DefineMenuMouseSettings();
        }

        private static void DefineMenuMouseSettings()
        {
            foreach (UIMenu menu in menuPool)
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
            EditPathMenu.BuildEditPathMenu();
            BarrierMenu.BuildBarrierMenu();
        }
    }
}
