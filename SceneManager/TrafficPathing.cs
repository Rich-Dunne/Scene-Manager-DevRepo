using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Rage;

namespace SceneManager
{
    public static class TrafficPathing
    {
        public static Dictionary<string, ControlledVehicle> collectedVehicles = new Dictionary<string, ControlledVehicle>();

        public static void WaypointVehicleCollector(List<Path> paths, Path path, Waypoint waypoint)
        {
            //GameFiber AssignStopForVehiclesFlagFiber = new GameFiber(() => AssignStopForVehiclesFlag(paths, path, waypointData));
            //AssignStopForVehiclesFlagFiber.Start();

            while (paths.Contains(path) && path.Waypoint.Contains(waypoint))
            {
                if (!path.PathDisabled && waypoint.Collector)
                {
                    foreach (Vehicle v in GetNearbyVehicles(waypoint.WaypointPos, waypoint.CollectorRadius).Where(v => v.IsValidForCollection()))
                    {
                        v.IsPersistent = true;
                        v.Driver.IsPersistent = true;
                        v.Driver.BlockPermanentEvents = true;
                        v.Driver.Tasks.Clear();

                        // If the vehicle is not in the collection yet
                        if (!collectedVehicles.ContainsKey(v.LicensePlate))
                        {
                            var collectedVehicle = new ControlledVehicle(v, v.LicensePlate, path.PathNum, path.Waypoint.Count, waypoint.WaypointNum, true, false, false);
                            collectedVehicles.Add(v.LicensePlate, collectedVehicle);
                            Game.LogTrivial($"[WaypointVehicleCollector] Added {v.Model.Name} to collection from path {path.PathNum}, waypoint {waypoint.WaypointNum}.");

                            GameFiber DismissCheckFiber = new GameFiber(() => VehicleDismissed(collectedVehicle, path.Waypoint));
                            DismissCheckFiber.Start();

                            AssignTasks(collectedVehicle, path.Waypoint, waypoint);
                        }
                        // If the vehicle is in the collection, but has no tasks
                        else if (collectedVehicles.ContainsKey(v.LicensePlate) && !collectedVehicles[v.LicensePlate].TasksAssigned)
                        {
                            Game.LogTrivial($"[WaypointVehicleCollector] {v.Model.Name} already in collection, but with no tasks.  Assigning tasks.");
                            collectedVehicles[v.LicensePlate].TasksAssigned = true;

                            AssignTasks(collectedVehicles[v.LicensePlate], path.Waypoint, waypoint);
                        }
                        // If the vehicle is in the collection and has tasks
                        else
                        {
                            Game.LogTrivial($"[WaypointVehicleCollector]: {v.Model.Name} was not collected because it's already in the collection and has tasks.");
                        }
                    }
                }
                GameFiber.Sleep(100);
                //GameFiber.Yield();
            }
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

        private static void AssignTasks(ControlledVehicle collectedVehicle, List<Waypoint> waypointData, Waypoint waypoint)
        {
            if (waypointData.Count == 1)
            {
                GameFiber AssignSingleTaskFiber = new GameFiber(() => AssignSingleWaypointTask(collectedVehicle, waypointData));
                AssignSingleTaskFiber.Start();
            }
            else if (waypointData.Count > 1)
            {
                GameFiber AssignMultiTaskFiber = new GameFiber(() => AssignMultiWaypointTasks(collectedVehicle, waypointData, waypoint));
                AssignMultiTaskFiber.Start();
            }
        }

        // TODO:  Combine single and multiwaypoint tasks into one method
        private static void AssignSingleWaypointTask(ControlledVehicle cv, List<Waypoint> waypointData)
        {
            // Give driver a task to the single path waypoint.  Run a loop with a condition checking for DismissNow for cases where the driver is dismissed or redirected
            Game.LogTrivial($"Assigning task for single waypoint.");
            cv.Vehicle.Driver.Tasks.DriveToPosition(waypointData[0].WaypointPos, waypointData[0].Speed, (VehicleDrivingFlags)262539, 1f);
            //SetDriveTaskDrivingFlags(cv.Vehicle.Driver, EDrivingFlags.TotalControl);
            while (waypointData.ElementAtOrDefault(0) != null && cv.Vehicle && cv.Vehicle.Driver && cv.Vehicle.DistanceTo(waypointData[0].WaypointPos) > 3f && !cv.DismissNow)
            {
                GameFiber.Sleep(1000);
            }

            if (cv.DismissNow)
            {
                Game.LogTrivial($"{cv.Vehicle.Model.Name} dismissed while in AssignSingleWaypointTask.");
            }
            else
            {
                Game.LogTrivial($"{cv.Vehicle.Model.Name} should be stopped at the waypoint.");
                cv.Vehicle.Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraightBraking);//.WaitForCompletion();
                cv.Vehicle.Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
                cv.StoppedAtWaypoint = true;
            }
        }

