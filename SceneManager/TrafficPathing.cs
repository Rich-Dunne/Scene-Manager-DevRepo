using System;
using System.Collections.Generic;
using System.Linq;
using Rage;

namespace SceneManager
{
    public static class TrafficPathing
    {
        public static Dictionary<string, ControlledVehicle> ControlledVehicles = new Dictionary<string, ControlledVehicle>();
        //public static List<ControlledVehicle> ControlledVehicles = new List<ControlledVehicle> { };

        public static void InitialWaypointVehicleCollector(List<WaypointData> waypointData)
        {
            waypointData[0].YieldZone = World.AddSpeedZone(waypointData[0].WaypointPos, 10f, waypointData[0].Speed);

            // If there's a path with a single stop waypoint, run a loop to give all nearby AI the StopForVehicle driving flag so they don't just go around
            if(waypointData.Count == 1)
            {
                GameFiber.StartNew(delegate
                {
                    while (waypointData.ElementAtOrDefault(0) != null)
                    {
                        foreach (Vehicle v in GetNearbyVehicles(waypointData[0].WaypointPos, 50f))
                        {
                            if(v.Exists() && v.IsValid() && v.HasDriver && v.Driver.Exists() && v.Driver.IsValid())
                            {
                                SetDriveTaskDrivingFlags(v.Driver, EDrivingFlags.StopForVehicles);
                            }
                        }
                        GameFiber.Yield();
                    }
                });
            }

            while (waypointData.ElementAtOrDefault(0) != null)
            {
                //Game.DisplaySubtitle($"Vehicles in collection: {ControlledVehicles.Count()}");
                // Getting vehicles within 3f of waypoint
                try
                {
                    foreach (Vehicle v in GetNearbyVehicles(waypointData[0].WaypointPos, 3f))
                    {
                        // No protection for player if they drive into the waypoints
                        if(VehicleAndDriverValid(v) && v != Game.LocalPlayer.Character.CurrentVehicle && v.HasDriver && v.Driver.IsAlive && (v.IsCar || v.IsBike || v.IsBicycle || v.IsQuadBike))
                        {
                            // Check if there's an object in the list with a matching vehicle
                            var matchingVehicle = ControlledVehicles.Where(cv => cv.Value.Vehicle == v).ToList();
                            // If there's a match, then check if the first match has tasksAssigned.  If not, AssignTasks
                            if (matchingVehicle.ElementAtOrDefault(0).Value != null && !matchingVehicle[0].Value.TasksAssigned && !matchingVehicle[0].Value.DismissNow)
                            {
                                Game.LogTrivial($"[InitialWaypointVehicleCollector] {v.Model.Name} already in collection, but with no tasks.  Assigning tasks.");
                                matchingVehicle[0].Value.TasksAssigned = true;
                                GameFiber AssignTasksFiber = new GameFiber(() => AssignTasks(matchingVehicle[0].Value, waypointData));
                                AssignTasksFiber.Start();
                            }
                            // Else if object doesn't exist, add to collection and AssignTasks
                            else if (matchingVehicle.ElementAtOrDefault(0).Value != null && matchingVehicle[0].Value.TasksAssigned)
                            {
                                //Game.LogTrivial($"Vehicle already in collection with tasks.  Do nothing.");
                            }
                            else
                            {
                                ControlledVehicles.Add(v.LicensePlate, new ControlledVehicle(v, v.LicensePlate, waypointData[0].Path, waypointData.Count, 1, true, false, false));
                                Game.LogTrivial($"Added {v.Model.Name} to collection from initial waypoint at path {waypointData[0].Path} with {waypointData.Count} waypoints");

                                GameFiber AssignTasksFiber = new GameFiber(() => AssignTasks(ControlledVehicles[v.LicensePlate], waypointData));
                                AssignTasksFiber.Start();
                            }
                        }
                        GameFiber.Yield();
                    }
                }
                catch
                {
                    Game.LogTrivial($"There was a problem getting vehicles near the start waypoint");
                }
                
                GameFiber.Yield();
            }
        }

