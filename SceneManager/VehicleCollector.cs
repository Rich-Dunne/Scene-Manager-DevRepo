using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rage;

namespace SceneManager
{
    internal static class VehicleCollector
    {
        // Driving styles https://gtaforums.com/topic/822314-guide-driving-styles/
        // also https://vespura.com/fivem/drivingstyle/

        internal static List<CollectedVehicle> collectedVehicles = new List<CollectedVehicle>();

        internal static void StartCollectingAtWaypoint(List<Path> paths, Path path, Waypoint waypoint)
        {
            LoopForVehiclesToBeDismissed(paths, path);

            while (paths.Contains(path) && path.Waypoints.Contains(waypoint))
            {
                if (path.IsEnabled && waypoint.IsCollector)
                {
                    LoopForNearbyValidVehicles(path, waypoint);
                }
                GameFiber.Sleep(100);
            }
        }

        private static void LoopForVehiclesToBeDismissed(List<Path> paths, Path path)
        {
            GameFiber.StartNew(() =>
            {
                while (paths.Contains(path))
                {
                    //Logger.Log($"Dismissing unused vehicles for cleanup");
                    foreach (CollectedVehicle cv in collectedVehicles.Where(cv => cv.Vehicle))
                    {
                        if (!cv.Vehicle.IsDriveable || cv.Vehicle.IsUpsideDown || !cv.Vehicle.HasDriver)
                        {
                            if (cv.Vehicle.HasDriver)
                            {
                                cv.Vehicle.Driver.Dismiss();
                            }
                            cv.Vehicle.Dismiss();
                        }
                    }

                    collectedVehicles.RemoveAll(cv => !cv.Vehicle);
                    GameFiber.Sleep(60000);
                }
            });
        }

        private static void LoopForNearbyValidVehicles(Path path, Waypoint waypoint)
        {
            foreach (Vehicle vehicle in GetNearbyVehiclesForCollection(waypoint.Position, waypoint.CollectorRadius))
            {
                if (!vehicle)
                {
                    break;
                }

                //Logger.Log($"Vehicle: {vehicle.Model.Name}, Waypoint collector radius: {waypoint.CollectorRadius}, Distance to waypoint: {vehicle.DistanceTo2D(waypoint.Position)}");

                var collectedVehicle = collectedVehicles.Where(cv => cv.Vehicle == vehicle).FirstOrDefault();
                if(collectedVehicle == null)
                {
                    SetVehicleAndDriverPersistence(vehicle);
                    CollectedVehicle newCollectedVehicle = AddVehicleToCollection(path, waypoint, vehicle);

                    GameFiber AssignTasksFiber = new GameFiber(() => AITasking.AssignWaypointTasks(newCollectedVehicle, path.Waypoints, waypoint));
                    AssignTasksFiber.Start();
                }
            }

            Vehicle[] GetNearbyVehiclesForCollection(Vector3 collectorWaypointPosition, float collectorRadius)
            {
                return (from v in World.GetAllVehicles() where v.DistanceTo2D(collectorWaypointPosition) <= collectorRadius*2 && Math.Abs(collectorWaypointPosition.Z - v.Position.Z) < 3 && v.IsValidForCollection() select v).ToArray();
            }
        }

        private static CollectedVehicle AddVehicleToCollection(Path path, Waypoint waypoint, Vehicle v)
        {
            var collectedVehicle = new CollectedVehicle(v, path, waypoint);
            collectedVehicles.Add(collectedVehicle);
            Logger.Log($"Added {v.Model.Name} to collection from path {path.Number}, waypoint {waypoint.Number}.");
            return collectedVehicle;
        }

        private static bool IsValidForCollection(this Vehicle v)
        {
            if(v && v.Speed > 1 && v.IsOnAllWheels && v.IsEngineOn && v != Game.LocalPlayer.Character.CurrentVehicle && v != Game.LocalPlayer.Character.LastVehicle && (v.IsCar || v.IsBike || v.IsBicycle || v.IsQuadBike) && !v.IsSirenOn && !collectedVehicles.Any(cv => cv?.Vehicle == v))
            {
                if(v.HasDriver && v.Driver && !v.Driver.IsAlive)
                {
                    return false;
                }
                if (!v.HasDriver)
                {
                    v.CreateRandomDriver();
                    while (!v.HasDriver)
                    {
                        GameFiber.Yield();
                    }
                    if(v && v.Driver)
                    {
                        var driverBlip = v.Driver.AttachBlip();
                        driverBlip.Color = Color.Green;
                        driverBlip.Scale = 0.25f;
                        v.Driver.IsPersistent = true;
                        v.Driver.BlockPermanentEvents = true;
                        Logger.Log($"A missing driver was created for {v.Model.Name}.");
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static void SetVehicleAndDriverPersistence(Vehicle v)
        {
            v.IsPersistent = true;
            v.Driver.IsPersistent = true;
            v.Driver.BlockPermanentEvents = true;
            v.Driver.Tasks.Clear();
        }
    }
}
