using System;
using System.Collections.Generic;
using System.Linq;
using Rage;

namespace SceneManager
{
    public static class TrafficPathing
    {
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
            foreach (Vehicle vehicle in GetNearbyVehicles(waypoint.Position, waypoint.CollectorRadius).Where(v => v.IsValidForCollection()))
            {
                // If the vehicle is not in the collection yet
                if (!collectedVehicles.ContainsKey(vehicle.LicensePlate))
                {
                    SetVehicleAndDriverPersistence(vehicle);
                    CollectedVehicle newCollectedVehicle = AddVehicleToCollection(path, waypoint, vehicle);
                    Game.LogTrivial($"[WaypointVehicleCollector] Added {vehicle.Model.Name} to collection.");
                    //GameFiber DismissCheckFiber = new GameFiber(() => VehicleDismissed(collectedVehicle, path.Waypoints));
                    //DismissCheckFiber.Start();

                    GameFiber AssignTasksFiber = new GameFiber(() => AssignWaypointTasks(newCollectedVehicle, path.Waypoints, waypoint));
                    AssignTasksFiber.Start();
                }
                // If the vehicle is in the collection, but has no tasks
                else if (collectedVehicles.ContainsKey(vehicle.LicensePlate) && !collectedVehicles[vehicle.LicensePlate].TasksAssigned)
                {
                    Game.LogTrivial($"[WaypointVehicleCollector] {vehicle.Model.Name} already in collection, but with no tasks.  Assigning tasks.");
                    collectedVehicles[vehicle.LicensePlate].SetTasksAssigned(true);

                    GameFiber AssignTasksFiber = new GameFiber(() => AssignWaypointTasks(collectedVehicles[vehicle.LicensePlate], path.Waypoints, waypoint));
                    AssignTasksFiber.Start();
                }
            }
        }

        private static CollectedVehicle AddVehicleToCollection(Path path, Waypoint waypoint, Vehicle v)
        {
            var collectedVehicle = new CollectedVehicle(v, v.LicensePlate, path.PathNum, path.Waypoints.Count, waypoint.Number, true, false, false);
            collectedVehicles.Add(v.LicensePlate, collectedVehicle);
            Game.LogTrivial($"[WaypointVehicleCollector] Added {v.Model.Name} to collection from path {path.PathNum}, waypoint {waypoint.Number}.");
            return collectedVehicle;
        }

        private static void SetVehicleAndDriverPersistence(Vehicle v)
        {
            v.IsPersistent = true;
            v.Driver.IsPersistent = true;
            v.Driver.BlockPermanentEvents = true;
            v.Driver.Tasks.Clear();
        }