        public static void DirectTask(ControlledVehicle cv, List<WaypointData> waypointData)
        {
            cv.DismissNow = false;
            if (VehicleAndDriverValid(cv))
            {
                cv.Vehicle.IsPersistent = true;
                cv.Vehicle.Driver.BlockPermanentEvents = true;
                cv.Vehicle.Driver.Tasks.Clear();
            }

            // Give vehicle task to initial waypoint of desired path, then run a loop to keep giving the task until they're close enough in case they try to wander away too early
            cv.Vehicle.Driver.Tasks.DriveToPosition(waypointData[0].WaypointPos, waypointData[0].Speed, VehicleDrivingFlags.FollowTraffic, 1f);
            while (waypointData.ElementAtOrDefault(0) != null && VehicleAndDriverValid(cv) && cv.Vehicle.DistanceTo(waypointData[0].WaypointPos) > 3f && !cv.DismissNow)
            {
                cv.Vehicle.Driver.Tasks.DriveToPosition(waypointData[0].WaypointPos, waypointData[0].Speed, VehicleDrivingFlags.FollowTraffic, 1f);
                GameFiber.Sleep(500);
            }
            cv.Redirected = false;
            Game.LogTrivial($"DirectTask loop over");
        }

        private static void AssignTasks(ControlledVehicle cv, List<WaypointData> waypointData)
        {
            if (VehicleAndDriverValid(cv))
            {
                cv.Vehicle.IsPersistent = true;
                cv.Vehicle.Driver.BlockPermanentEvents = true;
                cv.Vehicle.Driver.Tasks.Clear();
            }

            if (waypointData.Count == 1 && VehicleAndDriverValid(cv) && !cv.DismissNow)
            {
                AssignSingleWaypointTask(cv, waypointData);
                
            }
            else if(waypointData.Count > 1 && VehicleAndDriverValid(cv))
            {
                AssignMultiWaypointTasks(cv, waypointData);
            }
            while(!cv.DismissNow || cv.StoppedAtWaypoint)
            {
                GameFiber.Yield();
            }
            ControlledVehicles.Remove(cv.LicensePlate);
            Game.LogTrivial($"AssignTasks exit");
        }

        private static void AssignSingleWaypointTask(ControlledVehicle cv, List<WaypointData> waypointData)
        {
            // Give driver a task to the single path waypoint.  Run a loop with a condition checking for DismissNow for cases where the driver is dismissed or redirected
            Game.LogTrivial($"Assigning task for single waypoint.");
            cv.Vehicle.Driver.Tasks.DriveToPosition(waypointData[0].WaypointPos, waypointData[0].Speed, VehicleDrivingFlags.FollowTraffic, 1f);
            while (waypointData.ElementAtOrDefault(0) != null && VehicleAndDriverValid(cv) && cv.Vehicle.DistanceTo(waypointData[0].WaypointPos) > 3f && !cv.DismissNow)
            {
                GameFiber.Sleep(1000);
            }
            Game.LogTrivial($"{cv.Vehicle.Model.Name} should be stopped at the waypoint.");
            cv.Vehicle.Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraightBraking);//.WaitForCompletion();
            cv.Vehicle.Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
            cv.StoppedAtWaypoint = true;
        }

        private static void AssignMultiWaypointTasks(ControlledVehicle cv, List<WaypointData> waypointData)
        {
            // For each waypoint in the path, give driver a task to that waypoint
            for (int i = 1; i < waypointData.Count; i++)
            {
                if (cv.DismissNow)
                {
                    break;
                }
                Game.LogTrivial($"Assigning task to {cv.Vehicle.Model.Name} for waypoint {i+1} of {waypointData.Count}");
                cv.CurrentWaypoint++;
                //if (VehicleAndDriverValid(cv) && waypointData.ElementAtOrDefault(i) != null && i == waypointData.IndexOf(waypointData.Last()) && waypointData.Last().DrivingFlag == VehicleDrivingFlags.StopAtDestination)
                if (VehicleAndDriverValid(cv) && waypointData.ElementAtOrDefault(i) != null && waypointData[i].DrivingFlag == VehicleDrivingFlags.StopAtDestination) // NEW
                {
                    // Give driver a task to the waypoint.  Run a loop with a condition checking for DismissNow for cases where the driver is dismissed or redirected
                    cv.Vehicle.Driver.Tasks.DriveToPosition(waypointData[i].WaypointPos, waypointData[i].Speed, VehicleDrivingFlags.FollowTraffic, 1f);
                    while (waypointData.ElementAtOrDefault(i) != null && cv.Vehicle.DistanceTo(waypointData[i].WaypointPos) > 3f && !cv.DismissNow)
                    {
                        GameFiber.Yield();
                    }
                    if (cv.DismissNow)
                    {
                        break;
                    }
                    if (waypointData.ElementAtOrDefault(i) != null)
                    {
                        Game.LogTrivial($"{cv.Vehicle.Model.Name} stopping at stop waypoint");
                        //Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(v, 3f, 1, false);
                        cv.Vehicle.Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraightBraking);
                        cv.StoppedAtWaypoint = true;
                        while(waypointData.ElementAtOrDefault(i) != null && cv.StoppedAtWaypoint && !cv.DismissNow)
                        {
                            GameFiber.Yield();
                        }
                    }
                }
                else if (VehicleAndDriverValid(cv) && waypointData.ElementAtOrDefault(i) != null && !cv.DismissNow)
                {
                    cv.Vehicle.Driver.Tasks.DriveToPosition(waypointData[i].WaypointPos, waypointData[i].Speed, VehicleDrivingFlags.FollowTraffic, 1f).WaitForCompletion();
                }
                Game.LogTrivial($"{cv.Vehicle.Model.Name} waypoint {i+1} task complete");
            }
            if (cv.Redirected)
            {
                Game.LogTrivial($"{cv.Vehicle.Model.Name} was redirected, all old path tasks have been cleared.");
            }
            else
            {
                Game.LogTrivial($"{cv.Vehicle.Model.Name} all path {cv.Path} tasks complete.");
            }

