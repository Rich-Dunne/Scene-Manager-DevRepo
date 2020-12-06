using Rage;

namespace SceneManager.Utils
{
    class GetUserInput
    {
        internal static void LoopForUserInput()
        {
            while (true)
            {
                bool isTextEntryOpen = (Rage.Native.NativeFunction.Natives.UPDATE_ONSCREEN_KEYBOARD<int>() == 0);
                if (!isTextEntryOpen)
                {
                    GetKeyboardInput();
                    GetControllerInput();
                }
                else
                {
                    Game.LogTrivial($"A text menu is open.");
                }

#if DEBUG
                if (MenuManager.menuPool.IsAnyMenuOpen())
                {
                    Game.DisplaySubtitle($"You are using a test build of Scene Manager.  Please report any bugs/crashes in the Discord server.");
                }
#endif
                MenuManager.menuPool.ProcessMenus();
                GameFiber.Yield();
            }
        }
        
        private static void GetControllerInput()
        {
            if (Settings.ModifierButton == ControllerButtons.None)
            {
                if (Game.IsControllerButtonDown(Settings.ToggleButton) && AreMenusClosed())
                {
                    MainMenu.mainMenu.Visible = !MainMenu.mainMenu.Visible;
                }
            }
            else if (Game.IsControllerButtonDownRightNow(Settings.ModifierButton) && Game.IsControllerButtonDown(Settings.ToggleButton) && AreMenusClosed())
            {
                MainMenu.mainMenu.Visible = !MainMenu.mainMenu.Visible;
            }
        }

        private static void GetKeyboardInput()
        {
            if (Settings.ModifierKey == System.Windows.Forms.Keys.None)
            {
                if (Game.IsKeyDown(Settings.ToggleKey) && AreMenusClosed())
                {
                    MainMenu.mainMenu.Visible = !MainMenu.mainMenu.Visible;
                }
            }
            else if (Game.IsKeyDownRightNow(Settings.ModifierKey) && Game.IsKeyDown(Settings.ToggleKey) && AreMenusClosed())
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
