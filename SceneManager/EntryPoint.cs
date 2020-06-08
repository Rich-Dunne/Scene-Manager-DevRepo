using System.Windows.Forms;
using Rage;

[assembly: Rage.Attributes.Plugin("Scene Manager V1.7", Author = "Rich", Description = "Manage your scenes with custom AI traffic pathing and cone placement.")]

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
            internal static string id = Verification.passThrough(Verification.GetID());
            internal static string PatronKey = null; // This cannot reference VerifyUser because the file can just be shared and it will always work.  Must be manually set to each user's ID

            internal static void LoadSettings()
            {
                Game.LogTrivial("Loading SceneManager.ini settings");
                InitializationFile ini = new InitializationFile("Plugins/SceneManager.ini");
                ini.Create();
                PatronKey = ini.ReadString("Patreon","PatronKey", null);
                ToggleKey = ini.ReadEnum("Keybindings", "ToggleKey", Keys.T);
                ModifierKey = ini.ReadEnum("Keybindings", "ModifierKey", Keys.LShiftKey);
                ToggleButton = ini.ReadEnum("Keybindings", "ToggleButton", ControllerButtons.A);
                ModifierButton = ini.ReadEnum("Keybindings", "ModifierButton", ControllerButtons.DPadDown);
                EnableHints = ini.ReadBoolean("Other Settings", "EnableHints", true);
            }
        }

        public static void Main()
        {
            Settings.LoadSettings();
            Game.LogTrivial($"Scene Manager is ready.");

            // id is hardware ID and needs to match PatronKey, which is also hardware ID
            if (Settings.id == Settings.PatronKey)
            {
                Game.LogTrivial($"Patron status verified.");
                Game.DisplayNotification($"~o~Scene Manager\n~g~[Patreon]~w~ Thanks for the support, enjoy your session!");
            }
            else
            {
                Game.LogTrivial($"Patron status not verified.");
                Game.DisplayNotification($"~o~Scene Manager\n~y~[Patreon]~w~ Thanks for using my plugin!  If you would like to gain access to benefits such as ~g~new features for this plugin~w~, ~g~early access to new plugins~w~, and ~g~custom plugins made just for you~w~, please consider supporting me on ~b~Patreon~w~. ~y~https://www.patreon.com/richdevs");
            }

            if (Settings.EnableHints)
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

            GameFiber TrafficMenuFiber = new GameFiber(() => TrafficMenu.CheckUserInput());
            TrafficMenuFiber.Start();
        }
    }
}

/*
 * GameFiber.StartNew(delegate{
 * 
 * });
 * 
 *  public static Vehicle[] GetNearbyVehicles2(Vector3 OriginPosition, int amount)
 *  {
 *      return (Vehicle[])(from x in World.GetAllVehicles() orderby x.DistanceTo(OriginPosition) select x).Take(amount).ToArray();
 *  }
 */
