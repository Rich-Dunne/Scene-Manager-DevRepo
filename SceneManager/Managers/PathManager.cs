using Rage;
using SceneManager.Menus;
using SceneManager.Utils;
using SceneManager.Waypoints;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SceneManager.Managers
{
    internal class PathManager
    {
        internal static Paths.Path[] Paths { get; } = new Paths.Path[10];
        internal static Dictionary<string, List<Paths.Path>> ImportedPaths { get; } = new Dictionary<string, List<Paths.Path>>();

        internal static Paths.Path ImportPath(Paths.Path importedPath)
        {
            importedPath.State = State.Creating;
            var firstVacantIndex = Array.IndexOf(Paths, Paths.First(x => x == null));
            if (firstVacantIndex < 0)
            {
                firstVacantIndex = 0;
            }
            var pathNumber = firstVacantIndex + 1;

            importedPath.Number = pathNumber;
            Paths[firstVacantIndex] = importedPath;

            Game.LogTrivial($"Importing {importedPath.Name} at Paths index {firstVacantIndex}");
            Game.DisplayNotification($"~o~Scene Manager ~y~[Importing]\n~w~Importing path: ~b~{importedPath.Name} ~w~.");

            return importedPath;
        }

        internal static void ExportPath()
        {
            var currentPath = Paths[PathMainMenu.EditPath.Index];

            // If the path is in the import menu, autosave with the same name.
            if(ImportPathMenu.Menu.MenuItems.Any(x=> x.Text == currentPath.Name))
            {
                Game.LogTrivial($"Autosaving {currentPath.Name}");
                currentPath.Save();
            }
            else
            {
                // Reference PNWParks's UserInput class from LiveLights
                var filename = UserInput.PromptPlayerForFileName("Type the name you would like to save your file as", "Enter a filename", 100);

                // If filename != null or empty, check if export directory exists (GTA V/Plugins/SceneManager/Saved Paths)
                if (string.IsNullOrWhiteSpace(filename))
                {
                    Game.DisplayHelp($"Invalid filename given.  Filename cannot be null, empty, or consist of just white spaces.");
                    Game.LogTrivial($"Invalid filename given.  Filename cannot be null, empty, or consist of just white spaces.");
                    return;
                }

                Game.LogTrivial($"Filename: {filename}");
                currentPath.Name = filename;
                currentPath.Save();
            }

            PathMainMenu.ImportPath.Enabled = true;
            ImportPathMenu.Build();
            PathMainMenu.Build();
            BarrierMenu.Build();

            PathMainMenu.Menu.Visible = true;
        }

        internal static Paths.Path InitializeNewPath()
        {
            PathCreationMenu.PathCreationState = State.Creating;

            Paths.Path newPath = new Paths.Path();
            var firstEmptyIndex = Array.IndexOf(Paths, Paths.First(x => x == null));
            Game.LogTrivial($"First empty index: {firstEmptyIndex}");
            Paths[firstEmptyIndex] = newPath;
            newPath.Name = newPath.Number.ToString();

            PathMainMenu.CreateNewPath.Text = $"Continue Creating Path {newPath.Name}";
            Game.LogTrivial($"Creating path {newPath.Name} at Paths[{firstEmptyIndex}]");
            Game.DisplayNotification($"~o~Scene Manager ~y~[Creating]\n~w~Path ~b~{newPath.Name} ~w~started.");

            PathCreationMenu.RemoveLastWaypoint.Enabled = false;
            PathCreationMenu.EndPathCreation.Enabled = false;

            return newPath;
        }

        internal static void AddNewEditWaypoint(Paths.Path currentPath)
        {
            DrivingFlagType drivingFlag = EditWaypointMenu.DirectWaypointBehavior.Checked ? DrivingFlagType.Direct : DrivingFlagType.Normal;

            if (EditWaypointMenu.CollectorWaypoint.Checked)
            {
                currentPath.Waypoints.Add(new Waypoint(currentPath, UserInput.PlayerMousePosition, ConvertDriveSpeedForWaypoint(EditWaypointMenu.ChangeWaypointSpeed.Value), drivingFlag, EditWaypointMenu.StopWaypointType.Checked, true, EditWaypointMenu.ChangeCollectorRadius.Value, EditWaypointMenu.ChangeSpeedZoneRadius.Value));
            }
            else
            {
                currentPath.Waypoints.Add(new Waypoint(currentPath, UserInput.PlayerMousePosition, ConvertDriveSpeedForWaypoint(EditWaypointMenu.ChangeWaypointSpeed.Value), drivingFlag, EditWaypointMenu.StopWaypointType.Checked));
            }
            Game.LogTrivial($"New waypoint (#{currentPath.Waypoints.Last().Number}) added.");
        }

        internal static void UpdateWaypoint()
        {
            var currentPath = Paths.FirstOrDefault(x => x.Name == PathMainMenu.EditPath.OptionText);
            var currentWaypoint = currentPath.Waypoints[EditWaypointMenu.EditWaypoint.Index];
            DrivingFlagType drivingFlag = EditWaypointMenu.DirectWaypointBehavior.Checked ? DrivingFlagType.Direct : DrivingFlagType.Normal;

            if (currentPath.Waypoints.Count == 1)
            {
                currentWaypoint.UpdateWaypoint(currentWaypoint, UserInput.PlayerMousePosition, drivingFlag, EditWaypointMenu.StopWaypointType.Checked, ConvertDriveSpeedForWaypoint(EditWaypointMenu.ChangeWaypointSpeed.Value), true, EditWaypointMenu.ChangeCollectorRadius.Value, EditWaypointMenu.ChangeSpeedZoneRadius.Value, EditWaypointMenu.UpdateWaypointPosition.Checked);
            }
            else
            {
                currentWaypoint.UpdateWaypoint(currentWaypoint, UserInput.PlayerMousePosition, drivingFlag, EditWaypointMenu.StopWaypointType.Checked, ConvertDriveSpeedForWaypoint(EditWaypointMenu.ChangeWaypointSpeed.Value), EditWaypointMenu.CollectorWaypoint.Checked, EditWaypointMenu.ChangeCollectorRadius.Value, EditWaypointMenu.ChangeSpeedZoneRadius.Value, EditWaypointMenu.UpdateWaypointPosition.Checked);
            }
            Game.LogTrivial($"Path {currentPath.Number} Waypoint {currentWaypoint.Number} updated [Driving style: {drivingFlag} | Stop waypoint: {EditWaypointMenu.StopWaypointType.Checked} | Speed: {EditWaypointMenu.ChangeWaypointSpeed.Value} | Collector: {currentWaypoint.IsCollector}]");

            EditWaypointMenu.UpdateWaypointPosition.Checked = false;
            Game.DisplayNotification($"~o~Scene Manager ~g~[Success]~w~\nWaypoint {currentWaypoint.Number} updated.");
        }

        private static float ConvertDriveSpeedForWaypoint(float speed)
        {
            float convertedSpeed = SettingsMenu.SpeedUnits.SelectedItem == SpeedUnits.MPH
                ? MathHelper.ConvertMilesPerHourToMetersPerSecond(speed)
                : MathHelper.ConvertKilometersPerHourToMetersPerSecond(speed);
            return convertedSpeed;
        }

        internal static void RemoveEditWaypoint(Paths.Path currentPath)
        {
            var currentWaypoint = currentPath.Waypoints[EditWaypointMenu.EditWaypoint.Index];
            if (currentPath.Waypoints.Count == 1)
            {
                Game.LogTrivial($"Deleting the last waypoint from the path.");
                currentPath.Delete();
                Paths[Array.IndexOf(Paths, currentPath)] = null;
                PathMainMenu.Build();

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

        private static void DefaultWaypointToCollector(Paths.Path currentPath)
        {
            if (currentPath.Waypoints.Count == 1)
            {
                DrivingFlagType drivingFlag = EditWaypointMenu.DirectWaypointBehavior.Checked ? DrivingFlagType.Direct : DrivingFlagType.Normal;
                Hints.Display($"~o~Scene Manager ~y~[Hint]~w~\nYour path's first waypoint ~b~must~w~ be a collector.  If it's not, it will automatically be made into one.");
                Game.LogTrivial($"The path only has 1 waypoint left, this waypoint must be a collector.");
                currentPath.Waypoints[0].UpdateWaypoint(currentPath.Waypoints.First(), UserInput.PlayerMousePosition, drivingFlag, EditWaypointMenu.StopWaypointType.Checked, ConvertDriveSpeedForWaypoint(EditWaypointMenu.ChangeWaypointSpeed.Value), true, EditWaypointMenu.ChangeCollectorRadius.Value, EditWaypointMenu.ChangeSpeedZoneRadius.Value, EditWaypointMenu.UpdateWaypointPosition.Checked);
                EditWaypointMenu.CollectorWaypoint.Checked = true;
                EditWaypointMenu.ChangeCollectorRadius.Enabled = true;
                EditWaypointMenu.ChangeSpeedZoneRadius.Enabled = true;
            }
        }

        internal static void TogglePathCreationMenuItems(Paths.Path currentPath)
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
                PathCreationMenu.PathName.Enabled = false;
                PathCreationMenu.PathName.Description = "Add your first waypoint to enable this option.";
                currentPath.Delete();

                PathCreationMenu.PathCreationState = State.Uninitialized;
                PathMainMenu.CreateNewPath.Text = "Create New Path";
                PathMainMenu.Build();
                GameFiber.Yield();
                PathCreationMenu.Menu.Visible = true;
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

        internal static void ToggleAllPaths(bool disable)
        {
            var nonNullPaths = Paths.Where(x => x != null).ToList();
            if (disable)
            {
                nonNullPaths.ForEach(x => x.Disable());
                Game.LogTrivial($"All paths disabled.");
            }
            else
            {
                nonNullPaths.ForEach(x => x.Enable());
                Game.LogTrivial($"All paths enabled.");
            }
        }

        internal static void DeleteAllPaths()
        {
            for(int i = 0; i < Paths.Length; i++)
            {
                if(Paths[i] != null)
                {
                    Paths[i].Delete();
                }
            }
            Array.Clear(Paths, 0, Paths.Length);
            //Paths.Clear();
            Game.LogTrivial($"All paths deleted");
            Game.DisplayNotification($"~o~Scene Manager\n~w~All paths deleted.");
            MainMenu.Build();
        }

        internal static void ImportPaths()
        {
            ImportedPaths.Clear();

            // Check if Saved Paths directory exists
            var GAME_DIRECTORY = Directory.GetCurrentDirectory();
            var SAVED_PATHS_DIRECTORY = GAME_DIRECTORY + "\\plugins\\SceneManager\\Saved Paths\\";
            if (!Directory.Exists(SAVED_PATHS_DIRECTORY))
            {
                Game.LogTrivial($"Directory '\\plugins\\SceneManager\\Saved Paths' does not exist.  No paths available to import.");
                return;
            }

            // Check if any XML files are available to import from Saved Paths
            var savedPathNames = Directory.GetFiles(SAVED_PATHS_DIRECTORY, "*.xml");
            if (savedPathNames.Length == 0)
            {
                Game.LogTrivial($"No saved paths found.");
                return;
            }
            else
            {
                Game.LogTrivial($"{savedPathNames.Length} path(s) available to import.");
            }

            // Import paths
            foreach (string pathName in savedPathNames)
            {
                var fileName = Path.GetFileNameWithoutExtension(pathName);
                Game.LogTrivial($"File: {fileName}");

                var overrides = DefineOverridesForCombinedPath();
                var paths = Serializer.LoadItemFromXML<List<Paths.Path>>(SAVED_PATHS_DIRECTORY + Path.GetFileName(pathName), overrides);
                ImportedPaths.Add(fileName, paths);
            }
            Game.LogTrivial($"Successfully imported {ImportedPaths.Count} file(s).");
        }

        private static XmlAttributeOverrides DefineOverridesForCombinedPath()
        {
            XmlAttributeOverrides overrides = new XmlAttributeOverrides();
            XmlAttributes attr = new XmlAttributes();
            attr.XmlRoot = new XmlRootAttribute("Paths");
            overrides.Add(typeof(List<Paths.Path>), attr);

            return overrides;
        }
    }
}
