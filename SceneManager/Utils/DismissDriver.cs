using Rage;
using SceneManager.CollectedPeds;
using SceneManager.Managers;
using System.Linq;

namespace SceneManager.Utils
{
    internal static class DismissDriver
    {
        internal static void Dismiss(int dismissIndex)
        {
            var nearbyVehicle = Game.LocalPlayer.Character.GetNearbyVehicles(16).FirstOrDefault(v => v.VehicleAndDriverValid() && v != Game.LocalPlayer.Character.CurrentVehicle);
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
                CollectedPed collectedPed = PathManager.Paths.Where(x => x != null).SelectMany(x => x.CollectedPeds).FirstOrDefault(x => x.CurrentVehicle == nearbyVehicle);
                if(collectedPed != null)
                {
                    collectedPed.Dismiss((Dismiss)dismissIndex);
                }
            }
        }
    }
}
