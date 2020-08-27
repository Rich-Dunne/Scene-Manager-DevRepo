using Rage;
using System.Windows.Forms;

namespace SceneManager
{
    internal static class Settings
    {
        internal static Keys ToggleKey = Keys.T;
        internal static Keys ModifierKey = Keys.LShiftKey;
        internal static ControllerButtons ToggleButton = ControllerButtons.Y;
        internal static ControllerButtons ModifierButton = ControllerButtons.A;
        internal static bool EnableHints = true;
        internal static Object[] barriers;

        internal static void LoadSettings()
        {
            Game.LogTrivial("Loading SceneManager.ini settings");
            InitializationFile ini = new InitializationFile("Plugins/SceneManager.ini");
            ini.Create();

            ToggleKey = ini.ReadEnum("Keybindings", "ToggleKey", Keys.T);
            ModifierKey = ini.ReadEnum("Keybindings", "ModifierKey", Keys.LShiftKey);
            ToggleButton = ini.ReadEnum("Keybindings", "ToggleButton", ControllerButtons.A);
            ModifierButton = ini.ReadEnum("Keybindings", "ModifierButton", ControllerButtons.DPadDown);
            EnableHints = ini.ReadBoolean("Other Settings", "EnableHints", true);
        }
    }
}
