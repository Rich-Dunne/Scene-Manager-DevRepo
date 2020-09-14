using Rage;
using System.Collections.Generic;
using System.Linq;

namespace SceneManager
{
    class AITasking
    {
        internal static void AssignWaypointTasks(CollectedVehicle collectedVehicle, List<Waypoint> waypoints, Waypoint currentWaypoint)
        {
            if (!VehicleAndDriverNullChecks(collectedVehicle))
            {
                return;
            }

            if (currentWaypoint != null)
            {
                float acceptedDistance = GetAcceptedStoppingDistance(waypoints, waypoints.IndexOf(currentWaypoint));
                Logger.Log($"{collectedVehicle.Vehicle.Model.Name} distance to collection waypoint: {collectedVehicle.Vehicle.DistanceTo2D(currentWaypoint.Position)}");
                if(collectedVehicle.Vehicle.DistanceTo2D(currentWaypoint.Position) > (currentWaypoint.CollectorRadius + 0.5))
                {
                    Logger.Log($"{collectedVehicle.Vehicle.Model.Name} is driving to waypoint {currentWaypoint.Number}");
                    collectedVehicle.Driver.Tasks.DriveToPosition(currentWaypoint.Position, currentWaypoint.Speed, (VehicleDrivingFlags)263075, acceptedDistance);
                    LoopWhileDrivingToWaypoint(acceptedDistance);
                }
                if(currentWaypoint.DrivingFlag == VehicleDrivingFlags.StopAtDestination)
                {
                    StopVehicleAtWaypoint(currentWaypoint, collectedVehicle);
                }

            }

            DriveVehicleToNextWaypoint(collectedVehicle, waypoints, currentWaypoint);

            if (!VehicleAndDriverNullChecks(collectedVehicle))
            {
                return;
            }
            Logger.Log($"{collectedVehicle.Vehicle.Model.Name} all tasks complete.");
            if (!collectedVehicle.Dismissed)
            {
                collectedVehicle.Dismiss();
            }

            void LoopWhileDrivingToWaypoint(float acceptedDistance)
            {
                while (VehicleAndDriverNullChecks(collectedVehicle) && !collectedVehicle.Dismissed && !collectedVehicle.SkipWaypoint && collectedVehicle.Vehicle.FrontPosition.DistanceTo2D(currentWaypoint.Position) > acceptedDistance)
                {
                    //Logger.Log($"Looping while {collectedVehicle.Vehicle.Model.Name} drives to waypoint {currentWaypoint.Number} ({collectedVehicle.Vehicle.DistanceTo2D(currentWaypoint.Position)}m away)");
                    GameFiber.Yield();
                }
            }
        }

        private static void DriveVehicleToNextWaypoint(CollectedVehicle collectedVehicle, List<Waypoint> waypoints, Waypoint currentWaypoint)
        {
            if (!VehicleAndDriverNullChecks(collectedVehicle))
            {
                return;
            }

            var vehicle = collectedVehicle.Vehicle;
            var driver = vehicle.Driver;

            for (int nextWaypoint = currentWaypoint.Number; nextWaypoint < waypoints.Count; nextWaypoint++)
            {
                if (!VehicleAndDriverNullChecks(waypoints, nextWaypoint, collectedVehicle))
                {
                    return;
                }

                if (waypoints.ElementAtOrDefault(nextWaypoint) != null && !collectedVehicle.StoppedAtWaypoint)
                {
                    collectedVehicle.CurrentWaypoint = waypoints[nextWaypoint];
                    float acceptedDistance = GetAcceptedStoppingDistance(waypoints, nextWaypoint);

                    Logger.Log($"{vehicle.Model.Name} is driving to waypoint {waypoints[nextWaypoint].Number}");
                    if (waypoints[nextWaypoint].DrivingFlag == VehicleDrivingFlags.IgnorePathFinding)
                    {
                        collectedVehicle.Driver.Tasks.DriveToPosition(waypoints[nextWaypoint].Position, waypoints[nextWaypoint].Speed, (VehicleDrivingFlags)17040299, acceptedDistance);
                    }
                    else
                    {
                        collectedVehicle.Driver.Tasks.DriveToPosition(waypoints[nextWaypoint].Position, waypoints[nextWaypoint].Speed, (VehicleDrivingFlags)263075, acceptedDistance);
                    }
                    LoopWhileDrivingToWaypoint(nextWaypoint, acceptedDistance);

                    if (collectedVehicle.SkipWaypoint)
                    {
                        collectedVehicle.SkipWaypoint = false;
                        continue;
                    }

                    if (!collectedVehicle.Dismissed && waypoints.ElementAtOrDefault(nextWaypoint) != null && waypoints[nextWaypoint].DrivingFlag == VehicleDrivingFlags.StopAtDestination)
                    {
                        StopVehicleAtWaypoint(waypoints[nextWaypoint], collectedVehicle);
                    }

                    if (!VehicleAndDriverNullChecks(collectedVehicle) || collectedVehicle.Dismissed)
                    {
                        return;
                    }
                    collectedVehicle.Driver.Tasks.PerformDrivingManeuver(collectedVehicle.Vehicle, VehicleManeuver.GoForwardWithCustomSteeringAngle, 3);
                }
            }

            void LoopWhileDrivingToWaypoint(int nextWaypoint, float acceptedDistance)
            {
                while (VehicleAndDriverNullChecks(collectedVehicle) && !collectedVehicle.Dismissed && !collectedVehicle.SkipWaypoint && waypoints.ElementAtOrDefault(nextWaypoint) != null && collectedVehicle.Vehicle.FrontPosition.DistanceTo2D(waypoints[nextWaypoint].Position) > acceptedDistance)
                {
                    //Logger.Log($"Looping while {collectedVehicle.Vehicle.Model.Name} drives to waypoint {waypoints[nextWaypoint].Number} ({collectedVehicle.Vehicle.DistanceTo2D(waypoints[nextWaypoint].Position)}m away from collector radius {waypoints[nextWaypoint].CollectorRadius})");
                    //Logger.Log($"Distance of front of vehicle to waypoint: {collectedVehicle.Vehicle.FrontPosition.DistanceTo2D(waypoints[nextWaypoint].Position)}");
                    GameFiber.Yield();
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
            Logger.Log($"Accepted distance: {acceptedDistance}");
            return acceptedDistance;
        }

        private static bool VehicleAndDriverNullChecks(CollectedVehicle collectedVehicle)
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

        private static bool VehicleAndDriverNullChecks(List<Waypoint> waypoints, int nextWaypoint, CollectedVehicle collectedVehicle)
        {
            if (waypoints.ElementAtOrDefault(nextWaypoint) == null)
            {
                Logger.Log($"Waypoint is null");
                return false;
            }
            if(collectedVehicle == null)
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
            if (!VehicleAndDriverNullChecks(collectedVehicle))
            {
                return;
            }

            var stoppingDistance = GetAcceptedStoppingDistance(currentWaypoint.Path.Waypoints, currentWaypoint.Path.Waypoints.IndexOf(currentWaypoint));
            Logger.Log($"{collectedVehicle.Vehicle.Model.Name} stopping at waypoint.");
            Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(collectedVehicle.Vehicle, stoppingDistance, -1, true);
            collectedVehicle.StoppedAtWaypoint = true;
            collectedVehicle.Driver.Tasks.Clear();


            while (currentWaypoint != null && VehicleAndDriverNullChecks(collectedVehicle) && collectedVehicle.StoppedAtWaypoint)
            {
                GameFiber.Yield();
            }
            Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(collectedVehicle.Vehicle, 1f, 1, true);
        }
    }
}
