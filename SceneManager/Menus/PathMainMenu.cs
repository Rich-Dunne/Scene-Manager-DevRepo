using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    public enum DismissOption
    {
        FromPath = 0,
        FromWaypoint = 1,
        FromWorld = 2,
        FromPlayer = 3,
        FromDirected = 4
    }

    static class PathMainMenu
    {
        internal static List<Path> paths = new List<Path>() { };
        private static List<string> dismissOptions = new List<string>() { "From path", "From waypoint", "From world" };

        internal static UIMenu pathMainMenu = new UIMenu("Scene Manager", "~o~Path Manager Main Menu");
        internal static UIMenuItem createNewPath;
        internal static UIMenuItem deleteAllPaths = new UIMenuItem("Delete All Paths");
        internal static UIMenuNumericScrollerItem<int> editPath;
        internal static UIMenuListScrollerItem<string> directOptions = new UIMenuListScrollerItem<string>("Direct driver to path's", "", new[] { "First waypoint", "Nearest waypoint" });
        internal static UIMenuNumericScrollerItem<int> directDriver;
        internal static UIMenuListScrollerItem<string> dismissDriver = new UIMenuListScrollerItem<string>("Dismiss nearest driver", $"~b~From path: ~w~AI will be released from the path{Environment.NewLine}~b~From waypoint: ~w~AI will skip their current waypoint task{Environment.NewLine}~b~From world: ~w~AI will be removed from the world.", dismissOptions);
        internal static UIMenuCheckboxItem disableAllPaths = new UIMenuCheckboxItem("Disable All Paths", false);

        internal enum Delete
        {
            Single,
            All
        }

        internal static void InstantiateMenu()
        {
            pathMainMenu.ParentMenu = MainMenu.mainMenu;
            MenuManager.menuPool.Add(pathMainMenu);
        }

        internal static void BuildPathMenu()
        {
            // Need to unsubscribe from events, else there will be duplicate firings if the user left the menu and re-entered
            ResetEventHandlerSubscriptions();

            MenuManager.menuPool.CloseAllMenus();
            pathMainMenu.Clear();

            pathMainMenu.AddItem(createNewPath = new UIMenuItem("Create New Path"));
            createNewPath.ForeColor = Color.Gold;
            pathMainMenu.AddItem(editPath = new UIMenuNumericScrollerItem<int>("Edit Path", "", 1, paths.Count, 1));
            editPath.Index = 0;
            editPath.ForeColor = Color.Gold;
            pathMainMenu.AddItem(disableAllPaths);
            disableAllPaths.Enabled = true;
            pathMainMenu.AddItem(deleteAllPaths);
            deleteAllPaths.Enabled = true;
            deleteAllPaths.ForeColor = Color.Gold;
            pathMainMenu.AddItem(directOptions);
            pathMainMenu.AddItem(directDriver = new UIMenuNumericScrollerItem<int>("Direct nearest driver to path", "", 1, paths.Count, 1));
            directDriver.ForeColor = Color.Gold;
            directDriver.Enabled = true;
            pathMainMenu.AddItem(dismissDriver);
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

            MenuManager.menuPool.RefreshIndex();

            void ResetEventHandlerSubscriptions()
            {
                pathMainMenu.OnItemSelect -= PathMenu_OnItemSelected;
                pathMainMenu.OnCheckboxChange -= PathMenu_OnCheckboxChange;
                pathMainMenu.OnItemSelect += PathMenu_OnItemSelected;
                pathMainMenu.OnCheckboxChange += PathMenu_OnCheckboxChange;
            }
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

        internal static void DeletePath(Path path, Delete pathsToDelete)
        {
            //Game.LogTrivial($"Preparing to delete path {path.Number}");

            RemoveVehiclesFromPath();
            RemoveBlipsAndYieldZones();

            //Game.LogTrivial($"Clearing path waypoints");
            path.Waypoints.Clear();

            // Manipulating the menu to reflect specific paths being deleted
            if (pathsToDelete == Delete.Single)
            {
                paths.Remove(path);
                UpdatePathNumbers();
                UpdatePathBlips();
                BuildPathMenu();
                pathMainMenu.Visible = true;
                Game.LogTrivial($"Path {path.Number} deleted successfully.");
                Game.DisplayNotification($"~o~Scene Manager\n~w~Path {path.Number} deleted.");
            }

            EditPathMenu.editPathMenu.Reset(true, true);
            EditPathMenu.disablePath.Enabled = true;

            void RemoveVehiclesFromPath()
            {
                //Game.LogTrivial($"Removing all vehicles on the path");
                var pathVehicles = path.CollectedVehicles.Where(cv => cv.Path.Number == path.Number).ToList();
                foreach (CollectedVehicle cv in pathVehicles.Where(cv => cv != null && cv.Vehicle && cv.Driver))
                {
                    if (cv.StoppedAtWaypoint)
                    {
                        Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(cv.Vehicle, 1f, 1, true);
                    }
                    cv.StoppedAtWaypoint = false;
                    if (cv.Driver.GetAttachedBlip())
                    {
                        cv.Driver.GetAttachedBlip().Delete();
                    }
                    cv.Driver.Dismiss();
                    cv.Vehicle.IsSirenOn = false;
                    cv.Vehicle.IsSirenSilent = true;
                    cv.Vehicle.Dismiss();

                    //Game.LogTrivial($"{cv.vehicle.Model.Name} cleared from path {cv.path}");
                    path.CollectedVehicles.Remove(cv);
                }
                path.CollectedVehicles.Clear();
            }

            void RemoveBlipsAndYieldZones()
            {
                //Game.LogTrivial($"Removing waypoint blips and yield zones.");
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
            }

            void UpdatePathBlips()
            {
                foreach (Path p in paths)
                {
                    foreach (Waypoint waypoint in p.Waypoints)
                    {
                        var blipColor = waypoint.Blip.Color;
                        waypoint.Blip.Sprite = (BlipSprite)paths.IndexOf(p) + 17;
                        waypoint.Blip.Color = blipColor;
                    }
                }
            }

            void UpdatePathNumbers()
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    paths[i].Number = i + 1;
                }
            }
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
                        //Game.LogTrivial($"Resuming path {paths[i].Number}");
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
                disableAllPaths.Checked = false;
                paths.Clear();
                BuildPathMenu();
                pathMainMenu.Visible = true;
                Game.LogTrivial($"All paths deleted");
                Game.DisplayNotification($"~o~Scene Manager\n~w~All paths deleted.");
            }

            if (selectedItem == directDriver)
            {
                var nearbyVehicle = Game.LocalPlayer.Character.GetNearbyVehicles(16).Where(v => v != Game.LocalPlayer.Character.CurrentVehicle && v.VehicleAndDriverValid()).FirstOrDefault();
                var path = paths[directDriver.Index];
                var collectedVehicle = path.CollectedVehicles.Where(cv => cv.Vehicle == nearbyVehicle).FirstOrDefault();
                var waypoints = path.Waypoints;
                var firstWaypoint = waypoints.First();
                var nearestWaypoint = waypoints.Where(wp => wp.Position.DistanceTo2D(nearbyVehicle.FrontPosition) < wp.Position.DistanceTo2D(nearbyVehicle.RearPosition)).OrderBy(wp => wp.Position.DistanceTo2D(nearbyVehicle)).FirstOrDefault();

                if (nearbyVehicle)
                {
                    var nearbyVehiclePath = paths.Where(p => p.CollectedVehicles.Any(v => v.Vehicle == nearbyVehicle)).FirstOrDefault();
                    if(nearbyVehiclePath != null)
                    {
                        var nearbyCollectedVehicle = nearbyVehiclePath.CollectedVehicles.Where(v => v.Vehicle == nearbyVehicle).FirstOrDefault();
                        if (nearbyCollectedVehicle != null)
                        {
                            nearbyCollectedVehicle.Dismiss(DismissOption.FromDirected, path);
                            if (directOptions.SelectedItem == "First waypoint")
                            {
                                GameFiber.StartNew(() =>
                                {
                                    AITasking.AssignWaypointTasks(nearbyCollectedVehicle, path, firstWaypoint);
                                });
                            }
                            else
                            {
                                if (nearestWaypoint != null)
                                {
                                    GameFiber.StartNew(() =>
                                    {
                                        AITasking.AssignWaypointTasks(nearbyCollectedVehicle, path, nearestWaypoint);
                                    });
                                }
                            }
                            return;
                        }
                    }

                    // The vehicle should only be added to the collection when it's not null AND if the selected item is First Waypoint OR if the selected item is nearestWaypoint AND nearestWaypoint is not null
                    if (collectedVehicle == null && directOptions.SelectedItem == "First waypoint" || (directOptions.SelectedItem == "Nearest waypoint" && nearestWaypoint != null))
                    {
                        Game.LogTrivial($"[Direct Driver] Adding {nearbyVehicle.Model.Name} to collection.");
                        path.CollectedVehicles.Add(new CollectedVehicle(nearbyVehicle, path));
                        collectedVehicle = path.CollectedVehicles.Where(cv => cv.Vehicle == nearbyVehicle).FirstOrDefault();
                        //Logger.Log($"Collected vehicle is {collectedVehicle.Vehicle.Model.Name}");
                    }

                    if (collectedVehicle == null)
                    {
                        return;
                    }
                    collectedVehicle.Directed = true;
                    collectedVehicle.Driver.Tasks.Clear();

                    //Logger.Log($"Collected vehicle properties:  Dismissed [{collectedVehicle.Dismissed}], Directed [{collectedVehicle.Directed}], StopppedAtWaypoint [{collectedVehicle.StoppedAtWaypoint}]");
                    if (directOptions.SelectedItem == "First waypoint")
                    {
                        GameFiber.StartNew(() =>
                        {
                            AITasking.AssignWaypointTasks(collectedVehicle, path, firstWaypoint);
                        });
                    }
                    else
                    {
                        if (nearestWaypoint != null)
                        {
                            GameFiber.StartNew(() =>
                            {
                                AITasking.AssignWaypointTasks(collectedVehicle, path, nearestWaypoint);
                            });
                        }
                    }
                }
            }

            if (selectedItem == dismissDriver)
            {
                var nearbyVehicle = Game.LocalPlayer.Character.GetNearbyVehicles(16).Where(v => v != Game.LocalPlayer.Character.CurrentVehicle && v.VehicleAndDriverValid()).FirstOrDefault();
                if (nearbyVehicle)
                {
                    if (!paths.Any() && dismissDriver.Index == (int)DismissOption.FromWorld)
                    {
                        Game.LogTrivial($"Dismissed {nearbyVehicle.Model.Name} from the world");
                        while (nearbyVehicle && nearbyVehicle.HasOccupants)
                        {
                            foreach (Ped occupant in nearbyVehicle.Occupants)
                            {
                                occupant.Delete();
                            }
                            GameFiber.Yield();
                        }
                        if (nearbyVehicle)
                        {
                            nearbyVehicle.Delete();
                        }
                        return;
                    }

                    foreach(Path path in paths)
                    {
                        var collectedVehicle = path.CollectedVehicles.Where(cv => cv.Vehicle == nearbyVehicle).FirstOrDefault();
                        if (collectedVehicle != null)
                        {
                            collectedVehicle.Dismiss((DismissOption)dismissDriver.Index);
                            break;
                        }
                        else if (dismissDriver.Index == (int)DismissOption.FromWorld)
                        {
                            Game.LogTrivial($"Dismissed {nearbyVehicle.Model.Name} from the world");
                            while (nearbyVehicle && nearbyVehicle.HasOccupants)
                            {
                                foreach (Ped occupant in nearbyVehicle.Occupants)
                                {
                                    occupant.Delete();
                                }
                                GameFiber.Yield();
                            }
                            if (nearbyVehicle)
                            {
                                nearbyVehicle.Delete();
                            }
                            break;
                        }
                    }
                }
            }
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
                        else if (PathCreationMenu.stopWaypointType.Checked)
                        {
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 255, 65, 65, 80, false, false, 2, false, 0, 0, false);
                        }
                        else
                        {
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 65, 255, 65, 80, false, false, 2, false, 0, 0, false);
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
    }
}