        private static void AssignMultiWaypointTasks(ControlledVehicle cv, List<Waypoint> waypointData, Waypoint collectorWaypoint)
        {
            // For each waypoint in the path, give driver a task to that waypoint
            // i needs to be the index of the waypoint the vehicle was collected from
            for (int i = waypointData.IndexOf(collectorWaypoint); i < waypointData.Count; i++)
            //for (int i = 1; i < waypointData.Count; i++)
            {
                if (!cv.DismissNow)
                {
                    cv.CurrentWaypoint = waypointData[i].WaypointNum;
                    var nextWaypoint = i + 1;
                    Game.LogTrivial($"Assigning task to {cv.Vehicle.Model.Name} from waypoint {nextWaypoint} of {waypointData.Count}");

                    if (cv.Vehicle && cv.Vehicle.Driver && waypointData.ElementAtOrDefault(nextWaypoint) != null && waypointData[nextWaypoint].DrivingFlag == VehicleDrivingFlags.StopAtDestination)
                    {
                        // Give driver a task to the waypoint.  Run a loop with a condition checking for DismissNow for cases where the driver is dismissed or redirected
                        SetDriveTaskDrivingFlags(cv.Vehicle.Driver, EDrivingFlags.TotalControl);
                        if (waypointData[nextWaypoint] != null)
                        {
                            //Game.LogTrivial($"Driving to stop waypoint");
                            cv.Vehicle.Driver.Tasks.DriveToPosition(waypointData[nextWaypoint].WaypointPos, waypointData[nextWaypoint].Speed, (VehicleDrivingFlags)262539, 1f);
                        }
                        else
                        {
                            Game.LogTrivial($"i is out of bounds for assigning task");
                        }

                        while (cv.Vehicle && cv.Vehicle.Driver && waypointData.ElementAtOrDefault(nextWaypoint) != null && cv.Vehicle.DistanceTo(waypointData[nextWaypoint].WaypointPos) > 3f && !cv.DismissNow)
                        {
                            GameFiber.Sleep(100);
                        }

                        if (waypointData.ElementAtOrDefault(i) != null && !cv.DismissNow)
                        {
                            Game.LogTrivial($"{cv.Vehicle.Model.Name} stopping at stop waypoint");
                            cv.Vehicle.Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraightBraking);
                            cv.StoppedAtWaypoint = true;
                        }
                    }
                    else if (cv.Vehicle && cv.Vehicle.Driver && waypointData.ElementAtOrDefault(nextWaypoint) != null && !cv.DismissNow)
                    {
                        cv.Vehicle.Driver.Tasks.DriveToPosition(waypointData[nextWaypoint].WaypointPos, waypointData[nextWaypoint].Speed, (VehicleDrivingFlags)262539, 1f).WaitForCompletion();
                    }
                    Game.LogTrivial($"{cv.Vehicle.Model.Name} waypoint {nextWaypoint} task complete");
                }
                else
                {
                    Game.LogTrivial($"{cv.Vehicle.Model.Name} was dismissed while in AssignMultiWaypointTasks");
                    break;
                }
            }

            if (cv.Redirected)
            {
                Game.LogTrivial($"{cv.Vehicle.Model.Name} was redirected, all old path tasks have been cleared.");
            }
            else
            {
                Game.LogTrivial($"{cv.Vehicle.Model.Name} all path {cv.Path} tasks complete.");
            }

