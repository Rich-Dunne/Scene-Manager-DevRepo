using Rage;
using RAGENativeUI.Elements;
using SceneManager.Objects;
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
            var nearbyVehiclesPath = PathManager.Paths.FirstOrDefault(p => p.CollectedVehicles.Any(v => v.Vehicle == nearbyVehicle));
            if(nearbyVehiclesPath == null)
            {
                Game.LogTrivial($"Nearby vehicle does not belong to any path.");
            }

            var collectedVehicleOnThisPath = path.CollectedVehicles.FirstOrDefault(v => v.Vehicle == nearbyVehicle);
            var nearbyCollectedVehicleOtherPath = nearbyVehiclesPath?.CollectedVehicles.FirstOrDefault(v => v.Vehicle == nearbyVehicle);
            if (collectedVehicleOnThisPath == null)
            {
                Game.LogTrivial($"Nearby vehicle does not belong to this path.");
                if (nearbyCollectedVehicleOtherPath != null)
                {
                    Game.LogTrivial($"Dismissing nearby vehicle from other path.");
                    nearbyCollectedVehicleOtherPath.Dismiss(Dismiss.FromDirected, path);
                }
                Game.LogTrivial($"[Direct Driver] Adding {nearbyVehicle.Model.Name} to directed path.");
                path.CollectedVehicles.Add(collectedVehicleOnThisPath = new CollectedVehicle(nearbyVehicle, path));
                collectedVehicleOnThisPath.Directed = true;
                collectedVehicleOnThisPath.Driver.Tasks.Clear();
            }

            GameFiber.StartNew(() => collectedVehicleOnThisPath.AssignWaypointTasks(path, targetWaypoint));
        }
    }
}
