using Rage;
using RAGENativeUI.Elements;
using SceneManager.Menus;
using SceneManager.Objects;
using System.Collections.Generic;
using System.Linq;

namespace SceneManager.Utils
{
    internal class PathManager
    {
        internal static List<Path> Paths { get; } = new List<Path>(10);

        internal static Path ImportPath(Path importedPath)
        {
            importedPath.State = State.Creating;

            var firstVacantIndex = Paths.IndexOf(Paths.FirstOrDefault(x => x.State != State.Creating)) + 1;
            if (firstVacantIndex < 0)
            {
                firstVacantIndex = 0;
            }
            var pathNumber = firstVacantIndex + 1;

            importedPath.Number = pathNumber;
            Paths.Insert(firstVacantIndex, importedPath);

            Game.LogTrivial($"Importing path {importedPath.Number} at Paths index {firstVacantIndex}");
            Game.DisplayNotification($"~o~Scene Manager ~y~[Importing]\n~w~Path {importedPath.Number} import started.");

            return importedPath;
        }

        internal static void ExportPath()
        {
            var currentPath = Paths[PathMainMenu.EditPath.Index];
            // Reference PNWParks's UserInput class from LiveLights
            var filename = UserInput.GetFileName("Type the name you would like to save your file as", "Enter a filename", 100) + ".xml";

            // If filename != null or empty, check if export directory exists (GTA V/Plugins/SceneManager/Saved Paths)
            if (string.IsNullOrWhiteSpace(filename))
            {
                Game.DisplayHelp($"Invalid filename given.  Filename cannot be null, empty, or consist of just white spaces.");
                Game.LogTrivial($"Invalid filename given.  Filename cannot be null, empty, or consist of just white spaces.");
                return;
            }
            Game.LogTrivial($"Filename: {filename}");
            currentPath.Save(filename);
            currentPath.Name = filename.Remove(filename.Length - 4);
            Game.LogTrivial($"Path name: {currentPath.Name}");
            Game.LogTrivial($"Exporting path {currentPath.Number}");
            Game.DisplayNotification($"~o~Scene Manager ~y~[Exporting]\n~w~Path {currentPath.Number} exported.");
            Settings.ImportPaths();
            PathMainMenu.ImportPath.Enabled = true;
            ImportPathMenu.BuildImportMenu();
        }

        internal static Path InitializeNewPath()
        {
            PathCreationMenu.PathCreationState = State.Creating;

            var firstVacantIndex = Paths.IndexOf(Paths.FirstOrDefault(x => x.State != State.Creating)) + 1;
            if(firstVacantIndex < 0)
            {
                firstVacantIndex = 0;
            }
            var pathNumber = firstVacantIndex + 1;

            Path newPath = new Path(pathNumber, State.Creating);
            Paths.Insert(firstVacantIndex, newPath);

            Game.LogTrivial($"Creating path {newPath.Number} at Paths index {firstVacantIndex}");
            Game.DisplayNotification($"~o~Scene Manager ~y~[Creating]\n~w~Path {newPath.Number} started.");

            PathCreationMenu.RemoveLastWaypoint.Enabled = false;
            PathCreationMenu.EndPathCreation.Enabled = false;

            return newPath;
        }

        internal static void AddWaypoint(Path currentPath)
        {
            var waypointNumber = currentPath.Waypoints.Count + 1;
            DrivingFlagType drivingFlag = PathCreationMenu.DirectWaypoint.Checked ? DrivingFlagType.Direct : DrivingFlagType.Normal;
            Waypoint newWaypoint;
            if (PathCreationMenu.CollectorWaypoint.Checked)
            {
                newWaypoint = new Waypoint(currentPath, waypointNumber, UserInput.GetMousePosition, SetDriveSpeedForWaypoint(), drivingFlag, PathCreationMenu.StopWaypoint.Checked, true, PathCreationMenu.CollectorRadius.Value, PathCreationMenu.SpeedZoneRadius.Value);
            }
            else
            {
                newWaypoint = new Waypoint(currentPath, waypointNumber, UserInput.GetMousePosition, SetDriveSpeedForWaypoint(), drivingFlag, PathCreationMenu.StopWaypoint.Checked);
            }
            currentPath.Waypoints.Add(newWaypoint);
            Game.LogTrivial($"Path {currentPath.Number} Waypoint {waypointNumber} added [Driving style: {drivingFlag} | Stop waypoint: {newWaypoint.IsStopWaypoint} | Speed: {newWaypoint.Speed} | Collector: {newWaypoint.IsCollector}]");

            if(currentPath.Waypoints.Count == 1)
            {
                PathMainMenu.CreateNewPath.Text = $"Continue Creating Path {currentPath.Number}";
            }
        }

