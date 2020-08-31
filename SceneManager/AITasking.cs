using Rage;
using System.Collections.Generic;
using System.Linq;

namespace SceneManager
{
    class AITasking
    {
        public static void AssignWaypointTasks(CollectedVehicle collectedVehicle, List<Waypoint> waypoints, Waypoint currentWaypoint)
        {
            if(!collectedVehicle.Vehicle || !collectedVehicle.Vehicle.Driver)
            {
                Game.LogTrivial($"Collected vehicle or driver is null");
                return;
            }

            if (currentWaypoint != null && currentWaypoint.DrivingFlag == VehicleDrivingFlags.StopAtDestination)
            {
                StopVehicleAtWaypoint(collectedVehicle);
            }

            DriveVehicleToNextWaypoint(collectedVehicle, waypoints, currentWaypoint);

            if (!collectedVehicle.Vehicle || !collectedVehicle.Vehicle.Driver)
            {
                Game.LogTrivial($"Collected vehicle or driver is null");
                return;
            }
            Game.LogTrivial($"{collectedVehicle.Vehicle.Model.Name} all tasks complete.");
            DismissDriver(collectedVehicle);
        }

        private static void DriveVehicleToNextWaypoint(CollectedVehicle collectedVehicle, List<Waypoint> waypoints, Waypoint currentWaypoint)
        {
            var vehicle = collectedVehicle.Vehicle;
            var driver = vehicle.Driver;

            for (int nextWaypoint = currentWaypoint.Number; nextWaypoint < waypoints.Count; nextWaypoint++)
            {
                if (waypoints.ElementAtOrDefault(nextWaypoint) == null)
                {
                    Game.LogTrivial($"Waypoint is null");
                    return;
                }
                if (!vehicle)
                {
                    Game.LogTrivial($"Vehicle is null");
                    return;
                }
                if (!driver)
                {
                    Game.LogTrivial($"Driver is null");
                    return;
                }

                Game.LogTrivial($"{vehicle.Model.Name} is driving to waypoint {waypoints[nextWaypoint].Number}");
                if (waypoints.ElementAtOrDefault(nextWaypoint) != null && !collectedVehicle.StoppedAtWaypoint)
                {
                    collectedVehicle.Vehicle.Driver.Tasks.DriveToPosition(waypoints[nextWaypoint].Position, waypoints[nextWaypoint].Speed, (VehicleDrivingFlags)263083, 2f).WaitForCompletion();
                    collectedVehicle.Vehicle.Driver.Tasks.PerformDrivingManeuver(collectedVehicle.Vehicle, VehicleManeuver.GoForwardWithCustomSteeringAngle, 3);
                }

                if (waypoints.ElementAtOrDefault(nextWaypoint) != null && waypoints[nextWaypoint].DrivingFlag == VehicleDrivingFlags.StopAtDestination)
                {
                    Game.LogTrivial($"{collectedVehicle.Vehicle.Model.Name} stopping at waypoint.");
                    collectedVehicle.Vehicle.Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraightBraking);
                    collectedVehicle.SetStoppedAtWaypoint(true);
                }
            }
        }

        private static void StopVehicleAtWaypoint(CollectedVehicle collectedVehicle)
        {
            Game.LogTrivial($"{collectedVehicle.Vehicle.Model.Name} stopping at waypoint.");
            collectedVehicle.Vehicle.Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraightBraking);
            collectedVehicle.SetStoppedAtWaypoint(true);
        }

        public static void DismissDriver(CollectedVehicle cv)
        {
            if (!cv.Vehicle)
            {
                Game.LogTrivial($"Vehicle is not valid after tasks completed.");
                return;
            }
            if (!cv.Vehicle.Driver)
            {
                Game.LogTrivial($"Driver is not valid after tasks completed.");
                return;
            }

            cv.SetDismissNow(true);
            cv.SetStoppedAtWaypoint(false);
            if (cv.Vehicle && cv.Vehicle.Driver)
            {
                cv.Vehicle.Driver.Dismiss();
                cv.Vehicle.Driver.Tasks.Clear();
                cv.Vehicle.Driver.BlockPermanentEvents = false;
                if (cv.Vehicle.Driver.GetAttachedBlip())
                {
                    cv.Vehicle.Driver.GetAttachedBlip().Delete();
                }
                cv.Vehicle.Driver.IsPersistent = false;

                cv.Vehicle.Dismiss();
                cv.Vehicle.IsPersistent = false;

                Game.LogTrivial($"{cv.Vehicle.Model.Name} dismissed successfully.");
            }
        }
    }
}
