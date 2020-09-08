using Rage;
using System.Collections.Generic;
using System.Linq;

namespace SceneManager
{
    class AITasking
    {
        public static void AssignWaypointTasks(CollectedVehicle collectedVehicle, List<Waypoint> waypoints, Waypoint currentWaypoint)
        {
            if (!VehicleAndDriverNullChecks(collectedVehicle))
            {
                return;
            }

            if (currentWaypoint != null && currentWaypoint.DrivingFlag == VehicleDrivingFlags.StopAtDestination)
            {
                StopVehicleAtWaypoint(currentWaypoint, collectedVehicle);
            }

            DriveVehicleToNextWaypoint(collectedVehicle, waypoints, currentWaypoint);

            if (!VehicleAndDriverNullChecks(collectedVehicle))
            {
                return;
            }
            Game.LogTrivial($"{collectedVehicle.Vehicle.Model.Name} all tasks complete.");
            DismissDriver(collectedVehicle);
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

                Game.LogTrivial($"{vehicle.Model.Name} is driving to waypoint {waypoints[nextWaypoint].Number}");
                if (waypoints.ElementAtOrDefault(nextWaypoint) != null && !collectedVehicle.StoppedAtWaypoint)
                {
                    if(waypoints[nextWaypoint].DrivingFlag == VehicleDrivingFlags.Normal)
                    {
                        collectedVehicle.Vehicle.Driver.Tasks.DriveToPosition(waypoints[nextWaypoint].Position, waypoints[nextWaypoint].Speed, (VehicleDrivingFlags)263083, 2f).WaitForCompletion();
                    }
                    else if (waypoints[nextWaypoint].DrivingFlag == VehicleDrivingFlags.IgnorePathFinding)
                    {
                        collectedVehicle.Vehicle.Driver.Tasks.DriveToPosition(waypoints[nextWaypoint].Position, waypoints[nextWaypoint].Speed, (VehicleDrivingFlags)17040299, 2f).WaitForCompletion();
                    }

                    collectedVehicle.Vehicle.Driver.Tasks.PerformDrivingManeuver(collectedVehicle.Vehicle, VehicleManeuver.GoForwardWithCustomSteeringAngle, 3);
                }

                if (waypoints.ElementAtOrDefault(nextWaypoint) != null && waypoints[nextWaypoint].DrivingFlag == VehicleDrivingFlags.StopAtDestination)
                {
                    Game.LogTrivial($"{collectedVehicle.Vehicle.Model.Name} stopping at waypoint.");
                    collectedVehicle.Vehicle.Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraightBraking);
                    collectedVehicle.StoppedAtWaypoint = true;
                }
            }
        }

        private static bool VehicleAndDriverNullChecks(CollectedVehicle collectedVehicle)
        {
            if (collectedVehicle == null)
            {
                Game.LogTrivial($"CollectedVehicle is null");
                return false;
            }
            if (!collectedVehicle.Vehicle)
            {
                Game.LogTrivial($"Vehicle is null");
                return false;
            }
            if (!collectedVehicle.Driver)
            {
                Game.LogTrivial($"Driver is null");
                return false;
            }
            return true;
        }

        private static bool VehicleAndDriverNullChecks(List<Waypoint> waypoints, int nextWaypoint, CollectedVehicle collectedVehicle)
        {
            if (waypoints.ElementAtOrDefault(nextWaypoint) == null)
            {
                Game.LogTrivial($"Waypoint is null");
                return false;
            }
            if(collectedVehicle == null)
            {
                Game.LogTrivial($"CollectedVehicle is null");
                return false;
            }
            if (!collectedVehicle.Vehicle)
            {
                Game.LogTrivial($"Vehicle is null");
                return false;
            }
            if (!collectedVehicle.Driver)
            {
                Game.LogTrivial($"Driver is null");
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

            Game.LogTrivial($"{collectedVehicle.Vehicle.Model.Name} stopping at waypoint.");
            //collectedVehicle.Driver.Tasks.DriveToPosition(collectedVehicle.Vehicle.FrontPosition, 10f, (VehicleDrivingFlags)2147483648); // This causes FPS loss
            Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(collectedVehicle.Vehicle, 3f, -1, true);
            collectedVehicle.StoppedAtWaypoint = true;

            while (currentWaypoint != null && collectedVehicle.Vehicle && collectedVehicle.StoppedAtWaypoint)
            {
                GameFiber.Yield();
            }
            Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(collectedVehicle.Vehicle, 3f, 1, true);
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

            cv.StoppedAtWaypoint = false;
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
                cv.Vehicle.IsSirenOn = false;
                cv.Vehicle.IsSirenSilent = true;
                cv.Vehicle.IsPersistent = false;

                Game.LogTrivial($"{cv.Vehicle.Model.Name} dismissed successfully.");
            }
        }
    }
}
