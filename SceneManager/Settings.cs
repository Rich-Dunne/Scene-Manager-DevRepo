using Rage;
using System.Windows.Forms;

namespace SceneManager
{
    public enum State
    {
        Uninitialized,
        Creating,
        Finished
    }

    public enum SpeedUnits
    {
        MPH,
        KPH
    }

    internal static class Settings
    {
        internal static readonly InitializationFile ini = new InitializationFile("Plugins/SceneManager.ini");

        internal static Keys ToggleKey = Keys.T;
        internal static Keys ModifierKey = Keys.LShiftKey;
        internal static ControllerButtons ToggleButton = ControllerButtons.Y;
        internal static ControllerButtons ModifierButton = ControllerButtons.A;
        internal static bool Enable3DWaypoints = true;
        internal static bool EnableMapBlips = true;
        internal static bool EnableHints = true;
        internal static SpeedUnits SpeedUnit = SpeedUnits.MPH;
        internal static float BarrierPlacementDistance = 30f;
        internal static Object[] barriers;

        internal static void LoadSettings()
        {
            Game.LogTrivial("Loading SceneManager.ini settings");
            //InitializationFile ini = new InitializationFile("Plugins/SceneManager.ini");
            ini.Create();

            ToggleKey = ini.ReadEnum("Keybindings", "ToggleKey", Keys.T);
            ModifierKey = ini.ReadEnum("Keybindings", "ModifierKey", Keys.LShiftKey);
            ToggleButton = ini.ReadEnum("Keybindings", "ToggleButton", ControllerButtons.A);
            ModifierButton = ini.ReadEnum("Keybindings", "ModifierButton", ControllerButtons.DPadDown);
            Enable3DWaypoints = ini.ReadBoolean("Other Settings", "Enable3DWaypoints", true);
            EnableMapBlips = ini.ReadBoolean("Other Settings", "EnableMapBlips", true);
            EnableHints = ini.ReadBoolean("Other Settings", "EnableHints", true);
            SpeedUnit = ini.ReadEnum("Other Settings", "SpeedUnits", SpeedUnits.MPH);
            BarrierPlacementDistance = ini.ReadInt32("Other Settings", "BarrierPlacementDistance", 30);
        }

        internal static void UpdateSettings(bool threeDWaypointsEnabled, bool mapBlipsEnabled, bool hintsEnabled, SpeedUnits unit, float distance)
        {
            ini.Write("Other Settings", "Enable3DWaypoints", threeDWaypointsEnabled);
            ini.Write("Other Settings", "EnableMapBlips", mapBlipsEnabled);
            ini.Write("Other Settings", "EnableHints", hintsEnabled);
            ini.Write("Other Settings", "SpeedUnits", unit);
        }
    }
}
