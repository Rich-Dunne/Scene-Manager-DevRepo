using Rage;

namespace SceneManager
{
    class GetUserInput
    {
        public static void LoopForUserInput()
        {
            while (true)
            {
                // Keyboard
                GetKeyboardInput();

                // Controller
                GetControllerInput();

                // Display this message for test versions only
                if (MainMenu.mainMenu.Visible)
                {
                    Game.DisplaySubtitle($"You are using a test build of Scene Manager.  Please report any bugs/crashes in the Discord server.");
                }

                MenuManager.menuPool.ProcessMenus();
                GameFiber.Yield();
            }
        }
        
        private static void GetControllerInput()
        {
            if (EntryPoint.Settings.ModifierButton == ControllerButtons.None)
            {
                if (Game.IsControllerButtonDown(EntryPoint.Settings.ToggleButton) && AreMenusClosed())
                {
                    MainMenu.mainMenu.Visible = !MainMenu.mainMenu.Visible;
                }
            }
            else if (Game.IsControllerButtonDownRightNow(EntryPoint.Settings.ModifierButton) && Game.IsControllerButtonDown(EntryPoint.Settings.ToggleButton) && AreMenusClosed())
            {
                MainMenu.mainMenu.Visible = !MainMenu.mainMenu.Visible;
            }
        }

        private static void GetKeyboardInput()
        {
            if (EntryPoint.Settings.ModifierKey == System.Windows.Forms.Keys.None)
            {
                if (Game.IsKeyDown(EntryPoint.Settings.ToggleKey) && AreMenusClosed())
                {
                    MainMenu.mainMenu.Visible = !MainMenu.mainMenu.Visible;
                }
            }
            else if (Game.IsKeyDownRightNow(EntryPoint.Settings.ModifierKey) && Game.IsKeyDown(EntryPoint.Settings.ToggleKey) && AreMenusClosed())
            {
                MainMenu.mainMenu.Visible = !MainMenu.mainMenu.Visible;
            }
        }

        private static bool AreMenusClosed()
        {
            if(!BarrierMenu.barrierMenu.Visible && !PathMainMenu.pathMainMenu.Visible && !PathCreationMenu.pathCreationMenu.Visible && !EditPathMenu.editPathMenu.Visible && !EditWaypointMenu.editWaypointMenu.Visible && !SettingsMenu.settingsMenu.Visible)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
