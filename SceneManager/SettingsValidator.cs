using Rage;

namespace SceneManager
{
    // The only reason this class should change is to modify how settings are validated
    internal class SettingsValidator
    {
        internal static void ValidateWaypointSettings()
        {
            if (Settings.CollectorRadius > 50 || Settings.CollectorRadius < 1)
            {
                Settings.CollectorRadius = 1;
                Game.LogTrivial($"Invalid value for CollectorRadius in user settings, resetting to default.");
            }
            if (Settings.SpeedZoneRadius > 200 || Settings.SpeedZoneRadius < 5)
            {
                Settings.SpeedZoneRadius = 5;
                Game.LogTrivial($"Invalid value for SpeedZoneRadius in user settings, resetting to default.");
            }
            if (Settings.CollectorRadius > Settings.SpeedZoneRadius)
            {
                Settings.CollectorRadius = 1;
                Settings.SpeedZoneRadius = 5;
                Game.LogTrivial($"CollectorRadius is greater than SpeedZoneRadius in user settings, resetting to defaults.");
            }
            if (Settings.WaypointSpeed > 100 || Settings.WaypointSpeed < 5)
            {
                Settings.WaypointSpeed = 5;
                Game.LogTrivial($"Invalid value for WaypointSpeed in user settings, resetting to default.");
            }
        }

        internal static void ValidateBarrierSettings(InitializationFile ini)
        {
            foreach (string displayName in ini.GetKeyNames("Barriers"))
            {
                var model = new Model(ini.ReadString("Barriers", displayName.Trim()));
                if (model.IsValid)
                {
                    Settings.BarrierModels.Add(displayName, model);
                }
                else
                {
                    Game.LogTrivial($"{model.Name} is not valid.");
                }
            }
        }
    }
}
