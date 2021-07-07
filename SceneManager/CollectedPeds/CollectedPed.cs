using Rage;
using System.Collections.Generic;
using System.Linq;
using SceneManager.Utils;
using SceneManager.Waypoints;
using SceneManager.Paths;

namespace SceneManager.CollectedPeds
{
    internal class CollectedPed : Ped
    {
        internal Path Path { get; set; }
        internal Waypoint CurrentWaypoint { get; set; }
        internal bool StoppedAtWaypoint { get; private set; } = false;
        internal bool Dismissed { get; private set; } = false;
        internal bool Directed { get; set; } = false;
        internal bool SkipWaypoint { get; private set; } = false;
        internal bool ReadyForDirectTasks { get; private set; } = true;

        internal CollectedPed(Ped basePed, Path path, Waypoint waypoint) : base(basePed.Handle) 
        {
            Handle = basePed.Handle;
            Path = path;
            CurrentWaypoint = waypoint;
            SetPersistence();
            Game.LogTrivial($"Added {CurrentVehicle.Model.Name} to collection from path \"{Path.Name}\" waypoint {CurrentWaypoint.Number}.");

            GameFiber.StartNew(() => AssignWaypointTasks(), "Task Assignment Fiber");
        }

        private void SetPersistence()
        {
            IsPersistent = true;
            BlockPermanentEvents = true;
            CurrentVehicle.IsPersistent = true;
        }

        private void AssignWaypointTasks()
        {
            // Driving styles https://gtaforums.com/topic/822314-guide-driving-styles/
            // also https://vespura.com/fivem/drivingstyle/

            if (!VehicleAndDriverAreValid())
            {
                return;
            }

            AssignDirectedTask(); // This logic is a mess.

            if (CurrentWaypoint.IsStopWaypoint)
            {
                StopAtWaypoint();
            }
            if (Path?.Waypoints?.Count > 0 && CurrentWaypoint != Path?.Waypoints?.Last())
            {
                DriveToNextWaypoint();
            }

            if (Path.State == State.Deleting || !VehicleAndDriverAreValid())
            {
                Dismiss();
                return;
            }

            if(Directed)
            {
                Dismiss(Utils.Dismiss.FromDirected);
            }

            Game.LogTrivial($"{CurrentVehicle.Model.Name} [{CurrentVehicle.Handle}] all path \"{Path.Name}\" tasks complete.");
            Dismiss();
        }

        private void AssignDirectedTask()
        {
            if (CurrentWaypoint != null && Directed)
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
                Tasks.Clear();
                DriveToDirectedWaypoint();
            }
        }

        private void DriveToDirectedWaypoint()
        {
            Dismissed = false;

            while (!ReadyForDirectTasks)
            {
                GameFiber.Yield();
            }
            Tasks.Clear();
            AssignTasksForDirectedDriver();
        }

        private void AssignTasksForDirectedDriver()
        {
            float acceptedDistance = GetAcceptedStoppingDistance(Path.Waypoints, Path.Waypoints.IndexOf(CurrentWaypoint));
            Vector3 oldPosition = CurrentWaypoint.Position;
            Game.LogTrivial($"{CurrentVehicle.Model.Name} [{CurrentVehicle.Handle}] is driving to path \"{Path.Name}\" waypoint {CurrentWaypoint.Number} (directed)");
            Tasks.DriveToPosition(CurrentWaypoint.Position, CurrentWaypoint.Speed, (VehicleDrivingFlags)CurrentWaypoint.DrivingFlagType, acceptedDistance);
            LoopWhileDrivingToDirectedWaypoint();

            void LoopWhileDrivingToDirectedWaypoint()
            {
                while (VehicleAndDriverAreValid() && !Dismissed && !SkipWaypoint && CurrentVehicle.FrontPosition.DistanceTo2D(oldPosition) > acceptedDistance)
                {
                    if (oldPosition != CurrentWaypoint.Position)
                    {
                        oldPosition = CurrentWaypoint.Position;
                    }
                    GameFiber.Yield();
                }

                if(!VehicleAndDriverAreValid() || Path.State == State.Deleting)
                {
                    return;
                }

                if (CurrentVehicle)
                {
                    Tasks.PerformDrivingManeuver(CurrentVehicle, VehicleManeuver.GoForwardWithCustomSteeringAngle, 3).WaitForCompletion();
                    Game.LogTrivial($"{CurrentVehicle.Model.Name} [{CurrentVehicle.Handle}] directed task is complete, directed is now false");
                }
                Directed = false;
            }
        }

