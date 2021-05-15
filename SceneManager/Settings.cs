using Rage;
using System.Collections.Generic;
using System.Windows.Forms;
using SceneManager.Utils;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using System.Xml.Serialization;
using SceneManager.Managers;

namespace SceneManager
{
    // The only reason this class should change is to modify any plugin settings
    internal static class Settings
    {
        internal static readonly InitializationFile ini = new InitializationFile("Plugins/SceneManager.ini");

        // Keybindings
        internal static Keys ToggleKey { get; private set; } = Keys.T;
        internal static Keys ModifierKey { get; private set; } = Keys.LShiftKey;
        internal static ControllerButtons ToggleButton { get; private set; } = ControllerButtons.Y;
        internal static ControllerButtons ModifierButton { get; private set; } = ControllerButtons.A;

        // Plugin Settings
        internal static bool Enable3DWaypoints { get; private set; } = true;
        internal static bool EnableMapBlips { get; private set; } = true;
        internal static bool EnableHints { get; private set; } = true;
        internal static SpeedUnits SpeedUnit { get; private set; } = SpeedUnits.MPH;
        internal static float BarrierPlacementDistance { get; private set; } = 30f;
        internal static bool EnableAdvancedBarricadeOptions { get; private set; } = false;
        internal static bool EnableBarrierLightsDefaultOn { get; private set; } = false;

        // Default Waypoint Settings
        internal static int CollectorRadius { get; set; } = 1;
        internal static int SpeedZoneRadius { get; set; } = 5;
        internal static bool StopWaypoint { get; set; } = false;
        internal static bool DirectDrivingBehavior { get; set; } = false;
        internal static int WaypointSpeed { get; set; } = 5;

        // Barriers
        internal static Dictionary<string, Model> BarrierModels { get; private set; } = new Dictionary<string, Model>();

        internal static void LoadSettings()
        {
            Game.LogTrivial("Loading SceneManager.ini settings");
            ini.Create();

            // Keybindings
            ToggleKey = ini.ReadEnum("Keybindings", "ToggleKey", Keys.T);
            ModifierKey = ini.ReadEnum("Keybindings", "ModifierKey", Keys.LShiftKey);
            ToggleButton = ini.ReadEnum("Keybindings", "ToggleButton", ControllerButtons.A);
            ModifierButton = ini.ReadEnum("Keybindings", "ModifierButton", ControllerButtons.DPadDown);
            
            // Plugin Settings
            Enable3DWaypoints = ini.ReadBoolean("Plugin Settings", "Enable3DWaypoints", true);
            EnableMapBlips = ini.ReadBoolean("Plugin Settings", "EnableMapBlips", true);
            EnableHints = ini.ReadBoolean("Plugin Settings", "EnableHints", true);
            SpeedUnit = ini.ReadEnum("Plugin Settings", "SpeedUnits", SpeedUnits.MPH);
            BarrierPlacementDistance = ini.ReadInt32("Plugin Settings", "BarrierPlacementDistance", 30);
            EnableAdvancedBarricadeOptions = ini.ReadBoolean("Plugin Settings", "EnableAdvancedBarricadeOptions", false);
            EnableBarrierLightsDefaultOn = ini.ReadBoolean("Plugin Settings", "EnableBarrierLightsDefaultOn", false);

            // Default Waypoint Settings
            CollectorRadius = ini.ReadInt32("Default Waypoint Settings", "CollectorRadius", 1);
            SpeedZoneRadius = ini.ReadInt32("Default Waypoint Settings", "SpeedZoneRadius", 5);
            StopWaypoint = ini.ReadBoolean("Default Waypoint Settings", "StopWaypoint", false);
            DirectDrivingBehavior = ini.ReadBoolean("Default Waypoint Settings", "DirectDrivingBehavior", false);
            WaypointSpeed = ini.ReadInt32("Default Waypoint Settings", "WaypointSpeed", 5);
            
            SettingsValidator.ValidateWaypointSettings();
            SettingsValidator.ValidateBarrierSettings(ini);
        }

        internal static void UpdateSettings(bool threeDWaypointsEnabled, bool mapBlipsEnabled, bool hintsEnabled, SpeedUnits unit)
        {
            ini.Write("Other Settings", "Enable3DWaypoints", threeDWaypointsEnabled);
            ini.Write("Other Settings", "EnableMapBlips", mapBlipsEnabled);
            ini.Write("Other Settings", "EnableHints", hintsEnabled);
            ini.Write("Other Settings", "SpeedUnits", unit);
        }
    }
}