            DismissDriver(cv);
        }

        private static void DismissDriver(ControlledVehicle cv)
        {
            cv.DismissNow = true;
            cv.StoppedAtWaypoint = false;
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
            while (paths.Contains(path) && path.Waypoint.Contains(waypointData))
            {
                if (!path.PathDisabled)
                {
                    foreach (Vehicle v in GetNearbyVehicles(waypointData.WaypointPos, 50f).Where(v => v && v.HasDriver && v.Driver))
                    {
                        SetDriveTaskDrivingFlags(v.Driver, EDrivingFlags.StopForVehicles);
                    }
                }
                GameFiber.Sleep(500);
            }
        }

        public static void DirectTask(ControlledVehicle cv, List<Waypoint> waypointData)
        {
            cv.DismissNow = false;
            if (cv.Vehicle && cv.Vehicle.Driver)
            {
                cv.Vehicle.IsPersistent = true;
                cv.Vehicle.Driver.IsPersistent = true;
                cv.Vehicle.Driver.BlockPermanentEvents = true;
                cv.Vehicle.Driver.Tasks.Clear();
            }

            // Give vehicle task to initial waypoint of desired path, then run a loop to keep giving the task until they're close enough in case they try to wander away too early
            // Need to figure out how to only get waypoints which are in front of or within 90 degrees of either side of the vehicle
            var nearestWaypoint = waypointData.OrderBy(wp => wp.WaypointPos).Take(1) as Waypoint;

            cv.Vehicle.Driver.Tasks.DriveToPosition(nearestWaypoint.WaypointPos, nearestWaypoint.Speed, (VehicleDrivingFlags)262539, 1f); // waypointData[0].WaypointPos
            while (waypointData.ElementAtOrDefault(0) != null && cv.Vehicle && cv.Vehicle.Driver && cv.Vehicle.DistanceTo(waypointData[0].WaypointPos) > 3f && !cv.DismissNow)
            {
                cv.Vehicle.Driver.Tasks.DriveToPosition(nearestWaypoint.WaypointPos, nearestWaypoint.Speed, (VehicleDrivingFlags)262539, 1f);
                GameFiber.Sleep(500);
            }
            cv.Redirected = false;
            Game.LogTrivial($"DirectTask loop over");
        }

        private static void VehicleDismissed(ControlledVehicle cv, List<Waypoint> waypointData)
        {
            while (!cv.DismissNow)
            {
                GameFiber.Sleep(500);
            }
            Game.LogTrivial($"{cv.Vehicle.Model.Name} was dismissed (dismissal check loop).");


            Game.LogTrivial($"Looping to ensure the vehicle is far enough away from all attractor waypoints so it can be removed from the collection.");
            while (true)
            {
                var collectorWaypoints = waypointData.Where(wp => wp.Collector);
                var vehicleFarEnoughAwayFromCollectors = collectorWaypoints.All(wp => cv.Vehicle.DistanceTo(wp.WaypointPos) > wp.CollectorRadius);

                if (collectedVehicles.ContainsKey(cv.LicensePlate) && vehicleFarEnoughAwayFromCollectors)
                {
                    Game.LogTrivial($"{cv.Vehicle.Model.Name} is far enough away from all attractor waypoints and has been removed from the collection.");
                    cv.TasksAssigned = false;
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
            Normal = StopForVehicles | StopForPeds | AvoidEmptyVehicles | StopForTrafficLights | UseBlinkers,
            TotalControl = AllowWrongWay | AvoidObjects | AvoidPeds | TakeShortestPath | StopForTrafficLights | IgnorePathfinding | StopForVehicles
        }

        public static void SetDriveTaskDrivingFlags(this Ped ped, EDrivingFlags flags)
        {
            ulong SetDriveTaskDrivingFlagsHash = 0xDACE1BE37D88AF67;
            Rage.Native.NativeFunction.CallByHash<int>(SetDriveTaskDrivingFlagsHash, ped, (int)flags);
        }
    }
}
