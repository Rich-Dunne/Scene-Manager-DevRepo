using Rage;
using System.Linq;

namespace SceneManager
{
    public class CollectedVehicle
    {
        internal Ped Driver { get; set; }
        internal Vehicle Vehicle { get; set; }
        internal Path Path { get; set; }
        internal Waypoint CurrentWaypoint { get; set; }
        internal Waypoint NextWaypoint { get; private set; }
        internal bool StoppedAtWaypoint { get; set; } = false;
        internal bool Dismissed { get; set; } = false;
        internal bool Directed { get; set; } = false;
        internal bool SkipWaypoint { get; set; } = false;

        internal CollectedVehicle(Vehicle vehicle, Path path, Waypoint currentWaypoint)
        {
            Vehicle = vehicle;
            Driver = vehicle.Driver;
            Path = path;
            CurrentWaypoint = currentWaypoint;
        }

        internal CollectedVehicle(Vehicle vehicle, Path path)
        {
            Vehicle = vehicle;
            Driver = vehicle.Driver;
            Path = path;
        }

        internal void Dismiss(DismissOption dismissOption = DismissOption.FromPath)
        {
            if (!Vehicle || !Driver)
            {
                return;
            }

            if (dismissOption == DismissOption.FromWorld)
            {
                Game.LogTrivial($"Dismissed {Vehicle.Model.Name} from the world");
                while (Vehicle.HasOccupants)
                {
                    foreach(Ped occupant in Vehicle.Occupants)
                    {
                        occupant.Dismiss();
                        occupant.Delete();
                    }
                    GameFiber.Yield();
                }
                Vehicle.Delete();
                return;
            }

            Driver.Tasks.Clear();
            if(StoppedAtWaypoint)
            {
                Logger.Log($"Unstucking stopped vehicle");
                Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(Vehicle, 0f, 1, true);
                GameFiber.StartNew(() =>
                {
                    while(Vehicle && Vehicle.Speed < 1f)
                    {
                        GameFiber.Yield();
                    }
                });
                StoppedAtWaypoint = false;
            }

            if(dismissOption == DismissOption.FromWaypoint && CurrentWaypoint.Number != Path.Waypoints.Count)
            {
                Logger.Log($"Dismissed from waypoint.");
                SkipWaypoint = true;
                //GameFiber.StartNew(() =>
                //{
                //    GameFiber.Sleep(100);
                //    SkipWaypoint = false;
                //    Logger.Log($"SkipWaypoint false");
                //});
            }

            if(dismissOption == DismissOption.FromWaypoint && CurrentWaypoint.Number == Path.Waypoints.Count || dismissOption == DismissOption.FromPath)
            {
                Dismissed = true;
                GameFiber.StartNew(() =>
                {
                    // check if the vehicle is near any of the path's collector waypoints
                    var nearestCollectorWaypoint = Path.Waypoints.Where(wp => wp.IsCollector && Vehicle.DistanceTo2D(wp.Position) <= wp.CollectorRadius * 2).FirstOrDefault();
                    while (nearestCollectorWaypoint != null && Vehicle && Driver && Vehicle.DistanceTo2D(nearestCollectorWaypoint.Position) <= nearestCollectorWaypoint.CollectorRadius * 2)
                    {
                        //Game.LogTrivial($"{Vehicle.Model.Name} is too close to the collector to be fully dismissed.");
                        GameFiber.Yield();
                    }

                    if (!Vehicle || !Driver)
                    {
                        return;
                    }

                    VehicleCollector.collectedVehicles.Remove(this);
                    Logger.Log($"{Vehicle.Model.Name} dismissed successfully.");
                    if (Driver.GetAttachedBlip())
                    {
                        Driver.GetAttachedBlip().Delete();
                    }
                    Driver.BlockPermanentEvents = false;
                    Driver.Dismiss();
                    Vehicle.IsSirenOn = false;
                    Vehicle.IsSirenSilent = true;
                    Vehicle.Dismiss();
                });
            }
        }
    }
}