        private float GetAcceptedStoppingDistance(List<Waypoint> waypoints, int nextWaypoint)
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

        private void DriveToNextWaypoint()
        {
            if (!VehicleAndDriverAreValid() || CurrentWaypoint == null || Path == null)
            {
                Game.LogTrivial($"Vehicle, driver, waypoint, or path is null.");
                return;
            }

            Game.LogTrivial($"Preparing to run task loop for {CurrentVehicle.Model.Name} [{CurrentVehicle.Handle}] on path \"{Path.Name}\"");
            for (int currentWaypointTask = CurrentWaypoint.Number; currentWaypointTask < Path.Waypoints.Count; currentWaypointTask++)
            {
                var oldPosition = Path.Waypoints[currentWaypointTask].Position;
                SkipWaypoint = false;

                if (this == null || !this || !CurrentVehicle || Dismissed || Directed)
                {
                    Game.LogTrivial($"Vehicle dismissed, directed, or null, return");
                    return;
                }
                if (this == null || !this || !LastVehicle.HasDriver || !IsAlive || LastVehicle.Driver == Game.LocalPlayer.Character)
                {
                    Game.LogTrivial($"{CurrentVehicle.Model.Name} [{CurrentVehicle.Handle}] does not have a driver/driver is null or driver is dead.");
                    return;
                }

                if (Path.Waypoints.ElementAtOrDefault(currentWaypointTask) != null && !StoppedAtWaypoint)
                {
                    CurrentWaypoint = Path.Waypoints[currentWaypointTask];
                    float acceptedDistance = GetAcceptedStoppingDistance(Path.Waypoints, currentWaypointTask);

                    Game.LogTrivial($"{CurrentVehicle.Model.Name} [{CurrentVehicle.Handle}] is driving to path \"{Path.Name}\" waypoint {Path.Waypoints[currentWaypointTask].Number} (Stop: {CurrentWaypoint.IsStopWaypoint}, Driving flag: {CurrentWaypoint.DrivingFlagType})");
                    Tasks.DriveToPosition(Path.Waypoints[currentWaypointTask].Position, Path.Waypoints[currentWaypointTask].Speed, (VehicleDrivingFlags)Path.Waypoints[currentWaypointTask].DrivingFlagType, acceptedDistance);
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

                    Tasks.PerformDrivingManeuver(CurrentVehicle, VehicleManeuver.GoForwardWithCustomSteeringAngle, 3).WaitForCompletion();
                }
            }

            void LoopWhileDrivingToWaypoint(int currentWaypointTask, float acceptedDistance, Vector3 oldPosition)
            {
                int lostTasks = 0;
                while (VehicleAndDriverAreValid() && !Dismissed && !SkipWaypoint && !Directed && Path.Waypoints.ElementAtOrDefault(currentWaypointTask) != null && CurrentVehicle.FrontPosition.DistanceTo2D(Path.Waypoints[currentWaypointTask].Position) > acceptedDistance)
                {
                    if (oldPosition != Path.Waypoints[currentWaypointTask].Position)
                    {
                        Game.LogTrivial($"Waypoint position has changed, updating drive task.");
                        oldPosition = Path.Waypoints[currentWaypointTask].Position;
                        Tasks.DriveToPosition(Path.Waypoints[currentWaypointTask].Position, Path.Waypoints[currentWaypointTask].Speed, (VehicleDrivingFlags)Path.Waypoints[currentWaypointTask].DrivingFlagType, acceptedDistance);
                    }
                    if (Tasks.CurrentTaskStatus == TaskStatus.NoTask)
                    {
                        //Game.DisplayNotification($"~o~Scene Manager ~r~[Error]\n{Vehicle.Model.Name} [{Vehicle.Handle}] driver [{Driver.Handle}] has no task.  Reassiging current waypoint task.");
                        Game.LogTrivial($"{CurrentVehicle.Model.Name} [{CurrentVehicle.Handle}] driver [{Handle}] has no task.  Reassiging current waypoint task.");
                        if(lostTasks == 3)
                        {
                            Game.LogTrivial($"{CurrentVehicle.Model.Name} [{CurrentVehicle.Handle}] driver [{Handle}] lost their task too many times.  Dismissing from world to prevent problems.");
                            Dismiss(Utils.Dismiss.FromWorld);
                            return;
                        }
                        if (CurrentVehicle)
                        {
                            Tasks.DriveToPosition(Path.Waypoints[currentWaypointTask].Position, Path.Waypoints[currentWaypointTask].Speed, (VehicleDrivingFlags)Path.Waypoints[currentWaypointTask].DrivingFlagType, acceptedDistance);
                            lostTasks++;
                            Game.LogTrivial($"{CurrentVehicle.Model.Name} [{CurrentVehicle.Handle}] driver [{Handle}] should have a task now.");
                        }
                        else
                        {
                            Game.LogTrivial($"{CurrentVehicle.Model.Name} [{CurrentVehicle.Handle}] driver [{Handle}] is not in a vehicle.  Exiting loop.");
                            return;
                        }
                    }
                    GameFiber.Sleep(100);
                }
            }
        }

