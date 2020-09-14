using Rage;

namespace SceneManager
{
    class Hints
    {
        internal static bool Enabled { get; set; } = SettingsMenu.hints.Checked;

        internal static void Display(string message)
        {
            if (Enabled)
            {
                Game.DisplayNotification($"{message}");
            }
        }
    }
}
