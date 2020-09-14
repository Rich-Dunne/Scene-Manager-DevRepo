using Rage;
using System.Linq;

namespace SceneManager
{
    public class CollectedVehicle
    {
        public Ped Driver { get; set; }
        public Vehicle Vehicle { get; set; }
        public Path Path { get; set; }
        public Waypoint CurrentWaypoint { get; set; }
        public Waypoint NextWaypoint { get; private set; }
        public bool StoppedAtWaypoint { get; set; }
        public bool Dismissed { get; set; }
        public bool SkipWaypoint { get; set; }

        public CollectedVehicle(Vehicle vehicle, Path path, Waypoint currentWaypoint)
        {
            Vehicle = vehicle;
            Driver = vehicle.Driver;
            Path = path;
            CurrentWaypoint = currentWaypoint;
        }

        public CollectedVehicle(Vehicle vehicle, Path path)
        {
            Vehicle = vehicle;
            Driver = vehicle.Driver;
            Path = path;
        }

        public void Dismiss()
        {
            GameFiber.StartNew(() =>
            {
                if (!Vehicle || !Driver)
                {
                    return;
                }
                Dismissed = true;
                StoppedAtWaypoint = false;

                Driver.Tasks.Clear();
                Driver.Tasks.PerformDrivingManeuver(Vehicle, VehicleManeuver.GoForwardWithCustomSteeringAngle, 3);

                if (Driver.GetAttachedBlip())
                {
                    Driver.GetAttachedBlip().Delete();
                }

                // check if the vehicle is near any of the path's collector waypoints
                var nearestCollectorWaypoint = Path.Waypoints.Where(wp => wp.IsCollector && Vehicle.DistanceTo2D(wp.Position) <= wp.CollectorRadius * 2).FirstOrDefault();
                if (nearestCollectorWaypoint != null)
                {
                    while (nearestCollectorWaypoint != null && Vehicle && Driver && Vehicle.DistanceTo2D(nearestCollectorWaypoint.Position) <= nearestCollectorWaypoint.CollectorRadius * 2)
                    {
                        //Game.LogTrivial($"{_vehicle.Model.Name} is too close to the collector to be fully dismissed.");
                        GameFiber.Yield();
                    }
                }

                if (!Vehicle || !Driver)
                {
                    return;
                }

                VehicleCollector.collectedVehicles.Remove(this);
                Game.LogTrivial($"{Vehicle.Model.Name} dismissed successfully.");
                Driver.BlockPermanentEvents = false;
                Driver.Dismiss();
                Vehicle.IsSirenOn = false;
                Vehicle.IsSirenSilent = true;
                Vehicle.Dismiss();
            });
        }
    }
}
