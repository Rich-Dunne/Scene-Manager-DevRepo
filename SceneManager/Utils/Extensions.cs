using Rage;
using SceneManager.Managers;
using SceneManager.Paths;
using SceneManager.Waypoints;
using System;
using System.Linq;

namespace SceneManager.Utils
{
    internal enum PedType
    {
        /// <summary>Any ped
        /// </summary>
        Any = 0,
        /// <summary>Cop peds 
        /// </summary>
        Cop = 1,
        //Firefigher = 2,
        //EMS = 3
    }

    /// <summary>A collection of potentially useful code snippets for GTA/LSPDFR development. 
    /// </summary>
    internal static class Extensions
    {
        /// <summary>Determines if a ped can be considered ambient.  Checks any type of ped by default.
        /// </summary>
        internal static bool IsAmbient(this Ped ped, PedType pedType = 0)
        {
            // Universal tasks (virtually all peds seem have this)
            var taskAmbientClips = Rage.Native.NativeFunction.Natives.GET_IS_TASK_ACTIVE<bool>(ped, 38);

            // Universal on-foot tasks (virtually all ambient walking peds seem to have this)
            var taskComplexControlMovement = Rage.Native.NativeFunction.Natives.GET_IS_TASK_ACTIVE<bool>(ped, 35);

            // Universal in-vehicle tasks (virtually all ambient driver peds seem to have this)
            var taskInVehicleBasic = Rage.Native.NativeFunction.Natives.GET_IS_TASK_ACTIVE<bool>(ped, 150);
            var taskCarDriveWander = Rage.Native.NativeFunction.Natives.GET_IS_TASK_ACTIVE<bool>(ped, 151);

            // On-foot ambient tasks
            var taskPolice = Rage.Native.NativeFunction.Natives.GET_IS_TASK_ACTIVE<bool>(ped, 58); // From ambient cop (non-freemode) walking around
            var taskWanderingScenario = Rage.Native.NativeFunction.Natives.GET_IS_TASK_ACTIVE<bool>(ped, 100); // From ambient cop walking around
            var taskUseScenario = Rage.Native.NativeFunction.Natives.GET_IS_TASK_ACTIVE<bool>(ped, 118); // From ambient cop standing still
            var taskScriptedAnimation = Rage.Native.NativeFunction.Natives.GET_IS_TASK_ACTIVE<bool>(ped, 134); // From UB ped waiting for interaction

            // In-vehicle controlled tasks
            var taskControlVehicle = Rage.Native.NativeFunction.Natives.GET_IS_TASK_ACTIVE<bool>(ped, 169); // From backup unit driving to player

            // If ped relationship group does not contain "cop" then this extension doesn't apply
            if (pedType == PedType.Cop && !ped.RelationshipGroup.Name.ToLower().Contains("cop"))
            {
                //Game.LogTrivial($"Ped does not belong to a cop relationship group.");
                return false;
            }

            // Ped is in a vehicle
            if (ped.CurrentVehicle)
            {
                //Game.LogTrivial($"Ped is in a vehicle.");
                // Ped has a controlled driving task
                //if (taskControlVehicle)
                //{
                //    //Game.LogTrivial($"Ped has a controlled driving task. (non-ambient)");
                //    return false;
                //}

                // Ped has a wander driving task
                if (taskCarDriveWander)
                {
                    //Game.LogTrivial($"Ped has a wander driving task. (ambient)");
                    return true;
                }

                // If the ped is in a vehicle but doesn't have a driving task, then it's a passenger.  Check if the vehicle's driver has a driving wander task
                if (ped.CurrentVehicle.Driver && ped.CurrentVehicle.Driver != ped)
                {
                    var driverHasWanderTask = Rage.Native.NativeFunction.Natives.GET_IS_TASK_ACTIVE<bool>(ped.CurrentVehicle.Driver, 151);
                    if (driverHasWanderTask)
                    {
                        //Game.LogTrivial($"Ped is a passenger.  Vehicle's driver has a wander driving task. (ambient)");
                        return true;
                    }
                }
            }

            if (ped.IsOnFoot)
            {
                // Cop ped walking around or standing still
                if ((taskComplexControlMovement && taskWanderingScenario) || (taskAmbientClips && taskUseScenario))
                {
                    //Game.LogTrivial($"Ped is wandering around or standing still. (ambient)");
                    return true;
                }
            }

            // If nothing else returns true before now, then the ped is probably being controlled and doing something else
            //Game.LogTrivial($"Nothing else has returned true by this point. (non-ambient)");
            return false;
        }

        /// <summary>Determines if a vehicle and driver are valid.
        /// </summary>
        internal static bool VehicleAndDriverValid(this Vehicle vehicle)
        {
            if (vehicle && vehicle.HasDriver && vehicle.Driver && vehicle.Driver.IsAlive)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>Determines if this vehicle is within the waypoint's collection range.
        /// </summary>
        internal static bool IsNearCollectorWaypoint(this Vehicle vehicle, Waypoint waypoint)
        {
            if(!waypoint.IsCollector)
            {
                return false;
            }

            return vehicle.FrontPosition.DistanceTo2D(waypoint.Position) <= waypoint.CollectorRadius && Math.Abs(waypoint.Position.Z - vehicle.Position.Z) < 3;
        }

        internal static bool IsValidForPathCollection(this Vehicle vehicle, Path path)
        {
            if (!vehicle)
            {
                return false;
            }

            var vehicleCollectedOnAnotherPath = PathManager.Paths.Any(p => p != null && p.Number != path.Number && p.CollectedPeds.Any(cp => cp && cp.CurrentVehicle == vehicle));
            if (vehicleCollectedOnAnotherPath)
            {
                return false;
            }

            if (vehicle.Driver)
            {
                if (!vehicle.Driver.IsAlive)
                {
                    Game.LogTrivial($"Vehicle's driver is dead.");
                    path.BlacklistedVehicles.Add(vehicle);
                    return false;
                }
                if (vehicle.IsPoliceVehicle && !vehicle.Driver.IsAmbient())
                {
                    Game.LogTrivial($"Vehicle is a non-ambient police vehicle.");
                    path.BlacklistedVehicles.Add(vehicle);
                    return false;
                }
            }

            if (vehicle != Game.LocalPlayer.Character.LastVehicle && (vehicle.IsCar || vehicle.IsBike || vehicle.IsBicycle || vehicle.IsQuadBike) && !vehicle.IsSirenOn && vehicle.IsEngineOn && vehicle.IsOnAllWheels && vehicle.Speed > 1 && !path.CollectedPeds.Any(cp => cp && cp.CurrentVehicle == vehicle) && !path.BlacklistedVehicles.Contains(vehicle))
            { 
                if (!vehicle.HasDriver)
                {
                    vehicle.CreateRandomDriver();
                    while (vehicle && !vehicle.HasDriver)
                    {
                        GameFiber.Yield();
                    }
                    if(!vehicle || !vehicle.Driver)
                    {
                        return false;
                    }

                    vehicle.Driver.IsPersistent = true;
                    vehicle.Driver.BlockPermanentEvents = true;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
