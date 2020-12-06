using Rage;
using System.Collections.Generic;
using System.Linq;
using SceneManager.Utils;

namespace SceneManager.Objects
{
    internal class CollectedVehicle
    {
        internal Ped Driver { get; private set; }
        internal Vehicle Vehicle { get; private set; }
        internal Path Path { get; private set; }
        internal Waypoint CurrentWaypoint { get; private set; }
        internal Waypoint NextWaypoint { get; private set; }
        internal bool StoppedAtWaypoint { get; private set; } = false;
        internal bool Dismissed { get; private set; } = false;
        internal bool Directed { get; set; } = false;
        internal bool SkipWaypoint { get; private set; } = false;
        internal bool ReadyForDirectTasks { get; private set; } = true;

        internal CollectedVehicle(Vehicle vehicle, Path path, Waypoint currentWaypoint)
        {
            Vehicle = vehicle;
            Driver = Vehicle.Driver;
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

        private void SetPersistence()
        {
            Vehicle.IsPersistent = true;
            Driver.IsPersistent = true;
            Driver.BlockPermanentEvents = true;
        }

        internal void AssignWaypointTasks(Path path, Waypoint currentWaypoint)
        {
            // Driving styles https://gtaforums.com/topic/822314-guide-driving-styles/
            // also https://vespura.com/fivem/drivingstyle/

            if (!VehicleAndDriverAreValid())
            {
                return;
            }

            AssignPathAndCurrentWaypoint();

            AssignDirectedTask();

            if (currentWaypoint.IsStopWaypoint)
            {
                StopAtWaypoint();
            }
            if (path?.Waypoints?.Count > 0 && currentWaypoint != path?.Waypoints?.Last())
            {
                DriveToNextWaypoint();
            }

            if (!Dismissed && !VehicleAndDriverAreValid() || Directed)
            {
                return;
            }

            Game.LogTrivial($"{Vehicle.Model.Name} [{Vehicle.Handle}] all Path {path.Number} tasks complete.");
            if (!Dismissed)
            {
                Dismiss();
            }

            void AssignPathAndCurrentWaypoint()
            {
                Path = path;
                if (currentWaypoint != null)
                {
                    CurrentWaypoint = currentWaypoint;
                }
                else
                {
                    CurrentWaypoint = path.Waypoints[0];
                }
            }

            void AssignDirectedTask()
            {
                if (currentWaypoint != null && Directed)
                {
                    Dismissed = false;

                    while (!ReadyForDirectTasks)
                    {
                        GameFiber.Yield();
                    }
                    if (!VehicleAndDriverAreValid())
                    {
                        return;
                    }
                    Driver.Tasks.Clear();
                    DriveToDirectedWaypoint();
                }
            }

            void DriveToDirectedWaypoint()
            {
                Dismissed = false;

                while (!ReadyForDirectTasks)
                {
                    GameFiber.Yield();
                }
                Driver.Tasks.Clear();
                AssignTasksForDirectedDriver();

                void AssignTasksForDirectedDriver()
                {
                    float acceptedDistance = GetAcceptedStoppingDistance(Path.Waypoints, Path.Waypoints.IndexOf(currentWaypoint));
                    Vector3 oldPosition = currentWaypoint.Position;
                    Game.LogTrivial($"{Vehicle.Model.Name} [{Vehicle.Handle}] is driving to path {currentWaypoint.Path.Number} waypoint {currentWaypoint.Number} (directed)");
                    Driver.Tasks.DriveToPosition(currentWaypoint.Position, currentWaypoint.Speed, (VehicleDrivingFlags)currentWaypoint.DrivingFlagType, acceptedDistance);
                    LoopWhileDrivingToDirectedWaypoint();

                    void LoopWhileDrivingToDirectedWaypoint()
                    {
                        while (VehicleAndDriverAreValid() && !Dismissed && !SkipWaypoint && Vehicle.FrontPosition.DistanceTo2D(oldPosition) > acceptedDistance)
                        {
                            if (oldPosition != currentWaypoint.Position)
                            {
                                oldPosition = currentWaypoint.Position;
                            }
                            GameFiber.Yield();
                        }
                        if (Vehicle)
                        {
                            Driver.Tasks.PerformDrivingManeuver(Vehicle, VehicleManeuver.GoForwardWithCustomSteeringAngle, 3).WaitForCompletion();
                            Game.LogTrivial($"{Vehicle.Model.Name} [{Vehicle.Handle}] directed task is complete, directed is now false");
                        }
                        Directed = false;
                    }
                }
            }

            void DriveToNextWaypoint()
            {
                if (!VehicleAndDriverAreValid() || CurrentWaypoint == null || Path == null)
                {
                    Game.LogTrivial($"Vehicle, driver, waypoint, or path is null.");
                    return;
                }

                Game.LogTrivial($"Preparing to run task loop for {Vehicle.Model.Name} [{Vehicle.Handle}] on path {Path.Number}");
                for (int currentWaypointTask = CurrentWaypoint.Number; currentWaypointTask < Path.Waypoints.Count; currentWaypointTask++)
                {
                    var oldPosition = Path.Waypoints[currentWaypointTask].Position;
                    SkipWaypoint = false;

                    if (this == null || !Vehicle || Dismissed || Directed)
                    {
                        Game.LogTrivial($"Vehicle dismissed, directed, or null, return");
                        return;
                    }
                    if (Driver == null || !Driver || !Vehicle.HasDriver || !Driver.IsAlive || Vehicle.Driver == Game.LocalPlayer.Character)
                    {
                        Game.LogTrivial($"{Vehicle.Model.Name} [{Vehicle.Handle}] does not have a driver/driver is null or driver is dead.");
                        return;
                    }

                    if (Path.Waypoints.ElementAtOrDefault(currentWaypointTask) != null && !StoppedAtWaypoint)
                    {
                        CurrentWaypoint = Path.Waypoints[currentWaypointTask];
                        float acceptedDistance = GetAcceptedStoppingDistance(Path.Waypoints, currentWaypointTask);

                        Game.LogTrivial($"{Vehicle.Model.Name} [{Vehicle.Handle}] is driving to path {Path.Number} waypoint {Path.Waypoints[currentWaypointTask].Number} (Stop: {CurrentWaypoint.IsStopWaypoint}, Driving flag: {CurrentWaypoint.DrivingFlagType})");
                        Driver.Tasks.DriveToPosition(Path.Waypoints[currentWaypointTask].Position, Path.Waypoints[currentWaypointTask].Speed, (VehicleDrivingFlags)Path.Waypoints[currentWaypointTask].DrivingFlagType, acceptedDistance);
                        LoopWhileDrivingToWaypoint(currentWaypointTask, acceptedDistance, oldPosition);

                        if (!VehicleAndDriverAreValid())
                        {
                            return;
                        }

                        if (SkipWaypoint)
                        {
                            SkipWaypoint = false;
                            continue;
                        }

                        if (!Dismissed && !Directed && Path.Waypoints.ElementAtOrDefault(currentWaypointTask) != null && Path.Waypoints[currentWaypointTask].IsStopWaypoint)
                        {
                            StopAtWaypoint();
                        }

                        if (!VehicleAndDriverAreValid() || Dismissed || Directed)
                        {
                            return;
                        }

                        Driver.Tasks.PerformDrivingManeuver(Vehicle, VehicleManeuver.GoForwardWithCustomSteeringAngle, 3).WaitForCompletion();
                    }
                }

                void LoopWhileDrivingToWaypoint(int currentWaypointTask, float acceptedDistance, Vector3 oldPosition)
                {
                    while (VehicleAndDriverAreValid() && !Dismissed && !SkipWaypoint && !Directed && Path.Waypoints.ElementAtOrDefault(currentWaypointTask) != null && Vehicle.FrontPosition.DistanceTo2D(Path.Waypoints[currentWaypointTask].Position) > acceptedDistance)
                    {
                        if (oldPosition != Path.Waypoints[currentWaypointTask].Position)
                        {
                            Game.LogTrivial($"Waypoint position has changed, updating drive task.");
                            oldPosition = Path.Waypoints[currentWaypointTask].Position;
                            Driver.Tasks.DriveToPosition(Path.Waypoints[currentWaypointTask].Position, Path.Waypoints[currentWaypointTask].Speed, (VehicleDrivingFlags)Path.Waypoints[currentWaypointTask].DrivingFlagType, acceptedDistance);
                        }
                        if(Driver.Tasks.CurrentTaskStatus == TaskStatus.NoTask)
                        {
                            //Game.DisplayNotification($"~o~Scene Manager ~r~[Error]\n{Vehicle.Model.Name} [{Vehicle.Handle}] driver [{Driver.Handle}] has no task.  Reassiging current waypoint task.");
                            Game.LogTrivial($"{Vehicle.Model.Name} [{Vehicle.Handle}] driver [{Driver.Handle}] has no task.  Reassiging current waypoint task.");
                            if (Driver.CurrentVehicle)
                            {
                                Driver.Tasks.DriveToPosition(Path.Waypoints[currentWaypointTask].Position, Path.Waypoints[currentWaypointTask].Speed, (VehicleDrivingFlags)Path.Waypoints[currentWaypointTask].DrivingFlagType, acceptedDistance);
                            }
                            else
                            {
                                Game.LogTrivial($"{Vehicle.Model.Name} [{Vehicle.Handle}] driver [{Driver.Handle}] is not in a vehicle.  Exiting loop.");
                                return;
                            }
                        }
                        GameFiber.Sleep(100);
                    }
                }
            }

            float GetAcceptedStoppingDistance(List<Waypoint> waypoints, int nextWaypoint)
            {
                float dist;
                if (Settings.SpeedUnit == SpeedUnits.MPH)
                {
                    dist = (MathHelper.ConvertMilesPerHourToKilometersPerHour(waypoints[nextWaypoint].Speed) * MathHelper.ConvertMilesPerHourToKilometersPerHour(waypoints[nextWaypoint].Speed)) / (250 * 0.8f);
                }
                else
                {
                    dist = (waypoints[nextWaypoint].Speed * waypoints[nextWaypoint].Speed) / (250 * 0.8f);
                }
                var acceptedDistance = MathHelper.Clamp(dist, 2, 10);
                return acceptedDistance;
            }

            void StopAtWaypoint()
            {
                var stoppingDistance = GetAcceptedStoppingDistance(currentWaypoint.Path.Waypoints, currentWaypoint.Path.Waypoints.IndexOf(currentWaypoint));
                Game.LogTrivial($"{Vehicle.Model.Name} stopping at path {currentWaypoint.Path.Number} waypoint.");
                Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(Vehicle, stoppingDistance, -1, true);
                StoppedAtWaypoint = true;

                while (currentWaypoint != null && VehicleAndDriverAreValid() && StoppedAtWaypoint && !Directed)
                {
                    GameFiber.Yield();
                }
                if (Driver && Driver.CurrentVehicle)
                {
                    Game.LogTrivial($"{Vehicle.Model.Name} releasing from stop waypoint.");
                    Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(Vehicle, 0f, 1, true);
                    Driver.Tasks.CruiseWithVehicle(5f);
                }
            }

            bool VehicleAndDriverAreValid()
            {
                if (this == null)
                {
                    Game.LogTrivial($"CollectedVehicle is null");
                    return false;
                }
                if (!Vehicle)// && !Dismissed)
                {
                    Game.LogTrivial($"Vehicle is null");
                    Dismiss();
                    return false;
                }
                if (!Driver || !Driver.CurrentVehicle || !Driver.IsAlive)
                {
                    Game.LogTrivial($"Driver is null or dead or not in a vehicle");
                    Dismiss();
                    return false;
                }
                return true;
            }
        }

        internal void Dismiss(DismissOption dismissOption = DismissOption.FromPath, Path newPath = null)
        {
            if (!Vehicle)
            {
                Game.LogTrivial($"Vehicle is null.");
                return;
            }
            if (!Driver)
            {
                Game.LogTrivial($"Driver is null.");
                return;
            }

            if (dismissOption == DismissOption.FromWorld)
            {
                DismissFromWorld();
                return;
            }

            if (dismissOption == DismissOption.FromPlayer)
            {
                Dismissed = true;
                //if (Driver)
                //{
                    Driver.Dismiss();
                //}
                //if (Vehicle)
                //{
                    Vehicle.Dismiss();
                    Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(Vehicle, 0f, 1, true);
                //}
                Path.CollectedVehicles.Remove(this);
                return;
            }

            if(Driver.CurrentVehicle && StoppedAtWaypoint)
            {
                StoppedAtWaypoint = false;
                Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(Driver.LastVehicle, 0f, 1, true);
                //if (Driver)
                //{
                    Driver.Tasks.CruiseWithVehicle(5f);
                //}
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

            if(dismissOption == DismissOption.FromDirected)
            {
                DismissFromDirect();
            }

            void DismissFromWorld()
            {
                Game.LogTrivial($"Dismissed {Vehicle.Model.Name} [{Vehicle.Handle}] from the world");
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
                    Game.LogTrivial($"CurrentWaypoint or Path is null");
                    return;
                }
                
                if (CurrentWaypoint?.Number != Path?.Waypoints.Count)
                {
                    Game.LogTrivial($"{Vehicle?.Model.Name} [{Vehicle?.Handle}] dismissed from waypoint.");
                    SkipWaypoint = true;
                }
                else if (CurrentWaypoint?.Number == Path?.Waypoints.Count)
                {
                    DismissFromPath();
                }
            }

            void DismissFromPath()
            {
                Game.LogTrivial($"Dismissing {Vehicle?.Model.Name} [{Vehicle?.Handle}] from path");
                Dismissed = true;

                // Check if the vehicle is near any of the path's collector waypoints
                GameFiber.StartNew(() =>
                {
                    var nearestCollectorWaypoint = Path.Waypoints.Where(wp => wp.IsCollector).OrderBy(wp => Vehicle.DistanceTo2D(wp.Position)).FirstOrDefault();
                    if(nearestCollectorWaypoint == null)
                    {
                        Game.LogTrivial($"Nearest collector is null");
                    }
                    else
                    {
                        while (nearestCollectorWaypoint != null && Vehicle && Vehicle.HasDriver && Driver && Driver.IsAlive && Vehicle.FrontPosition.DistanceTo2D(nearestCollectorWaypoint.Position) <= nearestCollectorWaypoint.CollectorRadius)
                        {
                            GameFiber.Yield();
                        }
                    }

                    if (!Vehicle || !Driver)
                    {
                        Game.LogTrivial($"Vehicle or driver is null");
                        return;
                    }

                    if (!Directed)
                    {
                        Path.CollectedVehicles.Remove(this);
                        Game.LogTrivial($"{Vehicle.Model.Name} [{Vehicle.Handle}] dismissed successfully.");
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
                            Vehicle.Dismiss();
                            Vehicle.IsSirenOn = false;
                            Vehicle.IsSirenSilent = true;
                        }
                    }
                }, "DismissFromPath Fiber");
                
            }

            void DismissFromDirect()
            {
                Dismissed = true;
                Directed = true;
                if (newPath != null)
                {
                    newPath.CollectedVehicles.Add(this);
                    Path.CollectedVehicles.Remove(this);
                }
                Driver.Tasks.Clear();
            }
        }
    }
}
