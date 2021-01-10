using Rage;
using SceneManager.Objects;
using System.Linq;

namespace SceneManager.Utils
{
    internal static class DismissDriver
    {
        internal static void Dismiss(int dismissIndex)
        {
            var nearbyVehicle = Game.LocalPlayer.Character.GetNearbyVehicles(16).FirstOrDefault(v => v != Game.LocalPlayer.Character.CurrentVehicle && v.VehicleAndDriverValid());
            if (!nearbyVehicle)
            {
                Game.LogTrivial($"Nearby vehicle is null.");
                return;
            }

            if(dismissIndex == (int)Utils.Dismiss.FromWorld)
            {
                // Have to loop because sometimes police peds don't get deleted properly
                // The path should handle removing the deleted driver/vehicle from its list of collected vehicles
                while (nearbyVehicle && nearbyVehicle.HasOccupants)
                {
                    nearbyVehicle.Occupants.ToList().ForEach(x => x.Delete());
                    GameFiber.Yield();
                }
                if (nearbyVehicle)
                {
                    nearbyVehicle.Delete();
                }
                return;
            }
            else
            {
                CollectedVehicle collectedVehicle = PathManager.Paths.SelectMany(x => x.CollectedVehicles).FirstOrDefault(x => x.Vehicle == nearbyVehicle);
                if(collectedVehicle != null)
                {
                    collectedVehicle.Dismiss((Dismiss)dismissIndex);
                }
            }
        }
    }
}
