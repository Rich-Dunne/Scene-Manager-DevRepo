using Rage;

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
            if (taskInVehicleBasic)
            {
                //Game.LogTrivial($"Ped is in a vehicle.");
                // Ped has a controlled driving task
                if (taskControlVehicle)
                {
                    //Game.LogTrivial($"Ped has a controlled driving task. (non-ambient)");
                    return false;
                }

                // Ped has a wander driving task
                if (taskCarDriveWander)
                {
                    //Game.LogTrivial($"Ped has a wander driving task. (ambient)");
                    return true;
                }

                // If the ped is in a vehicle but doesn't have a driving task, then it's a passenger.  Check if the vehicle's driver has a driving wander task
                if (ped.CurrentVehicle && ped.CurrentVehicle.Driver)
                {
                    var driverHasWanderTask = Rage.Native.NativeFunction.Natives.GET_IS_TASK_ACTIVE<bool>(ped.CurrentVehicle.Driver, 151);
                    if (driverHasWanderTask)
                    {
                        //Game.LogTrivial($"[Ambient Ped Check]: Ped is a passenger.  Vehicle's driver has a wander driving task. (ambient)");
                        return true;
                    }
                }
            }

            if (ped.IsOnFoot)
            {
                // UB unit on-foot, waiting for interaction
                if (ped.RelationshipGroup.Name == "UBCOP")
                {
                    //Game.LogTrivial($"Cop is UB unit. (non-ambient)");
                    return false;
                }

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
    }
}
