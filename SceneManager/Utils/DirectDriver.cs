using Rage;
using RAGENativeUI.Elements;
using SceneManager.CollectedPeds;
using SceneManager.Managers;
using SceneManager.Paths;
using SceneManager.Waypoints;
using System.Linq;

namespace SceneManager.Utils
{
    // The only reason this class should change is to modify how vehicles are directed to paths.
    internal static class DirectDriver
    {
        internal static bool ValidateOptions(UIMenuListScrollerItem<string> menuItem, Path path, out Vehicle vehicle, out Waypoint waypoint)
        {
            var nearbyVehicle = Game.LocalPlayer.Character.GetNearbyVehicles(16).FirstOrDefault(v => v != Game.LocalPlayer.Character.CurrentVehicle && v.VehicleAndDriverValid());
            vehicle = nearbyVehicle;
            waypoint = null;
            if (!nearbyVehicle)
            {
                Game.LogTrivial($"Nearby vehicle is null.");
                return false;
            }

            var firstWaypoint = path.Waypoints.First();
            if (menuItem.SelectedItem == "First waypoint" && firstWaypoint == null)
            {
                Game.LogTrivial($"First waypoint is null.");
                return false;
            }
            else if(menuItem.SelectedItem == "First waypoint" && firstWaypoint != null)
            {
                waypoint = firstWaypoint;
                return true;
            }

            var nearestWaypoint = path.Waypoints.Where(wp => wp.Position.DistanceTo2D(nearbyVehicle.FrontPosition) < wp.Position.DistanceTo2D(nearbyVehicle.RearPosition)).OrderBy(wp => wp.Position.DistanceTo2D(nearbyVehicle)).FirstOrDefault();
            if (menuItem.SelectedItem == "Nearest waypoint" && nearestWaypoint == null)
            {
                Game.LogTrivial($"Nearest waypoint is null.");
                return false;
            }
            else if (menuItem.SelectedItem == "Nearest waypoint" && nearestWaypoint != null)
            {
                waypoint = nearestWaypoint;
                return true;
            }

            Game.LogTrivial($"What are we doing here?");
            return false;
        }

        internal static void Direct(Vehicle nearbyVehicle, Path path, Waypoint targetWaypoint)
        {
            var nearbyVehiclesPath = PathManager.Paths.FirstOrDefault(p => p != null && p.CollectedPeds.Any(v => v.CurrentVehicle == nearbyVehicle));
            if(nearbyVehiclesPath == null)
            {
                Game.LogTrivial($"Nearby vehicle does not belong to any path.");
            }

            var collectedVehicleOnThisPath = path.CollectedPeds.FirstOrDefault(v => v.CurrentVehicle == nearbyVehicle);
            var nearbyCollectedVehicleOtherPath = nearbyVehiclesPath?.CollectedPeds.FirstOrDefault(p => p.CurrentVehicle == nearbyVehicle);
            if (collectedVehicleOnThisPath == null)
            {
                Game.LogTrivial($"Nearby vehicle does not belong to this path.");
                if (nearbyCollectedVehicleOtherPath != null)
                {
                    Game.LogTrivial($"Dismissing nearby vehicle from other path.");
                    nearbyCollectedVehicleOtherPath.Dismiss(Dismiss.FromDirected, path);
                }
                Game.LogTrivial($"[Direct Driver] Adding {nearbyVehicle.Model.Name} to directed path.");
                var newCollectedPed = new CollectedPed(nearbyVehicle.Driver, path, targetWaypoint) { Directed = true };
                path.CollectedPeds.Add(newCollectedPed);
                //collectedVehicleOnThisPath.Tasks.Clear();
            }
        }
    }
}
