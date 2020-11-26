using Rage;
using Rage.Attributes;
using Rage.ConsoleCommands.AutoCompleters;
using System.Linq;

namespace SceneManager
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
    }
}