        internal static void AddNewEditWaypoint(Path currentPath)
        {
            DrivingFlagType drivingFlag = EditWaypointMenu.DirectWaypointBehavior.Checked ? DrivingFlagType.Direct : DrivingFlagType.Normal;

            if (EditWaypointMenu.CollectorWaypoint.Checked)
            {
                currentPath.Waypoints.Add(new Waypoint(currentPath, currentPath.Waypoints.Last().Number + 1, UserInput.GetMousePosition, SetDriveSpeedForWaypoint(), drivingFlag, EditWaypointMenu.StopWaypointType.Checked, true, EditWaypointMenu.ChangeCollectorRadius.Value, EditWaypointMenu.ChangeSpeedZoneRadius.Value));
            }
            else
            {
                currentPath.Waypoints.Add(new Waypoint(currentPath, currentPath.Waypoints.Last().Number + 1, UserInput.GetMousePosition, SetDriveSpeedForWaypoint(), drivingFlag, EditWaypointMenu.StopWaypointType.Checked));
            }
            Game.LogTrivial($"New waypoint (#{currentPath.Waypoints.Last().Number}) added.");
        }

        internal static void UpdateWaypoint()
        {
            var currentPath = Paths[PathMainMenu.EditPath.Index];
            var currentWaypoint = currentPath.Waypoints[EditWaypointMenu.EditWaypoint.Index];
            DrivingFlagType drivingFlag = EditWaypointMenu.DirectWaypointBehavior.Checked ? DrivingFlagType.Direct : DrivingFlagType.Normal;

            if (currentPath.Waypoints.Count == 1)
            {
                currentWaypoint.UpdateWaypoint(currentWaypoint, UserInput.GetMousePosition, drivingFlag, EditWaypointMenu.StopWaypointType.Checked, SetDriveSpeedForWaypoint(), true, EditWaypointMenu.ChangeCollectorRadius.Value, EditWaypointMenu.ChangeSpeedZoneRadius.Value, EditWaypointMenu.UpdateWaypointPosition.Checked);
            }
            else
            {
                currentWaypoint.UpdateWaypoint(currentWaypoint, UserInput.GetMousePosition, drivingFlag, EditWaypointMenu.StopWaypointType.Checked, SetDriveSpeedForWaypoint(), EditWaypointMenu.CollectorWaypoint.Checked, EditWaypointMenu.ChangeCollectorRadius.Value, EditWaypointMenu.ChangeSpeedZoneRadius.Value, EditWaypointMenu.UpdateWaypointPosition.Checked);
            }
            Game.LogTrivial($"Path {currentPath.Number} Waypoint {currentWaypoint.Number} updated [Driving style: {drivingFlag} | Stop waypoint: {EditWaypointMenu.StopWaypointType.Checked} | Speed: {EditWaypointMenu.ChangeWaypointSpeed.Value} | Collector: {currentWaypoint.IsCollector}]");

            EditWaypointMenu.UpdateWaypointPosition.Checked = false;
            Game.DisplayNotification($"~o~Scene Manager ~g~[Success]~w~\nWaypoint {currentWaypoint.Number} updated.");
        }

        private static float SetDriveSpeedForWaypoint()
        {
            float convertedSpeed;
            if (SettingsMenu.SpeedUnits.SelectedItem == SpeedUnits.MPH)
            {
                //Logger.Log($"Original speed: {waypointSpeeds[waypointSpeed.Index]}{SettingsMenu.speedUnits.SelectedItem}");
                convertedSpeed = MathHelper.ConvertMilesPerHourToMetersPerSecond(PathCreationMenu.WaypointSpeed.Value);
                //Logger.Log($"Converted speed: {convertedSpeed}m/s");
            }
            else
            {
                //Logger.Log($"Original speed: {waypointSpeeds[waypointSpeed.Index]}{SettingsMenu.speedUnits.SelectedItem}");
                convertedSpeed = MathHelper.ConvertKilometersPerHourToMetersPerSecond(PathCreationMenu.WaypointSpeed.Value);
                //Logger.Log($"Converted speed: {convertedSpeed}m/s");
            }

            return convertedSpeed;
        }

        internal static void RemoveWaypoint(Path currentPath)
        {
            Waypoint lastWaypoint = currentPath.Waypoints.Last();
            lastWaypoint.Delete();
            currentPath.Waypoints.Remove(lastWaypoint);
        }

