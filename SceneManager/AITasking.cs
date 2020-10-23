using Rage;
using System.Collections.Generic;
using System.Linq;

namespace SceneManager
{
    // Driving styles https://gtaforums.com/topic/822314-guide-driving-styles/
    // also https://vespura.com/fivem/drivingstyle/

    class AITasking
    {
        internal static void AssignWaypointTasks(CollectedVehicle collectedVehicle, Path path, Waypoint currentWaypoint)
        {
            float acceptedDistance = GetAcceptedStoppingDistance(path.Waypoints, path.Waypoints.IndexOf(currentWaypoint));
            Vector3 oldPosition = currentWaypoint.Position;
            if (!VehicleAndDriverAreValid(collectedVehicle))
            {
                return;
            }

            collectedVehicle.Path = path;
            if(currentWaypoint != null)
            {
                collectedVehicle.CurrentWaypoint = currentWaypoint;
            }
            else
            {
                collectedVehicle.CurrentWaypoint = path.Waypoints[0];
            }

            if (currentWaypoint != null && collectedVehicle.Directed)
            {
                collectedVehicle.Dismissed = false;

                while (!collectedVehicle.ReadyForDirectTasks)
                {
                    GameFiber.Yield();
                }
                collectedVehicle.Driver.Tasks.Clear();
                AssignTasksForDirectedDriver();
                LoopWhileDrivingToDirectedWaypoint();
                if(collectedVehicle != null)
                {
                    collectedVehicle.Directed = false;
                }
                if (collectedVehicle.Vehicle)
                {
                    collectedVehicle.Driver.Tasks.PerformDrivingManeuver(collectedVehicle.Vehicle, VehicleManeuver.GoForwardWithCustomSteeringAngle, 3).WaitForCompletion();
                    Game.LogTrivial($"{collectedVehicle.Vehicle.Model.Name} directed task is complete, directed is now false");
                }
            }

            if (currentWaypoint.IsStopWaypoint)
            {
                StopVehicleAtWaypoint(currentWaypoint, collectedVehicle);
            }
            if(path?.Waypoints?.Count > 0 && currentWaypoint != path?.Waypoints?.Last())
            {
                DriveVehicleToNextWaypoint(collectedVehicle, path, currentWaypoint);
            }

            if (!VehicleAndDriverAreValid(collectedVehicle) || collectedVehicle.Directed)
            {
                return;
            }
            Game.LogTrivial($"{collectedVehicle.Vehicle.Model.Name} all Path {path.Number} tasks complete.");
            if(!collectedVehicle.Dismissed)
            {
                collectedVehicle.Dismiss();
            }

            void AssignTasksForDirectedDriver()
            {
                Game.LogTrivial($"{collectedVehicle.Vehicle.Model.Name} is driving to path {currentWaypoint.Path.Number} waypoint {currentWaypoint.Number} (directed)");
                collectedVehicle.Driver.Tasks.DriveToPosition(currentWaypoint.Position, currentWaypoint.Speed, (VehicleDrivingFlags)currentWaypoint.DrivingFlagType, acceptedDistance);
            }

            void LoopWhileDrivingToDirectedWaypoint()
            {
                while (VehicleAndDriverAreValid(collectedVehicle) && !collectedVehicle.Dismissed && !collectedVehicle.SkipWaypoint && collectedVehicle.Vehicle.FrontPosition.DistanceTo2D(oldPosition) > acceptedDistance)
                {
                    if (oldPosition != currentWaypoint.Position)
                    {
                        oldPosition = currentWaypoint.Position;
                    }
                    GameFiber.Yield();
                }
            }
        }

