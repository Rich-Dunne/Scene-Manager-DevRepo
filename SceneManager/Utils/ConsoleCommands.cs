using Rage;
using Rage.Attributes;
using Rage.ConsoleCommands.AutoCompleters;
using System.Linq;
using System;
using System.Collections.Generic;
using SceneManager.Managers;
using SceneManager.Paths;

namespace SceneManager.Utils
{
    internal static class ConsoleCommands
    {
        [ConsoleCommand("ShowCollectedVehicleInfo")]
        internal static void Command_ShowCollectedVehicleInfo([ConsoleCommandParameter(AutoCompleterType = typeof(ConsoleCommandAutoCompleterVehicle), Name = "ShowCollectedVehicleInfo")] Vehicle vehicle)
        {
            foreach(Path path in PathManager.Paths)
            {
                var collectedVehicle = path.CollectedPeds.Where(v => v.CurrentVehicle == vehicle).FirstOrDefault();
                if(collectedVehicle != null)
                {
                    Game.LogTrivial($"Vehicle: {collectedVehicle.CurrentVehicle.Model.Name} [{collectedVehicle.CurrentVehicle.Handle}]");
                    Rage.Native.NativeFunction.Natives.xA6E9C38DB51D7748(collectedVehicle.CurrentVehicle, out uint script);
                    Game.LogTrivial($"Vehicle spawned by: {script}");
                    Game.LogTrivial($"Driver handle: {collectedVehicle.Handle}");
                    Game.LogTrivial($"Path: {collectedVehicle.Path.Number}");
                    Game.LogTrivial($"Current waypoint: {collectedVehicle.CurrentWaypoint.Number}");
                    Game.LogTrivial($"StoppedAtWaypoint: {collectedVehicle.StoppedAtWaypoint}");
                    Game.LogTrivial($"SkipWaypoint: {collectedVehicle.SkipWaypoint}");
                    Game.LogTrivial($"ReadyForDirectTasks: {collectedVehicle.ReadyForDirectTasks}");
                    Game.LogTrivial($"Directed: {collectedVehicle.Directed}");
                    Game.LogTrivial($"Dismissed: {collectedVehicle.Dismissed}");
                    Game.LogTrivial($"Task status: {collectedVehicle.Tasks.CurrentTaskStatus}");
                    return;
                }
            }
            Game.LogTrivial($"{vehicle.Model.Name} [{vehicle.Handle}] was not found collected by any path.");
        }

        [ConsoleCommand("GetPedsActiveTasks")]
        internal static void Command_GetPedsActiveTasks([ConsoleCommandParameter(AutoCompleterType = typeof(ConsoleCommandAutoCompleterPedAliveOnly), Name = "GetPedsActiveTasks")] Ped ped)
        {
            var tasks = (PedTask[])Enum.GetValues(typeof(PedTask));
            foreach (PedTask task in tasks)
            {
                if(Rage.Native.NativeFunction.Natives.GET_IS_TASK_ACTIVE<bool>(ped, (int)task))
                {
                    Game.LogTrivial($"Ped [{ped.Handle}] active task: {task} ({(int)task})");
                }
            }
        }

        [ConsoleCommand("DeleteVehicle")]
        internal static void Command_DeleteVehicle([ConsoleCommandParameter(AutoCompleterType = typeof(ConsoleCommandAutoCompleterVehicle), Name = "Vehicle")] Vehicle vehicle)
        {
            if (vehicle)
            {
                vehicle.Delete();
            }
        }

        [ConsoleCommand("DeletePed")]
        internal static void Command_DeletePed([ConsoleCommandParameter(AutoCompleterType = typeof(ConsoleCommandAutoCompleterPed), Name = "Ped")] Ped ped)
        {
            if (ped && ped != Game.LocalPlayer.Character)
            {
                ped.Delete();
            }
        }
    }
}