        internal static void RemoveEditWaypoint(Path currentPath)
        {
            var currentWaypoint = currentPath.Waypoints[EditWaypointMenu.EditWaypoint.Index];
            if (currentPath.Waypoints.Count == 1)
            {
                Game.LogTrivial($"Deleting the last waypoint from the path.");
                currentPath.Delete();
                Paths.Remove(currentPath);
                PathMainMenu.BuildPathMenu();

                EditWaypointMenu.Menu.Visible = false;
                PathMainMenu.Menu.Visible = true;
                return;
            }

            currentWaypoint.Delete();
            currentPath.Waypoints.Remove(currentWaypoint);
            Game.LogTrivial($"[Path {currentPath.Number}] Waypoint {currentWaypoint.Number} ({currentWaypoint.DrivingFlagType}) removed");
            currentPath.Waypoints.ForEach(x => x.Number = currentPath.Waypoints.IndexOf(x) + 1);

            DefaultWaypointToCollector(currentPath);
        }

        private static void DefaultWaypointToCollector(Path currentPath)
        {
            if (currentPath.Waypoints.Count == 1)
            {
                DrivingFlagType drivingFlag = EditWaypointMenu.DirectWaypointBehavior.Checked ? DrivingFlagType.Direct : DrivingFlagType.Normal;
                Hints.Display($"~o~Scene Manager ~y~[Hint]~w~\nYour path's first waypoint ~b~must~w~ be a collector.  If it's not, it will automatically be made into one.");
                Game.LogTrivial($"The path only has 1 waypoint left, this waypoint must be a collector.");
                currentPath.Waypoints[0].UpdateWaypoint(currentPath.Waypoints.First(), UserInput.GetMousePosition, drivingFlag, EditWaypointMenu.StopWaypointType.Checked, SetDriveSpeedForWaypoint(), true, EditWaypointMenu.ChangeCollectorRadius.Value, EditWaypointMenu.ChangeSpeedZoneRadius.Value, EditWaypointMenu.UpdateWaypointPosition.Checked);
                EditWaypointMenu.CollectorWaypoint.Checked = true;
                EditWaypointMenu.ChangeCollectorRadius.Enabled = true;
                EditWaypointMenu.ChangeSpeedZoneRadius.Enabled = true;
            }
        }

        internal static void EndPath(Path currentPath)
        {
            Game.LogTrivial($"[Path Creation] Path {currentPath.Number} finished with {currentPath.Waypoints.Count} waypoints.");
            Game.DisplayNotification($"~o~Scene Manager ~g~[Success]\n~w~Path {currentPath.Number} complete.");
            currentPath.State = State.Finished;
            currentPath.IsEnabled = true;
            currentPath.Waypoints.ForEach(x => x.EnableBlip());
            GameFiber.StartNew(() => currentPath.LoopForVehiclesToBeDismissed(), "Vehicle Cleanup Loop Fiber");
            GameFiber.StartNew(() => currentPath.LoopWaypointCollection(), "Waypoint Collection Loop Fiber");

            PathMainMenu.CreateNewPath.Text = "Create New Path";
            PathMainMenu.BuildPathMenu();
            PathMainMenu.Menu.Visible = true;
        }

        internal static void TogglePathCreationMenuItems(Path currentPath)
        {
            if(currentPath.Waypoints.Count == 1)
            {
                PathCreationMenu.CollectorWaypoint.Enabled = true;
                PathCreationMenu.CollectorWaypoint.Checked = false;
                PathCreationMenu.RemoveLastWaypoint.Enabled = true;
                PathCreationMenu.EndPathCreation.Enabled = true;
            }

            if (currentPath.Waypoints.Count < 1)
            {
                PathCreationMenu.CollectorWaypoint.Enabled = false;
                PathCreationMenu.CollectorWaypoint.Checked = true;
                PathCreationMenu.RemoveLastWaypoint.Enabled = false;
                PathCreationMenu.EndPathCreation.Enabled = false;
            }

            if (PathCreationMenu.CollectorWaypoint.Checked)
            {
                PathCreationMenu.CollectorRadius.Enabled = true;
                PathCreationMenu.SpeedZoneRadius.Enabled = true;
            }
            else
            {
                PathCreationMenu.CollectorRadius.Enabled = false;
                PathCreationMenu.SpeedZoneRadius.Enabled = false;
            }
        }

        internal static void ToggleBlips(bool enabled)
        {
            if (enabled)
            {
                Paths.SelectMany(x => x.Waypoints).ToList().ForEach(x => x.EnableBlip());
            }
            else
            {
                Paths.SelectMany(x => x.Waypoints).ToList().ForEach(x => x.DisableBlip());
            }
        }
    }
}