        private void StopAtWaypoint()
        {
            var stoppingDistance = GetAcceptedStoppingDistance(CurrentWaypoint.Path.Waypoints, CurrentWaypoint.Path.Waypoints.IndexOf(CurrentWaypoint));
            Game.LogTrivial($"{CurrentVehicle.Model.Name} stopping at path \"{CurrentWaypoint.Path.Name}\" waypoint.");
            var vehicleToStop = CurrentVehicle;
            Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(vehicleToStop, stoppingDistance, -1, true);
            StoppedAtWaypoint = true;

            while (CurrentWaypoint != null && VehicleAndDriverAreValid() && StoppedAtWaypoint && !Directed && IsInVehicle(CurrentVehicle, false))
            {
                GameFiber.Yield();
            }
            if(vehicleToStop)
            {
                Game.LogTrivial($"{vehicleToStop.Model.Name} releasing from stop waypoint.");
                Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(vehicleToStop, 0f, 1, true);
            }
        }

        private bool VehicleAndDriverAreValid()
        {
            if (!this)
            {
                Game.LogTrivial($"CollectedPed is null");
                return false;
            }
            if (!CurrentVehicle)
            {
                Game.LogTrivial($"Vehicle is null");
                return false;
            }
            if (!IsAlive)
            {
                Game.LogTrivial($"Driver is null or dead or not in a vehicle");
                return false;
            }
            return true;
        }

