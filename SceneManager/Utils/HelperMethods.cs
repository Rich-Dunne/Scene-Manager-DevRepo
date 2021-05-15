using Rage;
using SceneManager.Menus;

namespace SceneManager.Utils
{
    internal class HelperMethods
    {
        internal static float ConvertDriveSpeedForWaypoint(float speed)
        {
            float convertedSpeed = SettingsMenu.SpeedUnits.SelectedItem == SpeedUnits.MPH
                ? MathHelper.ConvertMilesPerHourToMetersPerSecond(speed)
                : MathHelper.ConvertKilometersPerHourToMetersPerSecond(speed);
            return convertedSpeed;
        }
    }
}
