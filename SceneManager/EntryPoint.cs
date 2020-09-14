using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Rage;

[assembly: Rage.Attributes.Plugin("Scene Manager [Release Candidate]", Author = "Rich", Description = "Manage your scenes with custom AI traffic pathing and cone placement.")]

namespace SceneManager
{
    public class EntryPoint
    {
        public static void Main()
        {
            AppDomain.CurrentDomain.DomainUnload += MyTerminationHandler;
            Settings.LoadSettings();
            GetAssemblyVersion();
            MenuManager.InstantiateMenus();

            DisplayHintsToOpenMenu();

            GameFiber UserInputFiber = new GameFiber(() => GetUserInput.LoopForUserInput());
            UserInputFiber.Start();
        }

        private static void GetAssemblyVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            var version = fvi.FileVersion;
            Logger.Log($"Scene Manager V{version} is ready.");
        }

        private static void DisplayHintsToOpenMenu()
        {
            if (Settings.ModifierKey == Keys.None && Settings.ModifierButton == ControllerButtons.None)
            {
                Hints.Display($"~o~Scene Manager\n~y~[Hint]~w~ To open the menu, press the ~b~{Settings.ToggleKey} key ~w~or ~b~{Settings.ToggleButton} button");
            }
            else if (Settings.ModifierKey == Keys.None)
            {
                Hints.Display($"~o~Scene Manager\n~y~[Hint]~w~ To open the menu, press the ~b~{Settings.ToggleKey} key ~w~or ~b~{Settings.ModifierButton} ~w~+ ~b~{Settings.ToggleButton} buttons");
            }
            else if (Settings.ModifierButton == ControllerButtons.None)
            {
                Hints.Display($"~o~Scene Manager\n~y~[Hint]~w~ To open the menu, press ~b~{Settings.ModifierKey} ~w~+ ~b~{Settings.ToggleKey} ~w~or the ~b~{Settings.ToggleButton} button");
            }
            else
            {
                Hints.Display($"~o~Scene Manager\n~y~[Hint]~w~ To open the menu, press the ~b~{Settings.ModifierKey} ~w~+ ~b~{Settings.ToggleKey} keys ~w~or ~b~{Settings.ModifierButton} ~w~+ ~b~{Settings.ToggleButton} buttons");
            }
        }

        private static void MyTerminationHandler(object sender, EventArgs e)
        {
            // Clean up paths
            for (int i = 0; i < PathMainMenu.GetPaths().Count; i++)
            {
                PathMainMenu.DeletePath(PathMainMenu.GetPaths()[i], PathMainMenu.Delete.All);
            }

            // Clean up cones
            foreach (Barrier barrier in BarrierMenu.barriers.Where(b => b.Object))
            {
                barrier.Object.Delete();
            }
            if (BarrierMenu.shadowBarrier)
            {
                BarrierMenu.shadowBarrier.Delete();
            }

            // Clear everything
            BarrierMenu.barriers.Clear();
            VehicleCollector.collectedVehicles.Clear();
            PathMainMenu.GetPaths().Clear();

            Logger.Log($"Plugin has shut down.");
            Game.DisplayNotification($"~o~Scene Manager\n~r~[Notice]~w~ The plugin has shut down.");
        }
    }
}

//// id is hardware ID and needs to match PatronKey, which is also hardware ID
//if (Settings.id == Settings.PatronKey)
//{
//    Game.LogTrivial($"Patron status verified.");
//    Game.DisplayNotification($"~o~Scene Manager\n~g~[Patreon]~w~ Thanks for the support, enjoy your session!");
//}
//else
//{
//    Game.LogTrivial($"Patron status not verified.");
//    Game.DisplayNotification($"~o~Scene Manager\n~y~[Patreon]~w~ Thanks for using my plugin!  If you would like to gain access to benefits such as ~g~new features for this plugin~w~, ~g~early access to new plugins~w~, and ~g~custom plugins made just for you~w~, please consider supporting me on ~b~Patreon~w~. ~y~https://www.patreon.com/richdevs");
//}
