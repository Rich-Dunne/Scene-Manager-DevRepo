using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Rage;

[assembly: Rage.Attributes.Plugin("Scene Manager [Test Build]", Author = "Rich", Description = "Manage your scenes with custom AI traffic pathing and cone placement.")]

namespace SceneManager
{
    public class EntryPoint
    {
        internal static class Settings
        {
            internal static Keys ToggleKey = Keys.T;
            internal static Keys ModifierKey = Keys.LShiftKey;
            internal static ControllerButtons ToggleButton = ControllerButtons.Y;
            internal static ControllerButtons ModifierButton = ControllerButtons.A;
            internal static bool EnableHints = true;
            //internal static string id = Verification.passThrough(Verification.GetID());
            //internal static string PatronKey = null; // This cannot reference VerifyUser because the file can just be shared and it will always work.  Must be manually set to each user's ID

            internal static void LoadSettings()
            {
                Game.LogTrivial("Loading SceneManager.ini settings");
                InitializationFile ini = new InitializationFile("Plugins/SceneManager.ini");
                ini.Create();
                //PatronKey = ini.ReadString("Patreon","PatronKey", null);
                ToggleKey = ini.ReadEnum("Keybindings", "ToggleKey", Keys.T);
                ModifierKey = ini.ReadEnum("Keybindings", "ModifierKey", Keys.LShiftKey);
                ToggleButton = ini.ReadEnum("Keybindings", "ToggleButton", ControllerButtons.A);
                ModifierButton = ini.ReadEnum("Keybindings", "ModifierButton", ControllerButtons.DPadDown);
                EnableHints = ini.ReadBoolean("Other Settings", "EnableHints", true);
            }
        }

        public static void Main()
        {
            AppDomain.CurrentDomain.DomainUnload += MyTerminationHandler;
            Settings.LoadSettings();
            GetAssemblyVersion();
            MenuManager.InstantiateMenus();

            if (Settings.EnableHints)
                DisplayHintsToOpenMenu();

            GameFiber UserInputFiber = new GameFiber(() => GetUserInput.LoopForUserInput());
            UserInputFiber.Start();
        }

        private static void GetAssemblyVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
            var version = fvi.FileVersion;
            Game.LogTrivial($"Scene Manager V{version} is ready.");
        }

        private static void DisplayHintsToOpenMenu()
        {
            if (Settings.ModifierKey == Keys.None && Settings.ModifierButton == ControllerButtons.None)
            {
                Game.DisplayNotification($"~o~Scene Manager\n~y~[Hint]~w~ To open the menu, press the ~b~{Settings.ToggleKey} key ~w~or ~b~{Settings.ToggleButton} button ~w~while on foot");
            }
            else if (Settings.ModifierKey == Keys.None)
            {
                Game.DisplayNotification($"~o~Scene Manager\n~y~[Hint]~w~ To open the menu, press the ~b~{Settings.ToggleKey} key ~w~or ~b~{Settings.ModifierButton} ~w~+ ~b~{Settings.ToggleButton} buttons ~w~while on foot");
            }
            else if (Settings.ModifierButton == ControllerButtons.None)
            {
                Game.DisplayNotification($"~o~Scene Manager\n~y~[Hint]~w~ To open the menu, press ~b~{Settings.ModifierKey} ~w~+ ~b~{Settings.ToggleKey} ~w~or the ~b~{Settings.ToggleButton} button ~w~while on foot");
            }
            else
            {
                Game.DisplayNotification($"~o~Scene Manager\n~y~[Hint]~w~ To open the menu, press the ~b~{Settings.ModifierKey} ~w~+ ~b~{Settings.ToggleKey} keys ~w~or ~b~{Settings.ModifierButton} ~w~+ ~b~{Settings.ToggleButton} buttons ~w~while on foot");
            }
        }

        private static void MyTerminationHandler(object sender, EventArgs e)
        {
            // Clean up paths
            for (int i = 0; i < PathMainMenu.GetPaths().Count; i++)
            {
                PathMainMenu.DeletePath(PathMainMenu.GetPaths()[i], i, PathMainMenu.Delete.All);
            }

            // Clean up cones
            foreach (Rage.Object cone in BarrierMenu.barriers.Where(c => c))
            {
                cone.Delete();
            }
            if (BarrierMenu.shadowBarrier)
            {
                BarrierMenu.shadowBarrier.Delete();
            }

            // Clear everything
            BarrierMenu.barriers.Clear();
            TrafficPathing.collectedVehicles.Clear();

            Game.LogTrivial($"Scene Manager has been terminated.");
            Game.DisplayNotification($"~o~Scene Manager\n~r~[Notice]~w~ The plugin has shut down.");
        }
    }
}

//GameFiber.StartNew(delegate{
 
//});
  
//public static Vehicle[] GetNearbyVehicles2(Vector3 OriginPosition, int amount)
//{
//    return (Vehicle[])(from x in World.GetAllVehicles() orderby x.DistanceTo(OriginPosition) select x).Take(amount).ToArray();
//}

//Type t = typeof(int);
//Game.LogTrivial($"Scene Manager V{Assembly.GetAssembly(t).GetName().Version} is ready.");*/

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
