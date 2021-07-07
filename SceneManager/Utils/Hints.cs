using Rage;
using System.Windows.Forms;
using SceneManager.Menus;

namespace SceneManager
{
    class Hints
    {
        internal static bool Enabled { get; set; } = SettingsMenu.Hints.Checked;

        internal static void Display(string message)
        {
            if (Enabled)
            {
                Game.DisplayNotification($"{message}");
            }
        }

        internal static void DisplayHintsToOpenMenu()
        {
            if (Settings.ModifierKey == Keys.None && Settings.ModifierButton == ControllerButtons.None)
            {
                Display($"~o~Scene Manager ~y~[Hint]\n~w~To open the menu, press the ~b~{Settings.ToggleKey} key ~w~or ~b~{Settings.ToggleButton} button");
            }
            else if (Settings.ModifierKey == Keys.None)
            {
                Display($"~o~Scene Manager ~y~[Hint]\n~w~To open the menu, press the ~b~{Settings.ToggleKey} key ~w~or ~b~{Settings.ModifierButton} ~w~+ ~b~{Settings.ToggleButton} buttons");
            }
            else if (Settings.ModifierButton == ControllerButtons.None)
            {
                Display($"~o~Scene Manager ~y~[Hint]\n~w~To open the menu, press ~b~{Settings.ModifierKey} ~w~+ ~b~{Settings.ToggleKey} ~w~or the ~b~{Settings.ToggleButton} button");
            }
            else
            {
                Display($"~o~Scene Manager ~y~[Hint]\n~w~To open the menu, press the ~b~{Settings.ModifierKey} ~w~+ ~b~{Settings.ToggleKey} keys ~w~or ~b~{Settings.ModifierButton} ~w~+ ~b~{Settings.ToggleButton} buttons");
            }
        }
    }
}
