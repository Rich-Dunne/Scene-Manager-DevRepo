using System;
using System.Collections.Generic;
using System.Linq;
using Rage;

namespace SceneManager
{
    public static class VehicleCollector
    {
        // Driving styles https://gtaforums.com/topic/822314-guide-driving-styles/
        // also https://vespura.com/fivem/drivingstyle/
        [Flags]
        private enum EDrivingFlags
        {
            None = 0,
            StopForVehicles = 1,
            StopForPeds = 2,
            AvoidEmptyVehicles = 8,
            AvoidPeds = 16,
            AvoidObjects = 32,
            StopForTrafficLights = 128,
            UseBlinkers = 256,
            AllowWrongWay = 512,
            TakeShortestPath = 262144,
            IgnoreRoads = 4194304,
            IgnorePathfinding = 16777216,
            AvoidHighways = 536870912,
            Normal = StopForVehicles | StopForPeds | AvoidEmptyVehicles | StopForTrafficLights | UseBlinkers | AllowWrongWay | IgnoreRoads,
            TotalControl = AllowWrongWay | AvoidObjects | AvoidPeds | TakeShortestPath | StopForTrafficLights | IgnorePathfinding | StopForVehicles
        }

        public static Dictionary<string, CollectedVehicle> collectedVehicles = new Dictionary<string, CollectedVehicle>();

        public static void StartCollectingAtWaypoint(List<Path> paths, Path path, Waypoint waypoint)
        {
            GameFiber AssignStopFlagForVehiclesFiber = new GameFiber(() => AssignStopForVehiclesFlag(paths, path, waypoint));
            AssignStopFlagForVehiclesFiber.Start();

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
            foreach (Vehicle vehicle in GetNearbyVehicles(waypoint.Position, waypoint.CollectorRadius))
            {
                if (!vehicle)
                {
                    break;
                }

                // If the vehicle is not in the collection yet
                if (!collectedVehicles.ContainsKey(vehicle.LicensePlate))
                {
                    SetVehicleAndDriverPersistence(vehicle);
                    CollectedVehicle newCollectedVehicle = AddVehicleToCollection(path, waypoint, vehicle);
                    Game.LogTrivial($"[WaypointVehicleCollector] Added {vehicle.Model.Name} to collection.");

                    GameFiber AssignTasksFiber = new GameFiber(() => AITasking.AssignWaypointTasks(newCollectedVehicle, path.Waypoints, waypoint));
                    AssignTasksFiber.Start();
                }
                // If the vehicle is in the collection, but has no tasks
                else if (collectedVehicles.ContainsKey(vehicle.LicensePlate) && !collectedVehicles[vehicle.LicensePlate].TasksAssigned)
                {
                    Game.LogTrivial($"[WaypointVehicleCollector] {vehicle.Model.Name} already in collection, but with no tasks.  Assigning tasks.");
                    collectedVehicles[vehicle.LicensePlate].SetTasksAssigned(true);

                    GameFiber AssignTasksFiber = new GameFiber(() => AITasking.AssignWaypointTasks(collectedVehicles[vehicle.LicensePlate], path.Waypoints, waypoint));
                    AssignTasksFiber.Start();
                }
            }
        }

        private static Vehicle[] GetNearbyVehicles(Vector3 collectorPosition, float radius)
        {
            return (from v in World.GetAllVehicles() where v.IsValidForCollection() && v.DistanceTo(collectorPosition) <= radius select v).ToArray(); //v.IsValidForCollection()
        }

        private static void AssignStopForVehiclesFlag(List<Path> paths, Path path, Waypoint waypointData)
        {
            while (paths.Contains(path) && path.Waypoints.Contains(waypointData))
            {
                if (path.IsEnabled)
                {
                    foreach (Vehicle v in GetNearbyVehicles(waypointData.Position, 50f).Where(v => v && v.HasDriver && v.Driver))
                    {
                        SetDriveTaskDrivingFlags(v.Driver, EDrivingFlags.StopForVehicles);
                    }
                }
                GameFiber.Sleep(500);
            }
        }

        private static void SetDriveTaskDrivingFlags(this Ped ped, EDrivingFlags flags)
        {
            ulong SetDriveTaskDrivingFlagsHash = 0xDACE1BE37D88AF67;
            Rage.Native.NativeFunction.CallByHash<int>(SetDriveTaskDrivingFlagsHash, ped, (int)flags);
        }

        private static CollectedVehicle AddVehicleToCollection(Path path, Waypoint waypoint, Vehicle v)
        {
            var collectedVehicle = new CollectedVehicle(v, v.LicensePlate, path.PathNum, path.Waypoints.Count, waypoint.Number, true, false);
            collectedVehicles.Add(v.LicensePlate, collectedVehicle);
            Game.LogTrivial($"[WaypointVehicleCollector] Added {v.Model.Name} to collection from path {path.PathNum}, waypoint {waypoint.Number}.");
            return collectedVehicle;
        }

        public static void SetVehicleAndDriverPersistence(Vehicle v)
        {
            v.IsPersistent = true;
            v.Driver.IsPersistent = true;
            v.Driver.BlockPermanentEvents = true;
            v.Driver.Tasks.Clear();
        }

        private static bool IsValidForCollection(this Vehicle v)
        {
            if(v && v.Speed > 0 && v.IsOnAllWheels && v != Game.LocalPlayer.Character.CurrentVehicle && (v.IsCar || v.IsBike || v.IsBicycle || v.IsQuadBike || (v.HasSiren && !v.IsSirenOn)) && !collectedVehicles.ContainsKey(v.LicensePlate))
            {
                if(v.HasDriver && !v.Driver.IsAlive)
                {
                    return false;
                }
                if (!v.HasDriver)
                {
                    v.CreateRandomDriver();
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

        private static void VehicleDismissed(CollectedVehicle cv, List<Waypoint> waypointData)
        {
            while (!cv.DismissNow)
            {
                GameFiber.Sleep(500);
            }
            Game.LogTrivial($"{cv.Vehicle.Model.Name} was dismissed (dismissal check loop).");


            Game.LogTrivial($"Looping to ensure the vehicle is far enough away from all attractor waypoints so it can be removed from the collection.");
            while (true)
            {
                var collectorWaypoints = waypointData.Where(wp => wp.IsCollector);
                var vehicleFarEnoughAwayFromCollectors = collectorWaypoints.All(wp => cv.Vehicle.DistanceTo(wp.Position) > wp.CollectorRadius);

                if (collectedVehicles.ContainsKey(cv.LicensePlate) && vehicleFarEnoughAwayFromCollectors)
                {
                    Game.LogTrivial($"{cv.Vehicle.Model.Name} is far enough away from all attractor waypoints and has been removed from the collection.");
                    cv.SetTasksAssigned(false);
                    cv.Vehicle.Driver.BlockPermanentEvents = false;
                    cv.Vehicle.Driver.IsPersistent = false;
                    cv.Vehicle.IsPersistent = false;
                    collectedVehicles.Remove(cv.LicensePlate);

                    break;
                }
                GameFiber.Sleep(1000);
            }
        }
    }
}
