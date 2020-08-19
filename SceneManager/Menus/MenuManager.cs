using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    public static class MenuManager
    {
        public static MenuPool menuPool = new MenuPool();
        public static UIMenu mainMenu, pathMenu, barrierMenu, pathCreationMenu, editPathMenu, editWaypointMenu, settingsMenu;

        public static void InstantiateMenus()
        {
            mainMenu = new UIMenu("Scene Manager", "");
            settingsMenu = new UIMenu("Scene Menu", "~o~Plugin Settings");
            settingsMenu.ParentMenu = mainMenu;
            pathMenu = new UIMenu("Scene Manager", "~o~Path Menu");
            pathMenu.ParentMenu = mainMenu;
            pathCreationMenu = new UIMenu("Scene Manager", "~o~Path Creation");
            pathCreationMenu.ParentMenu = pathMenu;
            barrierMenu = new UIMenu("Scene Manager", "~o~Barrier Management");
            barrierMenu.ParentMenu = mainMenu;
            editPathMenu = new UIMenu("Scene Manager", "~o~Edit Path");
            editPathMenu.ParentMenu = pathMenu;
            editWaypointMenu = new UIMenu("Scene Manager", "~o~Edit Waypoint");
            editWaypointMenu.ParentMenu = editPathMenu;

            AddMenusToMenuPool();
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
            TrafficMenu.BuildPathMenu();
            PathCreationMenu.BuildPathCreationMenu();
            EditPathMenu.BuildEditPathMenu();
            BarrierMenu.BuildBarrierMenu();
        }

        private static void AddMenusToMenuPool()
        {
            menuPool.Add(mainMenu);
            menuPool.Add(settingsMenu);
            menuPool.Add(pathMenu);
            menuPool.Add(barrierMenu);
            menuPool.Add(pathCreationMenu);
            menuPool.Add(editPathMenu);
            menuPool.Add(editWaypointMenu);
        }
    }
}
