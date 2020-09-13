using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rage;

namespace SceneManager
{
    public static class VehicleCollector
    {
        // Driving styles https://gtaforums.com/topic/822314-guide-driving-styles/
        // also https://vespura.com/fivem/drivingstyle/

        public static List<CollectedVehicle> collectedVehicles = new List<CollectedVehicle>();

        public static void StartCollectingAtWaypoint(List<Path> paths, Path path, Waypoint waypoint)
        {
            while (paths.Contains(path) && path.Waypoints.Contains(waypoint))
            {
                if (path.IsEnabled && waypoint.IsCollector)
                {
                    LoopForNearbyValidVehicles(path, waypoint);
                }
                GameFiber.Sleep(100);
            }
        }

        private static void LoopForNearbyValidVehicles(Path path, Waypoint waypoint)
        {
            foreach (Vehicle vehicle in GetNearbyVehiclesForCollection(waypoint.Position, waypoint.CollectorRadius))
            {
                if (!vehicle)
                {
                    break;
                }

                Game.LogTrivial($"Vehicle: {vehicle.Model.Name}, Waypoint collector radius: {waypoint.CollectorRadius}, Distance to waypoint: {vehicle.DistanceTo2D(waypoint.Position)}");

                var collectedVehicle = collectedVehicles.Where(cv => cv.Vehicle == vehicle) as CollectedVehicle;
                // If the vehicle is not in the collection yet
                if(collectedVehicle == null)
                {
                    SetVehicleAndDriverPersistence(vehicle);
                    CollectedVehicle newCollectedVehicle = AddVehicleToCollection(path, waypoint, vehicle);
                    newCollectedVehicle.TasksAssigned = true;

                    GameFiber AssignTasksFiber = new GameFiber(() => AITasking.AssignWaypointTasks(newCollectedVehicle, path.Waypoints, waypoint));
                    AssignTasksFiber.Start();
                }
                // If the vehicle is in the collection, but has no tasks
                else if (!collectedVehicle.TasksAssigned)
                {
                    Game.LogTrivial($"[WaypointVehicleCollector] {vehicle.Model.Name} already in collection, but with no tasks.  Assigning tasks.");
                    collectedVehicle.TasksAssigned = true;

                    GameFiber AssignTasksFiber = new GameFiber(() => AITasking.AssignWaypointTasks(collectedVehicle, path.Waypoints, waypoint));
                    AssignTasksFiber.Start();
                }
            }
        }

        private static Vehicle[] GetNearbyVehiclesForCollection(Vector3 collectorWaypointPosition, float collectorRadius)
        {
            return (from v in World.GetAllVehicles() where v.DistanceTo2D(collectorWaypointPosition) < collectorRadius && Math.Abs(collectorWaypointPosition.Z - v.Position.Z) < 3 && v.IsValidForCollection() select v).ToArray();
        }

        private static CollectedVehicle AddVehicleToCollection(Path path, Waypoint waypoint, Vehicle v)
        {
            var collectedVehicle = new CollectedVehicle(v, path, waypoint, false);
            collectedVehicles.Add(collectedVehicle);
            Game.LogTrivial($"[WaypointVehicleCollector] Added {v.Model.Name} to collection from path {path.Number}, waypoint {waypoint.Number}.");
            return collectedVehicle;
        }

        private static bool IsValidForCollection(this Vehicle v)
        {
            if(v && v.Speed > 1 && v.IsOnAllWheels && v != Game.LocalPlayer.Character.CurrentVehicle && v != Game.LocalPlayer.Character.LastVehicle && (v.IsCar || v.IsBike || v.IsBicycle || v.IsQuadBike || (v.HasSiren && !v.IsSirenOn)) && !collectedVehicles.Any(cv => cv.Vehicle == v))
            {
                if(v.HasDriver && !v.Driver.IsAlive)
                {
                    return false;
                }
                if (!v.HasDriver)
                {
                    v.CreateRandomDriver();
                    var driverBlip = v.Driver.AttachBlip();
                    driverBlip.Color = Color.Green;
                    driverBlip.Scale = 0.25f;
                    v.Driver.IsPersistent = true;
                    v.Driver.BlockPermanentEvents = true;
                    Game.LogTrivial($"A missing driver was created for {v.Model.Name}");
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void SetVehicleAndDriverPersistence(Vehicle v)
        {
            v.IsPersistent = true;
            v.Driver.IsPersistent = true;
            v.Driver.BlockPermanentEvents = true;
            v.Driver.Tasks.Clear();
        }
    }
}