        private static bool IsValidForCollection(this Vehicle v)
        {
            if (v && v.HasDriver && v.Driver && v.Driver.IsAlive && v != Game.LocalPlayer.Character.CurrentVehicle && (v.IsCar || v.IsBike || v.IsBicycle || v.IsQuadBike || (v.HasSiren && !v.IsSirenOn)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void AssignWaypointTasks(CollectedVehicle collectedVehicle, List<Waypoint> waypoints, Waypoint currentWaypoint)
        {
            var vehicle = collectedVehicle.Vehicle;
            var driver = vehicle.Driver;

            if (currentWaypoint != null && currentWaypoint.DrivingFlag == VehicleDrivingFlags.StopAtDestination)
            {
                Game.LogTrivial($"{collectedVehicle.Vehicle.Model.Name} stopping at waypoint.");
                collectedVehicle.Vehicle.Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraightBraking);
                collectedVehicle.SetStoppedAtWaypoint(true);
            }

            for (int i = currentWaypoint.Number; i < waypoints.Count; i++)
            {
                if (waypoints.ElementAtOrDefault(i) == null)
                {
                    Game.LogTrivial($"Waypoint is null");
                    break;
                }

                Game.LogTrivial($"{vehicle.Model.Name} is driving to waypoint {waypoints[i].Number}");
                if (waypoints.ElementAtOrDefault(i) != null && !collectedVehicle.StoppedAtWaypoint)
                {
                    collectedVehicle.Vehicle.Driver.Tasks.DriveToPosition(waypoints[i].Position, waypoints[i].Speed, (VehicleDrivingFlags)263043, 2f).WaitForCompletion();
                }

                if (waypoints.ElementAtOrDefault(i) != null && waypoints[i].DrivingFlag == VehicleDrivingFlags.StopAtDestination)
                {
                    Game.LogTrivial($"{collectedVehicle.Vehicle.Model.Name} stopping at waypoint.");
                    collectedVehicle.Vehicle.Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraightBraking);
                    collectedVehicle.SetStoppedAtWaypoint(true);
                }
            }
            Game.LogTrivial($"{vehicle.Model.Name} all tasks complete.");
            DismissDriver(collectedVehicle);
        }

        private static void DismissDriver(CollectedVehicle cv)
        {
            cv.SetDismissNow(true);
            cv.SetStoppedAtWaypoint(false);
            if (cv.Vehicle && cv.Vehicle.Driver && !cv.Redirected)
            {
                cv.Vehicle.Driver.Dismiss();
                cv.Vehicle.Driver.Tasks.Clear();
                cv.Vehicle.Driver.BlockPermanentEvents = false;
                cv.Vehicle.Driver.IsPersistent = false;

                cv.Vehicle.Dismiss();
                cv.Vehicle.IsPersistent = false;

                Game.LogTrivial($"{cv.Vehicle.Model.Name} dismissed successfully.");
            }
            else if (!cv.Vehicle)
            {
                Game.LogTrivial($"{cv.Vehicle.Model.Name} is not valid after tasks completed.");
            }
            else if (!cv.Vehicle.Driver)
            {
                Game.LogTrivial($"{cv.Vehicle.Model.Name} driver is not valid after tasks completed.");
            }
        }

        public static void AssignStopForVehiclesFlag(List<Path> paths, Path path, Waypoint waypointData)
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

        public static void DirectTask(CollectedVehicle cv, List<Waypoint> waypoints)
        {
            cv.SetDismissNow(false);
            if (cv.Vehicle && cv.Vehicle.Driver)
            {
                cv.Vehicle.IsPersistent = true;
                cv.Vehicle.Driver.IsPersistent = true;
                cv.Vehicle.Driver.BlockPermanentEvents = true;
                cv.Vehicle.Driver.Tasks.Clear();
            }

            // Give vehicle task to initial waypoint of desired path, then run a loop to keep giving the task until they're close enough in case they try to wander away too early
            // Need to figure out how to only get waypoints which are in front of or within 90 degrees of either side of the vehicle
            var nearestWaypoint = waypoints.OrderBy(wp => wp.Position).Take(1) as Waypoint;

            cv.Vehicle.Driver.Tasks.DriveToPosition(nearestWaypoint.Position, nearestWaypoint.Speed, (VehicleDrivingFlags)262539, 1f); // waypointData[0].WaypointPos
            while (nearestWaypoint != null && cv.Vehicle && cv.Vehicle.Driver && cv.Vehicle.DistanceTo(waypoints[0].Position) > 3f && !cv.DismissNow)
            {
                cv.Vehicle.Driver.Tasks.DriveToPosition(nearestWaypoint.Position, nearestWaypoint.Speed, (VehicleDrivingFlags)262539, 1f);
                GameFiber.Sleep(500);
            }
            cv.SetRedirected(false);
            Game.LogTrivial($"DirectTask loop over");
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

        private static Vehicle[] GetNearbyVehicles(Vector3 OriginPosition, float radius)
        {
            return (from x in World.GetAllVehicles() where !x.IsTrailer && x.DistanceTo(OriginPosition) < radius select x).ToArray();
        }

        // Driving styles https://gtaforums.com/topic/822314-guide-driving-styles/
        // also https://vespura.com/fivem/drivingstyle/
        [Flags]
        public enum EDrivingFlags
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

        public static void SetDriveTaskDrivingFlags(this Ped ped, EDrivingFlags flags)
        {
            ulong SetDriveTaskDrivingFlagsHash = 0xDACE1BE37D88AF67;
            Rage.Native.NativeFunction.CallByHash<int>(SetDriveTaskDrivingFlagsHash, ped, (int)flags);
        }
    }
}
