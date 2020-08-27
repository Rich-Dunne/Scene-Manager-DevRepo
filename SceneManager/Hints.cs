using Rage;

namespace SceneManager
{
    class Hints
    {
        public static bool Enabled { get; set; } = SettingsMenu.hints.Checked;

        public static void Display(string message)
        {
            if (Enabled)
            {
                Game.DisplayNotification($"{message}");
            }
        }
    }
}
