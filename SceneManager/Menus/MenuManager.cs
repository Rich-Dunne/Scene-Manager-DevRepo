using RAGENativeUI;

namespace SceneManager
{
    public static class MenuManager
    {
        public static MenuPool menuPool = new MenuPool();

        public static void InstantiateMenus()
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

        private static void AddMenusToMenuPool()
        {
            menuPool.Add(MainMenu.mainMenu);
            menuPool.Add(SettingsMenu.settingsMenu);
            menuPool.Add(PathMainMenu.pathMainMenu);
            menuPool.Add(BarrierMenu.barrierMenu);
            menuPool.Add(PathCreationMenu.pathCreationMenu);
            menuPool.Add(EditPathMenu.editPathMenu);
            menuPool.Add(EditWaypointMenu.editWaypointMenu);
        }
    }
}
