//using Rage;
//using System.Collections.Generic;
//using System.Linq;

//namespace SceneManager
//{
//    // Driving styles https://gtaforums.com/topic/822314-guide-driving-styles/
//    // also https://vespura.com/fivem/drivingstyle/

//    class AITasking
//    {
//        internal static void AssignWaypointTasks(CollectedVehicle collectedVehicle, Path path, Waypoint currentWaypoint)
//        {
//            if (!VehicleAndDriverAreValid(collectedVehicle))
//            {
//                return;
//            }

//            collectedVehicle.Path = path;
//            if(currentWaypoint != null)
//            {
//                collectedVehicle.CurrentWaypoint = currentWaypoint;
//            }
//            else
//            {
//                collectedVehicle.CurrentWaypoint = path.Waypoints[0];
//            }

//            if (currentWaypoint != null && collectedVehicle.Directed)
//            {
//                collectedVehicle.Dismissed = false;

//                while (!collectedVehicle.ReadyForDirectTasks)
//                {
//                    GameFiber.Yield();
//                }
//                if (!VehicleAndDriverAreValid(collectedVehicle))
//                {
//                    return;
//                }
//                collectedVehicle.Driver.Tasks.Clear();
//                collectedVehicle.DriveToDirectedWaypoint(currentWaypoint);
//            }

//            if (currentWaypoint.IsStopWaypoint)
//            {
//                collectedVehicle.StopAtWaypoint(currentWaypoint);
//            }
//            if(path?.Waypoints?.Count > 0 && currentWaypoint != path?.Waypoints?.Last())
//            {
//                collectedVehicle.DriveToNextWaypoint();
//            }

//            if (!VehicleAndDriverAreValid(collectedVehicle) || collectedVehicle.Directed)
//            {
//                return;
//            }
//            Game.LogTrivial($"{collectedVehicle.Vehicle.Model.Name} all Path {path.Number} tasks complete.");
//            if(!collectedVehicle.Dismissed)
//            {
//                collectedVehicle.Dismiss();
//            }
//        }

//        private static bool VehicleAndDriverAreValid(CollectedVehicle collectedVehicle)
//        {
//            if (collectedVehicle == null)
//            {
//                Game.LogTrivial($"CollectedVehicle is null");
//                return false;
//            }
//            if (!collectedVehicle.Vehicle && !collectedVehicle.Dismissed)
//            {
//                Game.LogTrivial($"Vehicle is null");
//                collectedVehicle.Dismiss();
//                return false;
//            }
//            if (collectedVehicle.Driver == null || !collectedVehicle.Driver || !collectedVehicle.Driver.IsAlive && !collectedVehicle.Dismissed)
//            {
//                collectedVehicle.Dismiss();
//                return false;
//            }
//            return true;
//        }
//    }
//}
