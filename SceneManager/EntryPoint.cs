using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Rage;

[assembly: Rage.Attributes.Plugin("Scene Manager", Author = "Rich", Description = "Control your scenes with custom AI pathing and traffic barrier management.")]

namespace SceneManager
{
    public class EntryPoint
    {
        internal static void Main()
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
            for (int i = 0; i < PathMainMenu.paths.Count; i++)
            {
                PathMainMenu.DeletePath(PathMainMenu.paths[i], PathMainMenu.Delete.All);
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
            PathMainMenu.paths.Clear();

            Logger.Log($"Plugin has shut down.");
            Game.DisplayNotification($"~o~Scene Manager\n~r~[Notice]~w~ The plugin has shut down.");
        }
    }
}