        internal void Dismiss(Dismiss dismissOption = Utils.Dismiss.FromPath, Path newPath = null)
        {
            if(!this)
            {
                Game.LogTrivial($"CollectedPed is not valid in Dismiss.");
                Path.CollectedPeds.Remove(this);
                return;
            }
            if(Dismissed)
            {
                Game.LogTrivial($"CollectedPed is already dismissed.");
                return;
            }

            if (dismissOption == Utils.Dismiss.FromWorld)
            {
                DismissFromWorld();
                return;
            }

            if (CurrentVehicle && StoppedAtWaypoint)
            {
                StoppedAtWaypoint = false;
                Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(LastVehicle, 0f, 1, true);
                Tasks.CruiseWithVehicle(5f);
            }
            Tasks.Clear();

            if (dismissOption == Utils.Dismiss.FromWaypoint)
            {
                DismissFromWaypoint();
            }

            if (dismissOption == Utils.Dismiss.FromPath)
            {
                DismissFromPath();
            }

            if (dismissOption == Utils.Dismiss.FromDirected)
            {
                DismissFromDirect();
            }

            void DismissFromWorld()
            {
                Game.LogTrivial($"Dismissed {CurrentVehicle.Model.Name} [{CurrentVehicle.Handle}] from the world");
                if (GetAttachedBlip())
                {
                    GetAttachedBlip().Delete();
                }
                while (this && CurrentVehicle && CurrentVehicle.HasOccupants)
                {
                    foreach (Ped occupant in CurrentVehicle.Occupants)
                    {
                        occupant.Delete();
                    }
                    GameFiber.Yield();
                }
                if(!this)
                {
                    Path.CollectedPeds.Remove(this);
                    return;
                }
                if (CurrentVehicle)
                {
                    CurrentVehicle.Delete();
                }
                Path.CollectedPeds.Remove(this);
            }

            void DismissFromWaypoint()
            {
                if (Path == null)
                {
                    Game.LogTrivial($"Path is null");
                    return;
                }

                if(CurrentWaypoint == null)
                {
                    Game.LogTrivial($"CurrentWaypoint is null");
                    return;
                }
                
                if (CurrentWaypoint?.Number != Path?.Waypoints.Count)
                {
                    Game.LogTrivial($"{CurrentVehicle?.Model.Name} [{CurrentVehicle?.Handle}] dismissed from waypoint.");
                    SkipWaypoint = true;
                }
                else if (CurrentWaypoint?.Number == Path?.Waypoints.Count)
                {
                    DismissFromPath();
                }
            }

            void DismissFromPath()
            {
                if(!this)
                {
                    Game.LogTrivial($"CollectedPed is not valid in DismissFromPath.");
                    Path.CollectedPeds.Remove(this);
                    return;
                }

                if(Dismissed)
                {
                    Game.LogTrivial($"CollectedPed is already dismissed.");
                    return;
                }

                Game.LogTrivial($"Dismissing {CurrentVehicle?.Model.Name} [{CurrentVehicle?.Handle}] from path");
                Dismissed = true;

                // Check if the vehicle is near any of the path's collector waypoints
                //GameFiber.StartNew(() =>
                //{
                    //var nearestCollectorWaypoint = Path.Waypoints.Where(wp => wp.IsCollector).OrderBy(wp => CurrentVehicle.DistanceTo2D(wp.Position)).FirstOrDefault();
                    //if (nearestCollectorWaypoint != null)
                    //{
                    //    while (nearestCollectorWaypoint != null && CurrentVehicle && CurrentVehicle.HasDriver && this && IsAlive && CurrentVehicle.FrontPosition.DistanceTo2D(nearestCollectorWaypoint.Position) <= nearestCollectorWaypoint.CollectorRadius)
                    //    {
                    //        GameFiber.Yield();
                    //    }
                    //}

                if (!Directed)
                {
                    if (this)
                    {
                        if (GetAttachedBlip())
                        {
                            GetAttachedBlip().Delete();
                        }
                        BlockPermanentEvents = false;
                        IsPersistent = false;
                        base.Dismiss();
                    }
                    
                    if (CurrentVehicle)
                    {
                        Game.LogTrivial($"{CurrentVehicle.Model.Name} [{CurrentVehicle.Handle}] dismissed successfully.");
                        LastVehicle.Dismiss();

                        if (CurrentVehicle)
                        {
                            LastVehicle.IsSirenOn = false;
                            LastVehicle.IsSirenSilent = true;
                        }
                    }
                    Path.CollectedPeds.Remove(this);
                }
                //}, "DismissFromPath Fiber");         
            }

            void DismissFromDirect()
            {
                Dismissed = true;
                Directed = true;
                if (newPath != null)
                {
                    newPath.CollectedPeds.Add(this);
                    Path.CollectedPeds.Remove(this);
                }
                Tasks.Clear();
            }
        }
    }
}
