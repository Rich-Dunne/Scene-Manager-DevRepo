using Rage;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SceneManager
{
    internal enum State
    {
        Uninitialized,
        Creating,
        Finished
    }

    internal enum SpeedUnits
    {
        MPH,
        KPH
    }

    internal enum DrivingFlagType
    {
        Normal = 263075,
        Direct = 17040259
    }

    public enum DismissOption
    {
        FromPath = 0,
        FromWaypoint = 1,
        FromWorld = 2,
        FromPlayer = 3,
        FromDirected = 4
    }

    internal static class Settings
    {
        internal static readonly InitializationFile ini = new InitializationFile("Plugins/SceneManager.ini");

        // Keybindings
        internal static Keys ToggleKey = Keys.T;
        internal static Keys ModifierKey = Keys.LShiftKey;
        internal static ControllerButtons ToggleButton = ControllerButtons.Y;
        internal static ControllerButtons ModifierButton = ControllerButtons.A;
        // Plugin Settings
        internal static bool Enable3DWaypoints = true;
        internal static bool EnableMapBlips = true;
        internal static bool EnableHints = true;
        internal static SpeedUnits SpeedUnit = SpeedUnits.MPH;
        internal static float BarrierPlacementDistance = 30f;
        // Default Waypoint Settings
        internal static int CollectorRadius = 1;
        internal static int SpeedZoneRadius = 5;
        internal static bool StopWaypoint = false;
        internal static bool DirectDrivingBehavior = false;
        internal static int WaypointSpeed = 5;
        // Barriers
        internal static List<string> barrierKeys = new List<string>();
        internal static List<string> barrierValues = new List<string>();

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
            // Default Waypoint Settings
            CollectorRadius = ini.ReadInt32("Default Waypoint Settings", "CollectorRadius", 1);
            SpeedZoneRadius = ini.ReadInt32("Default Waypoint Settings", "SpeedZoneRadius", 5);
            StopWaypoint = ini.ReadBoolean("Default Waypoint Settings", "StopWaypoint", false);
            DirectDrivingBehavior = ini.ReadBoolean("Default Waypoint Settings", "DirectDrivingBehavior", false);
            WaypointSpeed = ini.ReadInt32("Default Waypoint Settings", "WaypointSpeed", 5);
            CheckForValidWaypointSettings();
            // Barriers
            foreach(string key in ini.GetKeyNames("Barriers"))
            {
                barrierKeys.Add(key.Trim());
                var m = new Model(ini.ReadString("Barriers", key));
                if (m.IsValid)
                    barrierValues.Add(m.Name);
            }

            void CheckForValidWaypointSettings()
            {
                if(CollectorRadius > 50 || CollectorRadius < 1)
                {
                    CollectorRadius = 1;
                }
                if(SpeedZoneRadius > 200 || SpeedZoneRadius < 5)
                {
                    SpeedZoneRadius = 5;
                }
                if(WaypointSpeed > 100 || WaypointSpeed < 5)
                {
                    WaypointSpeed = 5;
                }
            }
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
