using Rage;
using System.Linq;

namespace SceneManager
{
    internal class CollectedVehicle
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
        internal bool ReadyForDirectTasks { get; set; } = true;

        internal CollectedVehicle(Vehicle vehicle, Path path, Waypoint currentWaypoint)
        {
            Vehicle = vehicle;
            Driver = vehicle.Driver;
            Path = path;
            CurrentWaypoint = currentWaypoint;
            SetPersistence();
        }

        internal CollectedVehicle(Vehicle vehicle, Path path)
        {
            Vehicle = vehicle;
            Driver = vehicle.Driver;
            Path = path;
            SetPersistence();
        }

        internal void SetPersistence()
        {
            Vehicle.IsPersistent = true;
            Driver.IsPersistent = true;
            Driver.BlockPermanentEvents = true;
            Logger.Log($"{Vehicle.Model.Name} and driver are now persistent.");
        }

        internal void Dismiss(DismissOption dismissOption = DismissOption.FromPath)
        {
            if (!Vehicle || !Driver)
            {
                return;
            }

            if (dismissOption == DismissOption.FromWorld)
            {
                DismissFromWorld();
                return;
            }

            if (dismissOption == DismissOption.FromPlayer)
            {
                if (Driver)
                {
                    Driver.Dismiss();
                }
                if (Vehicle)
                {
                    Vehicle.Dismiss();
                }
                Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(Vehicle, 0f, 1, true);
                VehicleCollector.collectedVehicles.Remove(this);
                return;
            }

            if(StoppedAtWaypoint)
            {
                Logger.Log($"Unstucking {Vehicle.Model.Name}");
                StoppedAtWaypoint = false;
                Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(Vehicle, 0f, 1, true);
                Driver.Tasks.CruiseWithVehicle(5f);
            }
            Driver.Tasks.Clear();

            if (dismissOption == DismissOption.FromWaypoint)
            {
                DismissFromWaypoint();
            }

            if (dismissOption == DismissOption.FromPath)
            {
                DismissFromPath();
            }

            void DismissFromWorld()
            {
                Game.LogTrivial($"Dismissed {Vehicle.Model.Name} from the world");
                while (Vehicle.HasOccupants)
                {
                    foreach (Ped occupant in Vehicle.Occupants)
                    {
                        occupant.Dismiss();
                        occupant.Delete();
                    }
                    GameFiber.Yield();
                }
                Vehicle.Delete();
            }

            void DismissFromWaypoint()
            {
                if (CurrentWaypoint == null || Path == null)
                {
                    Logger.Log($"CurrentWaypoint or Path are null");
                }
                else if (CurrentWaypoint?.Number != Path?.Waypoints.Count)
                {
                    Logger.Log($"Dismissed from waypoint.");
                    SkipWaypoint = true;
                }
                else if (CurrentWaypoint?.Number == Path?.Waypoints.Count)
                {
                    DismissFromPath();
                }
            }

            void DismissFromPath()
            {
                Logger.Log($"Dismissing from path");
                Dismissed = true;

                // Check if the vehicle is near any of the path's collector waypoints
                GameFiber.StartNew(() =>
                {
                    var nearestCollectorWaypoint = Path.Waypoints.Where(wp => wp.IsCollector).OrderBy(wp => Vehicle.DistanceTo2D(wp.Position)).FirstOrDefault();
                    if (nearestCollectorWaypoint != null)
                    {
                        // Enabling this will keep the menu, but the dismissed vehicle is immediately re - collected
                        while (nearestCollectorWaypoint != null && Vehicle && Vehicle.HasDriver && Driver && Driver.IsAlive && Vehicle.FrontPosition.DistanceTo2D(nearestCollectorWaypoint.Position) <= nearestCollectorWaypoint.CollectorRadius)
                        {
                            //Game.LogTrivial($"{Vehicle.Model.Name} is within 2x collector radius, cannot be fully dismissed yet.");
                            GameFiber.Yield();
                        }
                    }
                    else
                    {
                        Logger.Log($"Nearest collector is null");
                    }

                    if (!Vehicle || !Driver)
                    {
                        return;
                    }

                    if (!Directed)
                    {
                        VehicleCollector.collectedVehicles.Remove(this);
                        Logger.Log($"{Vehicle.Model.Name} dismissed successfully.");
                        if (Driver)
                        {
                            if (Driver.GetAttachedBlip())
                            {
                                Driver.GetAttachedBlip().Delete();
                            }
                            Driver.BlockPermanentEvents = false;
                            Driver.Dismiss();
                        }
                        if (Vehicle)
                        {
                            Vehicle.IsSirenOn = false;
                            Vehicle.IsSirenSilent = true;
                            Vehicle.Dismiss();
                        }
                    }
                });
                
            }
        }
    }
}
