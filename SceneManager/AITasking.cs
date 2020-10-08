using Rage;
using System.Collections.Generic;
using System.Linq;

namespace SceneManager
{
    // Driving styles https://gtaforums.com/topic/822314-guide-driving-styles/
    // also https://vespura.com/fivem/drivingstyle/

    class AITasking
    {
        internal static void AssignWaypointTasks(CollectedVehicle collectedVehicle, List<Waypoint> waypoints, Waypoint currentWaypoint)
        {
            if (!VehicleAndDriverAreValid(collectedVehicle))
            {
                return;
            }

            if (currentWaypoint != null && collectedVehicle.Directed)
            {
                float acceptedDistance = GetAcceptedStoppingDistance(waypoints, waypoints.IndexOf(currentWaypoint));
                AssignTasksForDirectedDriver(acceptedDistance);
                LoopWhileDrivingToDirectedWaypoint(acceptedDistance);
            }

            if (currentWaypoint.DrivingFlag == VehicleDrivingFlags.StopAtDestination)
            {
                StopVehicleAtWaypoint(currentWaypoint, collectedVehicle);
            }
            DriveVehicleToNextWaypoint(collectedVehicle, waypoints, currentWaypoint);

            if (!VehicleAndDriverAreValid(collectedVehicle))
            {
                return;
            }
            Logger.Log($"{collectedVehicle.Vehicle.Model.Name} all tasks complete.");
            if (!collectedVehicle.Dismissed)
            {
                Logger.Log($"Dismissing {collectedVehicle.Vehicle.Model.Name} from path");
                collectedVehicle.Dismiss();
            }

            void AssignTasksForDirectedDriver(float acceptedDistance)
            {
                collectedVehicle.Dismissed = false;
                collectedVehicle.Directed = false;
                //Logger.Log($"{collectedVehicle.Vehicle.Model.Name} distance to collection waypoint: {collectedVehicle.Vehicle.DistanceTo2D(currentWaypoint.Position)}");

                Logger.Log($"{collectedVehicle.Vehicle.Model.Name} is driving to path {currentWaypoint.Path.Number} waypoint {currentWaypoint.Number}");
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
                    //Logger.Log($"Looping while {collectedVehicle.Vehicle.Model.Name} drives to waypoint {currentWaypoint.Number} ({collectedVehicle.Vehicle.DistanceTo2D(currentWaypoint.Position)}m away)");
                    GameFiber.Yield();
                }
            }
        }

        private static void DriveVehicleToNextWaypoint(CollectedVehicle collectedVehicle, List<Waypoint> waypoints, Waypoint currentWaypoint)
        {
            if (!VehicleAndDriverAreValid(collectedVehicle) || currentWaypoint == null || currentWaypoint.Path == null)
            {
                Logger.Log($"Vehicle, driver, waypoint, or path is null.");
                return;
            }

            var vehicle = collectedVehicle.Vehicle;
            var driver = vehicle.Driver;

            Logger.Log($"Preparing to run task loop for {collectedVehicle.Vehicle.Model.Name}");
            for (int currentWaypointTask = currentWaypoint.Number; currentWaypointTask < waypoints.Count; currentWaypointTask++)
            {
                //Logger.Log($"{collectedVehicle.Vehicle.Model.Name} in the task loop");
                collectedVehicle.SkipWaypoint = false;

                if (collectedVehicle.Dismissed || collectedVehicle == null)
                {
                    Logger.Log($"Vehicle dismissed or null, return");
                    return;
                }

                if (waypoints.ElementAtOrDefault(currentWaypointTask) != null && !collectedVehicle.StoppedAtWaypoint)
                {
                    collectedVehicle.CurrentWaypoint = waypoints[currentWaypointTask];
                    //Logger.Log($"{collectedVehicle.Vehicle.Model.Name} current waypoint: {collectedVehicle.CurrentWaypoint.Number}");
                    float acceptedDistance = GetAcceptedStoppingDistance(waypoints, currentWaypointTask);

                    Logger.Log($"{vehicle.Model.Name} is driving to path {currentWaypoint.Path.Number} waypoint {waypoints[currentWaypointTask].Number}");
                    if (waypoints[currentWaypointTask].DrivingFlag == VehicleDrivingFlags.IgnorePathFinding)
                    {
                        driver.Tasks.DriveToPosition(waypoints[currentWaypointTask].Position, waypoints[currentWaypointTask].Speed, (VehicleDrivingFlags)17040299, acceptedDistance);
                    }
                    else
                    {
                        driver.Tasks.DriveToPosition(waypoints[currentWaypointTask].Position, waypoints[currentWaypointTask].Speed, (VehicleDrivingFlags)263075, acceptedDistance);
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

                    if (!collectedVehicle.Dismissed && waypoints.ElementAtOrDefault(currentWaypointTask) != null && waypoints[currentWaypointTask].DrivingFlag == VehicleDrivingFlags.StopAtDestination)
                    {
                        StopVehicleAtWaypoint(waypoints[currentWaypointTask], collectedVehicle);
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
                while (VehicleAndDriverAreValid(collectedVehicle) && !collectedVehicle.Dismissed && !collectedVehicle.SkipWaypoint && waypoints.ElementAtOrDefault(nextWaypoint) != null && collectedVehicle.Vehicle.FrontPosition.DistanceTo2D(waypoints[nextWaypoint].Position) > acceptedDistance)
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
            if (!collectedVehicle.Vehicle)
            {
                Logger.Log($"Vehicle is null");
                return false;
            }
            if (!collectedVehicle.Driver)
            {
                Logger.Log($"Driver is null");
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

            while (currentWaypoint != null && VehicleAndDriverAreValid(collectedVehicle) && collectedVehicle.StoppedAtWaypoint)
            {
                GameFiber.Yield();
            }
            if(collectedVehicle.Vehicle && collectedVehicle.Driver)
            {
                Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(collectedVehicle.Vehicle, 0f, 1, true);
                collectedVehicle.Driver.Tasks.CruiseWithVehicle(5f);
            }
        }
    }
}
