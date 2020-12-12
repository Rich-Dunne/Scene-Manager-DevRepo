using Rage;
using Rage.Attributes;
using Rage.ConsoleCommands.AutoCompleters;
using System.Linq;
using SceneManager.Objects;
using System;
using System.Collections.Generic;

namespace SceneManager.Utils
{
    internal static class ConsoleCommands
    {
        [ConsoleCommand]
        internal static void Command_ShowCollectedVehicleInfo([ConsoleCommandParameter(AutoCompleterType = typeof(ConsoleCommandAutoCompleterVehicle))] Vehicle vehicle)
        {
            foreach(Path path in PathMainMenu.paths)
            {
                var collectedVehicle = path.CollectedVehicles.Where(v => v.Vehicle == vehicle).FirstOrDefault();
                if(collectedVehicle != null)
                {
                    Game.LogTrivial($"Vehicle: {collectedVehicle.Vehicle.Model.Name} [{collectedVehicle.Vehicle.Handle}]");
                    Rage.Native.NativeFunction.Natives.xA6E9C38DB51D7748(collectedVehicle.Vehicle, out uint script);
                    Game.LogTrivial($"Vehicle spawned by: {script}");
                    Game.LogTrivial($"Driver handle: {collectedVehicle.Driver.Handle}");
                    Game.LogTrivial($"Path: {collectedVehicle.Path.Number}");
                    Game.LogTrivial($"Current waypoint: {collectedVehicle.CurrentWaypoint.Number}");
                    Game.LogTrivial($"StoppedAtWaypoint: {collectedVehicle.StoppedAtWaypoint}");
                    Game.LogTrivial($"SkipWaypoint: {collectedVehicle.SkipWaypoint}");
                    Game.LogTrivial($"ReadyForDirectTasks: {collectedVehicle.ReadyForDirectTasks}");
                    Game.LogTrivial($"Directed: {collectedVehicle.Directed}");
                    Game.LogTrivial($"Dismissed: {collectedVehicle.Dismissed}");
                    Game.LogTrivial($"Task status: {collectedVehicle.Driver.Tasks.CurrentTaskStatus}");
                    return;
                }
            }
            Game.LogTrivial($"{vehicle.Model.Name} [{vehicle.Handle}] was not found collected by any path.");
        }

        [ConsoleCommand]
        internal static void Command_GetPedsActiveTasks([ConsoleCommandParameter(AutoCompleterType = typeof(ConsoleCommandAutoCompleterPedAliveOnly))] Ped ped)
        {
            var tasks = new List<PedTask>();
            foreach (PedTask task in (PedTask[])Enum.GetValues(typeof(PedTask)))
            {
                if(Rage.Native.NativeFunction.Natives.GET_IS_TASK_ACTIVE<bool>(ped, (int)task))
                {
                    Game.LogTrivial($"Ped [{ped.Handle}] active task: {task} ({(int)task})");
                }
            }
        }
    }
}