        private static void DriveVehicleToNextWaypoint(CollectedVehicle collectedVehicle, Path path, Waypoint currentWaypoint)
        {
            if (!VehicleAndDriverAreValid(collectedVehicle) || currentWaypoint == null || currentWaypoint.Path == null)
            {
                Game.LogTrivial($"Vehicle, driver, waypoint, or path is null.");
                return;
            }

            var vehicle = collectedVehicle.Vehicle;
            var driver = vehicle.Driver;

            Game.LogTrivial($"Preparing to run task loop for {collectedVehicle.Vehicle.Model.Name} on path {path.Number}");
            //Logger.Log($"Current path: {collectedVehicle.Path.Number}, Current waypoint: {collectedVehicle.CurrentWaypoint.Number}");
            for (int currentWaypointTask = currentWaypoint.Number; currentWaypointTask < path.Waypoints.Count; currentWaypointTask++)
            {
                var oldPosition = path.Waypoints[currentWaypointTask].Position;
                collectedVehicle.SkipWaypoint = false;

                if (collectedVehicle == null || !collectedVehicle.Vehicle || collectedVehicle.Dismissed || collectedVehicle.Directed)
                {
                    Game.LogTrivial($"Vehicle dismissed, directed, or null, return");
                    return;
                }
                if(collectedVehicle.Driver == null || !collectedVehicle.Driver || !collectedVehicle.Vehicle.HasDriver || !collectedVehicle.Driver.IsAlive || collectedVehicle.Vehicle.Driver == Game.LocalPlayer.Character)
                {
                    Game.LogTrivial($"{vehicle.Model.Name} does not have a driver/driver is null or driver is dead.");
                    return;
                }

                if (path.Waypoints.ElementAtOrDefault(currentWaypointTask) != null && !collectedVehicle.StoppedAtWaypoint)
                {
                    collectedVehicle.CurrentWaypoint = path.Waypoints[currentWaypointTask];
                    float acceptedDistance = GetAcceptedStoppingDistance(path.Waypoints, currentWaypointTask);

                    Game.LogTrivial($"{vehicle.Model.Name} is driving to path {currentWaypoint.Path.Number} waypoint {path.Waypoints[currentWaypointTask].Number} (Stop: {currentWaypoint.IsStopWaypoint}, Driving flag: {currentWaypoint.DrivingFlagType})");
                    //Logger.Log($"{vehicle.Model.Name} driver is persistent: {driver.IsPersistent}");
                    driver.Tasks.DriveToPosition(path.Waypoints[currentWaypointTask].Position, path.Waypoints[currentWaypointTask].Speed, (VehicleDrivingFlags)path.Waypoints[currentWaypointTask].DrivingFlagType, acceptedDistance);
                    LoopWhileDrivingToWaypoint(currentWaypointTask, acceptedDistance, oldPosition);

                    if (!VehicleAndDriverAreValid(collectedVehicle))
                    {
                        return;
                    }

                    if (collectedVehicle.SkipWaypoint)
                    {
                        collectedVehicle.SkipWaypoint = false;
                        continue;
                    }

                    if (!collectedVehicle.Dismissed && !collectedVehicle.Directed && path.Waypoints.ElementAtOrDefault(currentWaypointTask) != null && path.Waypoints[currentWaypointTask].IsStopWaypoint)
                    {
                        StopVehicleAtWaypoint(path.Waypoints[currentWaypointTask], collectedVehicle);
                    }

                    if (!VehicleAndDriverAreValid(collectedVehicle) || collectedVehicle.Dismissed || collectedVehicle.Directed)
                    {
                        return;
                    }

                    driver.Tasks.PerformDrivingManeuver(collectedVehicle.Vehicle, VehicleManeuver.GoForwardWithCustomSteeringAngle, 3).WaitForCompletion();
                    // Do we need this?
                    //if (driver)
                    //{
                    //    driver.Tasks.Clear();
                    //}
                }
            }

            void LoopWhileDrivingToWaypoint(int currentWaypointTask, float acceptedDistance, Vector3 oldPosition)
            {
                while (VehicleAndDriverAreValid(collectedVehicle) && !collectedVehicle.Dismissed && !collectedVehicle.SkipWaypoint && !collectedVehicle.Directed && path.Waypoints.ElementAtOrDefault(currentWaypointTask) != null && collectedVehicle.Vehicle.FrontPosition.DistanceTo2D(path.Waypoints[currentWaypointTask].Position) > acceptedDistance)
                {
                    if (oldPosition != path.Waypoints[currentWaypointTask].Position)
                    {
                        oldPosition = path.Waypoints[currentWaypointTask].Position;
                        driver.Tasks.DriveToPosition(path.Waypoints[currentWaypointTask].Position, path.Waypoints[currentWaypointTask].Speed, (VehicleDrivingFlags)path.Waypoints[currentWaypointTask].DrivingFlagType, acceptedDistance);
                    }
                    GameFiber.Sleep(100);
                }
            }
        }

        private static float GetAcceptedStoppingDistance(List<Waypoint> waypoints, int nextWaypoint)
        {
            float dist;
            if(Settings.SpeedUnit == SpeedUnits.MPH)
            {
                dist = (MathHelper.ConvertMilesPerHourToKilometersPerHour(waypoints[nextWaypoint].Speed) * MathHelper.ConvertMilesPerHourToKilometersPerHour(waypoints[nextWaypoint].Speed)) / (250 * 0.8f);
            }
            else
            {
                dist = (waypoints[nextWaypoint].Speed * waypoints[nextWaypoint].Speed) / (250 * 0.8f);
            }
            var acceptedDistance = MathHelper.Clamp(dist, 2, 10);
            //Logger.Log($"Accepted distance: {acceptedDistance}");
            return acceptedDistance;
        }

        private static bool VehicleAndDriverAreValid(CollectedVehicle collectedVehicle)
        {
            if (collectedVehicle == null)
            {
                Game.LogTrivial($"CollectedVehicle is null");
                return false;
            }
            if (!collectedVehicle.Vehicle && !collectedVehicle.Dismissed)
            {
                Game.LogTrivial($"Vehicle is null");
                collectedVehicle.Dismiss();
                return false;
            }
            if (collectedVehicle.Driver == null || !collectedVehicle.Driver || !collectedVehicle.Driver.IsAlive && !collectedVehicle.Dismissed)
            {
                collectedVehicle.Dismiss();
                //Logger.Log($"{collectedVehicle.Vehicle.Model.Name} driver is null or dead");
                return false;
            }
            return true;
        }

        private static void StopVehicleAtWaypoint(Waypoint currentWaypoint, CollectedVehicle collectedVehicle)
        {
            if (!VehicleAndDriverAreValid(collectedVehicle))
            {
                return;
            }

            var stoppingDistance = GetAcceptedStoppingDistance(currentWaypoint.Path.Waypoints, currentWaypoint.Path.Waypoints.IndexOf(currentWaypoint));
            Game.LogTrivial($"{collectedVehicle.Vehicle.Model.Name} stopping at path {currentWaypoint.Path.Number} waypoint.");
            Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(collectedVehicle.Vehicle, stoppingDistance, -1, true);
            collectedVehicle.StoppedAtWaypoint = true;

            while (currentWaypoint != null && VehicleAndDriverAreValid(collectedVehicle) && collectedVehicle.StoppedAtWaypoint && !collectedVehicle.Directed)
            {
                GameFiber.Yield();
            }
            if(collectedVehicle.Vehicle && collectedVehicle.Driver)
            {
                Game.LogTrivial($"{collectedVehicle.Vehicle.Model.Name} releasing from stop waypoint.");
                Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(collectedVehicle.Vehicle, 0f, 1, true);
                collectedVehicle.Driver.Tasks.CruiseWithVehicle(5f);
            }
        }
    }
}
