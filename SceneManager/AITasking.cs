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
            if (!VehicleAndDriverAreValid(collectedVehicle))
            {
                return;
            }

            if (currentWaypoint != null && collectedVehicle.Directed)
            {
                float acceptedDistance = GetAcceptedStoppingDistance(path.Waypoints, path.Waypoints.IndexOf(currentWaypoint));
                while (!collectedVehicle.ReadyForDirectTasks)
                {
                    GameFiber.Yield();
                }
                AssignTasksForDirectedDriver(acceptedDistance);
                LoopWhileDrivingToDirectedWaypoint(acceptedDistance);
                collectedVehicle.Directed = false;
                Logger.Log($"{collectedVehicle.Vehicle.Model.Name} directed task is complete, directed is now false");
            }

            if (currentWaypoint.DrivingFlag == VehicleDrivingFlags.StopAtDestination)
            {
                StopVehicleAtWaypoint(currentWaypoint, collectedVehicle);
            }
            if(currentWaypoint != path?.Waypoints?.Last())
            {
                DriveVehicleToNextWaypoint(collectedVehicle, path, currentWaypoint);
            }

            if (!VehicleAndDriverAreValid(collectedVehicle))
            {
                return;
            }
            Logger.Log($"{collectedVehicle.Vehicle.Model.Name} all Path {path.Number} tasks complete.");
            if (collectedVehicle.Directed)
            {
                collectedVehicle.Dismiss(DismissOption.FromDirect);
            }
            else if(!collectedVehicle.Dismissed)
            {
                collectedVehicle.Dismiss();
            }

            void AssignTasksForDirectedDriver(float acceptedDistance)
            {
                //Logger.Log($"{collectedVehicle.Vehicle.Model.Name} distance to collection waypoint: {collectedVehicle.Vehicle.DistanceTo2D(currentWaypoint.Position)}");

                Logger.Log($"{collectedVehicle.Vehicle.Model.Name} is driving to path {currentWaypoint.Path.Number} waypoint {currentWaypoint.Number} (directed)");
                Logger.Log($"{collectedVehicle.Vehicle.Model.Name} Dismissed: {collectedVehicle.Dismissed}, Directed: {collectedVehicle.SkipWaypoint}");
                if (currentWaypoint.DrivingFlag == VehicleDrivingFlags.IgnorePathFinding)
                {
                    collectedVehicle.Driver.Tasks.DriveToPosition(currentWaypoint.Position, currentWaypoint.Speed, (VehicleDrivingFlags)17040299, acceptedDistance);
                }
                else
                {
                    collectedVehicle.Driver.Tasks.DriveToPosition(currentWaypoint.Position, currentWaypoint.Speed, (VehicleDrivingFlags)263075, acceptedDistance);
                }
            }

            void LoopWhileDrivingToDirectedWaypoint(float acceptedDistance)
            {
                while (VehicleAndDriverAreValid(collectedVehicle) && !collectedVehicle.Dismissed && !collectedVehicle.SkipWaypoint && collectedVehicle.Vehicle.FrontPosition.DistanceTo2D(currentWaypoint.Position) > acceptedDistance)
                {
                    //Logger.Log($"Looping while {collectedVehicle.Vehicle.Model.Name} drives to path {path.Number} waypoint {currentWaypoint.Number}");
                    GameFiber.Yield();
                }
            }
        }

        private static void DriveVehicleToNextWaypoint(CollectedVehicle collectedVehicle, Path path, Waypoint currentWaypoint)
        {
            if (!VehicleAndDriverAreValid(collectedVehicle) || currentWaypoint == null || currentWaypoint.Path == null)
            {
                Logger.Log($"Vehicle, driver, waypoint, or path is null.");
                return;
            }

            var vehicle = collectedVehicle.Vehicle;
            var driver = vehicle.Driver;

            Logger.Log($"Preparing to run task loop for {collectedVehicle.Vehicle.Model.Name} on path {path.Number}");
            for (int currentWaypointTask = currentWaypoint.Number; currentWaypointTask < path.Waypoints.Count; currentWaypointTask++)
            {
                //Logger.Log($"{collectedVehicle.Vehicle.Model.Name} in the task loop");
                collectedVehicle.SkipWaypoint = false;

                if (collectedVehicle == null || collectedVehicle.Dismissed || collectedVehicle.Directed)
                {
                    Logger.Log($"Vehicle dismissed or null, return");
                    return;
                }
                if(collectedVehicle.Driver == null || !collectedVehicle.Vehicle.HasDriver || !collectedVehicle.Driver.IsAlive)
                {
                    Logger.Log($"{vehicle.Model.Name} does not have a driver/driver is null or driver is dead.");
                }

                if (path.Waypoints.ElementAtOrDefault(currentWaypointTask) != null && !collectedVehicle.StoppedAtWaypoint)
                {
                    collectedVehicle.CurrentWaypoint = path.Waypoints[currentWaypointTask];
                    //Logger.Log($"{collectedVehicle.Vehicle.Model.Name} current waypoint: {collectedVehicle.CurrentWaypoint.Number}");
                    float acceptedDistance = GetAcceptedStoppingDistance(path.Waypoints, currentWaypointTask);

                    Logger.Log($"{vehicle.Model.Name} is driving to path {currentWaypoint.Path.Number} waypoint {path.Waypoints[currentWaypointTask].Number}");
                    Logger.Log($"{vehicle.Model.Name} driver is persistent: {driver.IsPersistent}");
                    if (path.Waypoints[currentWaypointTask].DrivingFlag == VehicleDrivingFlags.IgnorePathFinding)
                    {
                        driver.Tasks.DriveToPosition(path.Waypoints[currentWaypointTask].Position, path.Waypoints[currentWaypointTask].Speed, (VehicleDrivingFlags)17040299, acceptedDistance);
                    }
                    else
                    {
                        driver.Tasks.DriveToPosition(path.Waypoints[currentWaypointTask].Position, path.Waypoints[currentWaypointTask].Speed, (VehicleDrivingFlags)263075, acceptedDistance);
                    }
                    LoopWhileDrivingToWaypoint(currentWaypointTask, acceptedDistance);

                    if (!VehicleAndDriverAreValid(collectedVehicle))
                    {
                        return;
                    }

                    if (collectedVehicle.SkipWaypoint)
                    {
                        collectedVehicle.SkipWaypoint = false;
                        continue;
                    }

                    if (!collectedVehicle.Dismissed && path.Waypoints.ElementAtOrDefault(currentWaypointTask) != null && path.Waypoints[currentWaypointTask].DrivingFlag == VehicleDrivingFlags.StopAtDestination)
                    {
                        StopVehicleAtWaypoint(path.Waypoints[currentWaypointTask], collectedVehicle);
                    }

                    if (!VehicleAndDriverAreValid(collectedVehicle) || collectedVehicle.Dismissed)
                    {
                        return;
                    }

                    driver.Tasks.PerformDrivingManeuver(collectedVehicle.Vehicle, VehicleManeuver.GoForwardWithCustomSteeringAngle, 3).WaitForCompletion();
                    // Do we need this?
                    if (driver)
                    {
                        driver.Tasks.Clear();
                    }
                }
            }

            void LoopWhileDrivingToWaypoint(int nextWaypoint, float acceptedDistance)
            {
                while (VehicleAndDriverAreValid(collectedVehicle) && !collectedVehicle.Dismissed && !collectedVehicle.SkipWaypoint && path.Waypoints.ElementAtOrDefault(nextWaypoint) != null && collectedVehicle.Vehicle.FrontPosition.DistanceTo2D(path.Waypoints[nextWaypoint].Position) > acceptedDistance)
                {
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
                Logger.Log($"CollectedVehicle is null");
                return false;
            }
            if (!collectedVehicle.Vehicle && !collectedVehicle.Dismissed)
            {
                Logger.Log($"Vehicle is null");
                collectedVehicle.Dismiss();
                //if(collectedVehicle.Driver)
                //{
                //    collectedVehicle.Driver.IsPersistent = false;
                //}
                return false;
            }
            if (!collectedVehicle.Driver || !collectedVehicle.Driver.IsAlive && !collectedVehicle.Dismissed)
            {
                collectedVehicle.Dismiss();
                //Logger.Log($"{collectedVehicle.Vehicle.Model.Name} driver is null or dead");
                //Logger.Log($"{collectedVehicle.Vehicle.Model.Name} persistent: {collectedVehicle.Vehicle.IsPersistent}");
                //collectedVehicle.Vehicle.IsPersistent = false;
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
            Logger.Log($"{collectedVehicle.Vehicle.Model.Name} stopping at waypoint.");
            Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(collectedVehicle.Vehicle, stoppingDistance, -1, true);
            collectedVehicle.StoppedAtWaypoint = true;

            while (currentWaypoint != null && VehicleAndDriverAreValid(collectedVehicle) && collectedVehicle.StoppedAtWaypoint && !collectedVehicle.Directed)
            {
                GameFiber.Yield();
            }
            if(collectedVehicle.Vehicle && collectedVehicle.Driver)
            {
                Logger.Log($"{collectedVehicle.Vehicle.Model.Name} releasing from stop waypoint.");
                Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(collectedVehicle.Vehicle, 0f, 1, true);
                collectedVehicle.Driver.Tasks.CruiseWithVehicle(5f);
            }
        }
    }
}
