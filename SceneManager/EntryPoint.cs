using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Rage;
using SceneManager.Objects;
using SceneManager.Utils;

[assembly: Rage.Attributes.Plugin("Scene Manager", Author = "Rich", Description = "Control your scenes with custom AI pathing and traffic barrier management.", PrefersSingleInstance = true)]

namespace SceneManager
{
    [Obfuscation(Exclude = false, Feature = "-rename", ApplyToMembers = false)]
    public class EntryPoint
    {
        [Obfuscation(Exclude = false, Feature = "-rename")]
        internal static void Main()
        {
            if(!InputManagerChecker() || !CheckRNUIVersion())
            {
                Game.UnloadActivePlugin();
                return;
            }

            while (Game.IsLoading)
            {
                GameFiber.Yield();
            }

            AppDomain.CurrentDomain.DomainUnload += MyTerminationHandler;
            Settings.LoadSettings();
            GetAssemblyVersion();
            MenuManager.InstantiateMenus();

            DisplayHintsToOpenMenu();

            GameFiber UserInputFiber = new GameFiber(() => GetUserInput.LoopForUserInput());
            UserInputFiber.Start();

            void GetAssemblyVersion()
            {
                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                Game.LogTrivial($"Scene Manager V{version} is ready.");
            }
        }

        private static bool CheckRNUIVersion()
        {
            var directory = Directory.GetCurrentDirectory();
            var exists = File.Exists(directory + @"\RAGENativeUI.dll");
            if (!exists)
            {
                Game.LogTrivial($"RNUI was not found in the user's GTA V directory.");
                Game.DisplayNotification($"~o~Scene Manager ~r~[Error]\n~w~RAGENativeUI.dll was not found in your GTA V directory.  Please install RAGENativeUI and try again.");
                return false;
            }
 
            var userVersion = Assembly.LoadFrom(directory + @"\RAGENativeUI.dll").GetName().Version;
            Version requiredMinimumVersion = new Version("1.7.0.0");
            if(userVersion >= requiredMinimumVersion)
            {
                Game.LogTrivial($"User's RNUI version: {userVersion}");
                return true;
            }
            else
            {
                Game.DisplayNotification($"~o~Scene Manager~r~[Error]\n~w~Your RAGENativeUI.dll version is below 1.7.  Please update RAGENativeUI and try again.");
                return false;
            }
            
        }

        private static bool InputManagerChecker()
        {
            var directory = Directory.GetCurrentDirectory();
            var exists = File.Exists(directory + @"\InputManager.dll");
            if (!exists)
            {
                Game.LogTrivial($"InputManager was not found in the user's GTA V directory.");
                Game.DisplayNotification($"~o~Scene Manager ~r~[Error]\n~w~InputManager.dll was not found in your GTA V directory.  Please install InputManager.dll and try again.");
                return false;
            }
            return true;
        }

        private static void DisplayHintsToOpenMenu()
        {
            if (Settings.ModifierKey == Keys.None && Settings.ModifierButton == ControllerButtons.None)
            {
                Hints.Display($"~o~Scene Manager ~y~[Hint]\n~w~To open the menu, press the ~b~{Settings.ToggleKey} key ~w~or ~b~{Settings.ToggleButton} button");
            }
            else if (Settings.ModifierKey == Keys.None)
            {
                Hints.Display($"~o~Scene Manager ~y~[Hint]\n~w~To open the menu, press the ~b~{Settings.ToggleKey} key ~w~or ~b~{Settings.ModifierButton} ~w~+ ~b~{Settings.ToggleButton} buttons");
            }
            else if (Settings.ModifierButton == ControllerButtons.None)
            {
                Hints.Display($"~o~Scene Manager ~y~[Hint]\n~w~To open the menu, press ~b~{Settings.ModifierKey} ~w~+ ~b~{Settings.ToggleKey} ~w~or the ~b~{Settings.ToggleButton} button");
            }
            else
            {
                Hints.Display($"~o~Scene Manager ~y~[Hint]\n~w~To open the menu, press the ~b~{Settings.ModifierKey} ~w~+ ~b~{Settings.ToggleKey} keys ~w~or ~b~{Settings.ModifierButton} ~w~+ ~b~{Settings.ToggleButton} buttons");
            }
        }
        private static void MyTerminationHandler(object sender, EventArgs e)
        {
            // Clean up cones
            foreach (Barrier barrier in BarrierMenu.barriers.Where(b => b.Object))
            {
                barrier.Object.Delete();
            }
            if (BarrierMenu.shadowBarrier)
            {
                BarrierMenu.shadowBarrier.Delete();
            }

            // Clean up paths
            for (int i = 0; i < PathMainMenu.paths.Count; i++)
            {
                PathMainMenu.DeletePath(PathMainMenu.paths[i], Delete.All);
            }

            Game.LogTrivial($"Plugin has shut down.");
            Game.DisplayNotification($"~o~Scene Manager ~r~[Terminated]\n~w~The plugin has shut down.");
        }
    }
}