            cv.TasksAssigned = false;
            cv.DismissNow = true;
            cv.StoppedAtWaypoint = false;
            if (VehicleAndDriverValid(cv) && !cv.Redirected)
            {
                cv.Vehicle.Driver.Dismiss();
                cv.Vehicle.Driver.Tasks.Clear();
                //cv.Vehicle.IsPersistent = false;
                cv.Vehicle.Driver.BlockPermanentEvents = false;
            }
            else if(!VehicleAndDriverValid(cv))
            {
                Game.LogTrivial($"The vehicle is not valid after tasks completed.");
            }
        }

        private static bool VehicleAndDriverValid(Vehicle v)
        {
            // Ensure everything is valid before we do stuff with them so there isn't a crash
            if (v.Exists() && v.IsValid() && v.Driver.Exists() && v.Driver.IsValid())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool VehicleAndDriverValid(ControlledVehicle cv)
        {
            // Ensure everything is valid before we do stuff with them so there isn't a crash
            if(cv.Vehicle.Exists() && cv.Vehicle.IsValid() && cv.Vehicle.Driver.Exists() && cv.Vehicle.Driver.IsValid())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static Vehicle[] GetNearbyVehicles(Vector3 OriginPosition, float radius)
        {
            return (from x in World.GetAllVehicles() where !x.IsTrailer && x.DistanceTo(OriginPosition) < radius select x).ToArray();
        }

        // Driving styles https://gtaforums.com/topic/822314-guide-driving-styles/
        [Flags]
        public enum EDrivingFlags
        {
            None = 0,
            StopForVehicles = 1,
            StopForPeds = 2,
            AvoidEmptyVehicles = 8,
            StopForTrafficLights = 128,
            UseBlinkers = 256,
            TakeShortestPath = 262144,
            IgnoreRoads = 4194304,
            AvoidHighways = 536870912,
            Normal = StopForVehicles | StopForPeds | AvoidEmptyVehicles | StopForTrafficLights | UseBlinkers,
            //TotalControl = AllowMedianCrossing | AllowWrongWay | DriveAroundObjects | DriveAroundPeds | DriveBySight | FollowTraffic | IgnorePathfinding
        }

        public static void SetDriveTaskDrivingFlags(this Ped ped, EDrivingFlags flags)
        {
            ulong SetDriveTaskDrivingFlagsHash = 0xDACE1BE37D88AF67;
            Rage.Native.NativeFunction.CallByHash<int>(SetDriveTaskDrivingFlagsHash, ped, (int)flags);
        }
    }
}

/*
                    //while (waypointData.ElementAtOrDefault(0) != null && v.Exists() && v.IsValid() && v.Driver.Exists() && v.Driver.IsValid() && !cv.DismissNow)
                //{
                    //if (waypointData.ElementAtOrDefault(0) == null || cv.DismissNow)
                    //{
                        //driver.Tasks.Clear();
                        //Game.LogTrivial($"{v.Model.Name} released from wait loop");
                        //break;
                    //}
                    //else if (waypointData.ElementAtOrDefault(0) != null && !cv.DismissNow && v.Driver.IsInVehicle(v, false))
                    //{
                        v.Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraightBraking);
                        //v.Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
                    //}
                    //GameFiber.Yield();
                //}
                //v.Driver.Tasks.Clear();
                //Game.LogTrivial($"{v.Model.Name} is dismissed or the driver is not in the vehicle.");
*/
