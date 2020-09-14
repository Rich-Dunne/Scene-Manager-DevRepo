using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    static class PathMainMenu
    {
        private static List<Path> paths = new List<Path>() { };

        public static UIMenu pathMainMenu { get; private set; }
        public static UIMenuItem createNewPath { get; private set; }
        public static UIMenuItem deleteAllPaths;
        public static UIMenuNumericScrollerItem<int> editPath = new UIMenuNumericScrollerItem<int>("Edit Path", "", 1, paths.Count, 1);
        public static UIMenuListScrollerItem<string> directOptions { get; private set; }
        public static UIMenuNumericScrollerItem<int> directDriver = new UIMenuNumericScrollerItem<int>("Direct nearest driver to path", "", 1, paths.Count, 1);
        public static UIMenuListScrollerItem<string> dismissDriver { get; private set; }
        public static UIMenuCheckboxItem disableAllPaths { get; private set; }

        private static List<string> dismissOptions = new List<string>() { "From path", "From waypoint", "From position" };
        public enum Delete
        {
            Single,
            All
        }

        internal static void InstantiateMenu()
        {
            pathMainMenu = new UIMenu("Scene Manager", "~o~Path Manager Main Menu");
            pathMainMenu.ParentMenu = MainMenu.mainMenu;
            MenuManager.menuPool.Add(pathMainMenu);
        }

        public static void BuildPathMenu()
        {
            // Need to unsubscribe from events, else there will be duplicate firings if the user left the menu and re-entered
            ResetEventHandlerSubscriptions();

            MenuManager.menuPool.CloseAllMenus();
            pathMainMenu.Clear();

            pathMainMenu.AddItem(createNewPath = new UIMenuItem("Create New Path"));
            createNewPath.ForeColor = Color.Gold;
            pathMainMenu.AddItem(editPath = new UIMenuNumericScrollerItem<int>("Edit Path", "", 1, paths.Count, 1));
            editPath.ForeColor = Color.Gold;
            pathMainMenu.AddItem(disableAllPaths = new UIMenuCheckboxItem("Disable All Paths", false));
            pathMainMenu.AddItem(deleteAllPaths = new UIMenuItem("Delete All Paths"));
            deleteAllPaths.ForeColor = Color.Gold;
            pathMainMenu.AddItem(directOptions = new UIMenuListScrollerItem<string>("Direct driver to path's", "", new[] { "First waypoint", "Nearest waypoint" }));
            pathMainMenu.AddItem(directDriver = new UIMenuNumericScrollerItem<int>("Direct nearest driver to path", "", 1, paths.Count, 1));
            directDriver.ForeColor = Color.Gold;
            pathMainMenu.AddItem(dismissDriver = new UIMenuListScrollerItem<string>("Dismiss nearest driver", $"~b~From path: ~w~AI will be released from the path{Environment.NewLine}~b~From waypoint: ~w~AI will skip their current waypoint task{Environment.NewLine}~b~From position: ~w~AI will be released from current position.  This can be used for stuck vehicles, and is the default behavior for vehicles not collected by a path.", dismissOptions));
            dismissDriver.ForeColor = Color.Gold;

            if (paths.Count == 8)
            {
                createNewPath.Enabled = false;
            }
            if (paths.Count == 0)
            {
                editPath.Enabled = false;
                deleteAllPaths.Enabled = false;
                disableAllPaths.Enabled = false;
                directDriver.Enabled = false;
            }

            pathMainMenu.RefreshIndex();

            MenuManager.menuPool.RefreshIndex();
        }

        private static void ResetEventHandlerSubscriptions()
        {
            pathMainMenu.OnItemSelect -= PathMenu_OnItemSelected;
            pathMainMenu.OnCheckboxChange -= PathMenu_OnCheckboxChange;
            pathMainMenu.OnItemSelect += PathMenu_OnItemSelected;
            pathMainMenu.OnCheckboxChange += PathMenu_OnCheckboxChange;
        }

        public static ref List<Path> GetPaths()
        {
            return ref paths;
        }

        private static bool VehicleAndDriverValid(this Vehicle v)
        {
            if (v && v.HasDriver && v.Driver && v.Driver.IsAlive)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void DeletePath(Path path, Delete pathsToDelete)
        {
            // Before deleting a path, we need to dismiss any vehicles controlled by that path and remove the vehicles from ControlledVehicles
            Game.LogTrivial($"Deleting path {path.Number}");
            var pathVehicles = VehicleCollector.collectedVehicles.Where(cv => cv.Path.Number == path.Number).ToList();

            Game.LogTrivial($"Removing all vehicles on the path");
            foreach (CollectedVehicle cv in pathVehicles.Where(cv => cv != null && cv.Vehicle && cv.Driver))
            {
                if (cv.StoppedAtWaypoint)
                {
                    Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(cv.Vehicle, 1f, 1, true);
                }
                cv.StoppedAtWaypoint = false;
                cv.Driver.Tasks.Clear();
                cv.Driver.Dismiss();
                cv.Vehicle.IsSirenOn = false;
                cv.Vehicle.IsSirenSilent = true;
                cv.Vehicle.Dismiss();

                //Game.LogTrivial($"{cv.vehicle.Model.Name} cleared from path {cv.path}");
                VehicleCollector.collectedVehicles.Remove(cv);
            }

            // Remove the speed zone so cars don't continue to be affected after the path is deleted
            Game.LogTrivial($"Removing yield zone and waypoint blips");
            foreach (Waypoint waypoint in path.Waypoints)
            {
                if (waypoint.SpeedZone != 0)
                {
                    waypoint.RemoveSpeedZone();
                }
                if (waypoint.Blip)
                {
                    waypoint.Blip.Delete();
                }
                if (waypoint.CollectorRadiusBlip)
                {
                    waypoint.CollectorRadiusBlip.Delete();
                }
            }

            Game.LogTrivial($"Clearing path.WaypointData");
            path.Waypoints.Clear();

            // Manipulating the menu to reflect specific paths being deleted
            if (pathsToDelete == Delete.Single)
            {
                paths.Remove(path);
                BuildPathMenu();
                pathMainMenu.Visible = true;
                Game.LogTrivial($"Path {path.Number} deleted.");
                Game.DisplayNotification($"~o~Scene Manager\n~w~Path {path.Number} deleted.");
            }

            EditPathMenu.editPathMenu.Reset(true, true);
            EditPathMenu.disablePath.Enabled = true;
        }

        private static void PathMenu_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == createNewPath)
            {
                pathMainMenu.Visible = false;
                PathCreationMenu.pathCreationMenu.Visible = true;
                Draw3DWaypointOnPlayer();

                // For each element in paths, determine if the element exists but is not finished yet, or if it doesn't exist, create it.
                for (int i = 0; i <= paths.Count; i++)
                {
                    if (paths.ElementAtOrDefault(i) != null && paths[i].State == State.Creating)
                    {
                        Game.LogTrivial($"Resuming path {paths[i].Number}");
                        Game.DisplayNotification($"~o~Scene Manager\n~y~[Creating]~w~ Resuming path {paths[i].Number}");
                        break;
                    }
                }
            }

            if (selectedItem == editPath)
            {
                pathMainMenu.Visible = false;
                EditPathMenu.editPathMenu.Visible = true;
            }

            if (selectedItem == deleteAllPaths)
            {
                // Iterate through each item in paths and delete it
                for (int i = 0; i < paths.Count; i++)
                {
                    DeletePath(paths[i], Delete.All);
                }
                foreach (Path path in paths)
                {
                    foreach(Waypoint waypoint in path.Waypoints.Where(wp => wp.SpeedZone != 0))
                    {
                        waypoint.RemoveSpeedZone();
                    }
                    path.Waypoints.Clear();
                }
                paths.Clear();
                BuildPathMenu();
                pathMainMenu.Visible = true;
                Game.LogTrivial($"All paths deleted");
                Game.DisplayNotification($"~o~Scene Manager\n~w~All paths deleted.");
            }

            if (selectedItem == directDriver)
            {
                var nearbyVehicle = Game.LocalPlayer.Character.GetNearbyVehicles(1).Where(v => v.VehicleAndDriverValid()).SingleOrDefault();
                CollectedVehicle collectedVehicle;

                if (nearbyVehicle)
                {
                    collectedVehicle = VehicleCollector.collectedVehicles.Where(cv => cv.Vehicle == nearbyVehicle).FirstOrDefault();
                    var path = paths[directDriver.Index];
                    var waypoints = path.Waypoints;
                    var firstWaypoint = waypoints.First();
                    var nearestWaypoint = waypoints.Where(wp => wp.Position.DistanceTo2D(nearbyVehicle.FrontPosition) < wp.Position.DistanceTo2D(nearbyVehicle.RearPosition)).OrderBy(wp => wp.Position.DistanceTo2D(nearbyVehicle)).FirstOrDefault();

                    VehicleCollector.SetVehicleAndDriverPersistence(nearbyVehicle);

                    // The vehicle should only be added to the collection when it's not null AND if the selected item is First Waypoint OR if the selected item is nearestWaypoint AND nearestWaypoint is not null
                    if (collectedVehicle == null && (directOptions.SelectedItem == "First waypoint" || directOptions.SelectedItem == "Nearest waypoint" && nearestWaypoint != null))
                    {
                        Game.LogTrivial($"[Direct Driver] {nearbyVehicle.Model.Name} not found in collection, adding now.");
                        VehicleCollector.collectedVehicles.Add(new CollectedVehicle(nearbyVehicle, path, false));
                        collectedVehicle = VehicleCollector.collectedVehicles.Where(cv => cv.Vehicle == nearbyVehicle).FirstOrDefault();
                    }

                    if (collectedVehicle == null)
                    {
                        return;
                    }

                    collectedVehicle.Driver.Tasks.Clear();

                    if (directOptions.SelectedItem == "First waypoint")
                    {
                        GameFiber.StartNew(() =>
                        {
                            AITasking.AssignWaypointTasks(collectedVehicle, waypoints, firstWaypoint);
                        });
                    }
                    else
                    {
                        if (nearestWaypoint != null)
                        {
                            GameFiber.StartNew(() =>
                            {
                                AITasking.AssignWaypointTasks(collectedVehicle, waypoints, nearestWaypoint);
                            });
                        }
                    }
                }
            }

            if (selectedItem == dismissDriver)
            {
                var nearbyVehicle = Game.LocalPlayer.Character.GetNearbyVehicles(1).Where(v => v != Game.LocalPlayer.Character.CurrentVehicle && v.VehicleAndDriverValid()).SingleOrDefault();
                if (nearbyVehicle)
                {
                    var collectedVehicle = VehicleCollector.collectedVehicles.Where(cv => cv.Vehicle == nearbyVehicle).FirstOrDefault();
                    switch (dismissDriver.Index)
                    {
                        case 0:
                            Game.LogTrivial($"Dismiss from path");
                            if (collectedVehicle != null)
                            {
                                collectedVehicle.Dismiss();
                            }
                            else
                            {
                                goto case 2;
                            }
                            break;

                        case 1:
                            Game.LogTrivial($"Dismiss from waypoint");
                            if (collectedVehicle != null)
                            {
                                if (collectedVehicle.StoppedAtWaypoint)
                                {
                                    collectedVehicle.StoppedAtWaypoint = false;
                                }
                                else
                                {
                                    collectedVehicle.SkipWaypoint = true;
                                    collectedVehicle.Driver.Tasks.Clear();
                                }

                                if (collectedVehicle.CurrentWaypoint.Number == collectedVehicle.Path.Waypoints.Count && !collectedVehicle.StoppedAtWaypoint)
                                {
                                    Game.LogTrivial($"Dismissed driver of {collectedVehicle.Vehicle.Model.Name} from final waypoint and ultimately the path");
                                }
                                else
                                {
                                    Game.LogTrivial($"Dismissed driver of {collectedVehicle.Vehicle.Model.Name} from current waypoint task");
                                }
                            }
                            else
                            {
                                goto case 2;
                            }
                            break;

                        case 2:
                            Game.LogTrivial($"Dismiss from position");
                            if(collectedVehicle != null)
                            {
                                collectedVehicle.StoppedAtWaypoint = false;
                                Game.LogTrivial($"Dismissed driver of {nearbyVehicle.Model.Name} from position (in collection)");
                            }
                            else
                            {
                                if(nearbyVehicle.Speed < 1f)
                                {
                                    Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(nearbyVehicle, 3f, 1, true);
                                }
                                nearbyVehicle.Driver.Tasks.Clear();
                                nearbyVehicle.IsSirenOn = false;
                                nearbyVehicle.IsSirenSilent = true;
                                nearbyVehicle.Driver.Dismiss();
                                Game.LogTrivial($"Dismissed driver of {nearbyVehicle.Model.Name} from position (was not in collection)");
                            }
                            break;
                    }
                }
            }
        }

        private static void Draw3DWaypointOnPlayer()
        {
            GameFiber.StartNew(() =>
            {
                while (SettingsMenu.threeDWaypoints.Checked)
                {
                    if (PathCreationMenu.pathCreationMenu.Visible)
                    {
                        if (PathCreationMenu.collectorWaypoint.Checked)
                        {
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, (float)PathCreationMenu.collectorRadius.Value * 2, (float)PathCreationMenu.collectorRadius.Value * 2, 1f, 80, 130, 255, 80, false, false, 2, false, 0, 0, false);
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, (float)PathCreationMenu.speedZoneRadius.Value * 2, (float)PathCreationMenu.speedZoneRadius.Value * 2, 1f, 255, 185, 80, 80, false, false, 2, false, 0, 0, false);
                        }
                        else if (PathCreationMenu.waypointType.SelectedItem.Contains("Drive To"))
                        {
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 65, 255, 65, 80, false, false, 2, false, 0, 0, false);
                        }
                        else
                        {
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 255, 65, 65, 80, false, false, 2, false, 0, 0, false);
                        }
                    }
                    else
                    {
                        break;
                    }
                    GameFiber.Yield();
                }
            });
        }

        private static void PathMenu_OnCheckboxChange(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            if (checkboxItem == disableAllPaths)
            {
                if (disableAllPaths.Checked)
                {
                    foreach (Path path in paths)
                    {
                        path.DisablePath();
                    }
                    Game.LogTrivial($"All paths disabled.");
                }
                else
                {
                    foreach (Path path in paths)
                    {
                        path.EnablePath();
                    }
                    Game.LogTrivial($"All paths enabled.");
                }

            }
        }
    }
}
