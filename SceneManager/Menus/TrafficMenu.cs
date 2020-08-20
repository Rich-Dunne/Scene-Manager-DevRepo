﻿using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    static class TrafficMenu
    {
        #pragma warning disable CS0618 // Type or member is obsolete, clear NUI squiggles
        public static UIMenuItem createNewPath, deleteAllPaths;
        public static UIMenuListItem editPath, directDriver, dismissDriver;
        public static UIMenuCheckboxItem disableAllPaths;
        
        public static List<dynamic> pathsNum = new List<dynamic>() { };
        public static List<Path> paths = new List<Path>() { };
        public static List<dynamic> dismissOptions = new List<dynamic>() { "From path", "From waypoint", "From position" };

        public static void BuildPathMenu()
        {
            // New stuff to mitigate Rebuild method
            MenuManager.pathMenu.OnItemSelect -= PathMenu_OnItemSelected;
            MenuManager.pathMenu.OnCheckboxChange -= PathMenu_OnCheckboxChange;
            MenuManager.menuPool.CloseAllMenus();
            MenuManager.pathMenu.Clear();

            MenuManager.pathMenu.AddItem(createNewPath = new UIMenuItem("Create New Path"));
            MenuManager.pathMenu.AddItem(editPath = new UIMenuListItem("Edit Path", pathsNum, 0));
            MenuManager.pathMenu.AddItem(disableAllPaths = new UIMenuCheckboxItem("Disable All Paths", false));
            MenuManager.pathMenu.AddItem(deleteAllPaths = new UIMenuItem("Delete All Paths"));
            MenuManager.pathMenu.AddItem(directDriver = new UIMenuListItem("Direct nearest driver to path", pathsNum, 0));
            MenuManager.pathMenu.AddItem(dismissDriver = new UIMenuListItem("Dismiss nearest driver", dismissOptions, 0));

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

            MenuManager.pathMenu.RefreshIndex();
            MenuManager.pathMenu.OnItemSelect += PathMenu_OnItemSelected;
            MenuManager.pathMenu.OnCheckboxChange += PathMenu_OnCheckboxChange;

            // New stuff to mitigate Rebuild method
            MenuManager.menuPool.RefreshIndex();
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

        private static bool IsInCollectedVehicles(this Vehicle v)
        {
            if (v && TrafficPathing.collectedVehicles.ContainsKey(v.LicensePlate))
            {
                Game.LogTrivial($"{v.Model.Name} was found in the collection.");
                return true;
            }
            else
            {
                Game.LogTrivial($"{v.Model.Name} was not found in the collection.");
                return false;
            }
        }

        // Refactor string param to enum
        public static void DeletePath(Path path, int index, string pathsToDelete)
        {
            // Before deleting a path, we need to dismiss any vehicles controlled by that path and remove the vehicles from ControlledVehicles
            //Game.LogTrivial($"Deleting path {index+1}");
            Game.LogTrivial($"Deleting path {path.PathNum}");
            var pathVehicles = TrafficPathing.collectedVehicles.Where(cv => cv.Value.Path == path.PathNum).ToList();

            Game.LogTrivial($"Removing all vehicles on the path");
            foreach (KeyValuePair<string, CollectedVehicle> cv in pathVehicles.Where(cv => cv.Value.Vehicle && cv.Value.Vehicle.Driver))
            {
                cv.Value.DismissNow = true;
                cv.Value.Vehicle.Driver.Tasks.Clear();
                cv.Value.Vehicle.Driver.Dismiss();
                cv.Value.Vehicle.Driver.IsPersistent = false;
                cv.Value.Vehicle.Dismiss();
                cv.Value.Vehicle.IsPersistent = false;
                //TrafficPathing.ControlledVehicles.Remove(cv.Value.LicensePlate);
                //Game.LogTrivial($"{cv.vehicle.Model.Name} cleared from path {cv.path}");
                TrafficPathing.collectedVehicles.Remove(cv.Value.LicensePlate);
            }

            // Remove the speed zone so cars don't continue to be affected after the path is deleted
            Game.LogTrivial($"Removing yield zone and waypoint blips");
            foreach (Waypoint wp in path.Waypoint)
            {
                if (wp.YieldZone != 0)
                {
                    World.RemoveSpeedZone(wp.YieldZone);
                }
                if (wp.Blip)
                {
                    wp.Blip.Delete();
                }
                if (wp.CollectorRadiusBlip)
                {
                    wp.CollectorRadiusBlip.Delete();
                }
            }

            Game.LogTrivial($"Clearing path.WaypointData");
            path.Waypoint.Clear();
            // Manipulating the menu to reflect specific paths being deleted
            if (pathsToDelete == "Single")
            {
                paths.RemoveAt(index);
                //Game.LogTrivial("pathsNum count: " + pathsNum.Count);
                //Game.LogTrivial("index: " + index);
                pathsNum.RemoveAt(index);
                //RebuildTrafficMenu();
                BuildPathMenu();
                MenuManager.pathMenu.Visible = true;
                Game.LogTrivial($"Path {path.PathNum} deleted.");
                Game.DisplayNotification($"~o~Scene Manager\n~w~Path {path.PathNum} deleted.");
            }

            MenuManager.editPathMenu.Reset(true, true);
            EditPathMenu.togglePath.Enabled = true;
        }

        private static void PathMenu_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == createNewPath)
            {
                MenuManager.pathMenu.Visible = false;
                MenuManager.pathCreationMenu.Visible = true;

                // For each element in paths, determine if the element exists but is not finished yet, or if it doesn't exist, create it.
                for (int i = 0; i <= paths.Count; i++)
                {
                    if (paths.ElementAtOrDefault(i) != null && paths[i].PathFinished == false)
                    {
                        //Game.LogTrivial($"pathFinished: {paths[i].PathFinished}");
                        Game.LogTrivial($"Resuming path {i + 1}");
                        Game.DisplayNotification($"~o~Scene Manager\n~y~[Creating]~w~ Resuming path {i + 1}");
                        break;
                    }
                    else if (paths.ElementAtOrDefault(i) == null)
                    {
                        Game.LogTrivial($"Creating path {i + 1}");
                        Game.DisplayNotification($"~o~Scene Manager\n~y~[Creating]~w~ Path {i + 1} started.");
                        paths.Insert(i, new Path(i + 1, false));
                        PathCreationMenu.trafficRemoveWaypoint.Enabled = false;

                        if (SettingsMenu.debugGraphics.Checked)
                        {
                            GameFiber.StartNew(() =>
                            {
                                DebugGraphics.LoopToDrawDebugGraphics(SettingsMenu.debugGraphics, paths[i]);
                            });
                        }
                        break;
                    }
                }
            }

            if (selectedItem == editPath)
            {
                MenuManager.pathMenu.Visible = false;
                MenuManager.editPathMenu.Visible = true;
            }

            if (selectedItem == deleteAllPaths)
            {
                // Iterate through each item in paths and delete it
                for (int i = 0; i < paths.Count; i++)
                {
                    DeletePath(paths[i], i, "All");
                }
                foreach (Path path in paths)
                {
                    path.Waypoint.Clear();
                }
                paths.Clear();
                pathsNum.Clear();
                //RebuildTrafficMenu();
                BuildPathMenu();
                MenuManager.pathMenu.Visible = true;
                Game.LogTrivial($"All paths deleted");
                Game.DisplayNotification($"~o~Scene Manager\n~w~All paths deleted.");
            }

            if (selectedItem == directDriver)
            {
                var nearbyVehicle = Game.LocalPlayer.Character.GetNearbyVehicles(1).Where(v => v.VehicleAndDriverValid()).SingleOrDefault();
                if (nearbyVehicle)
                {
                    if (nearbyVehicle.IsInCollectedVehicles())
                    {
                        var vehicle = TrafficPathing.collectedVehicles[nearbyVehicle.LicensePlate];

                        Game.LogTrivial($"[Direct Driver] {nearbyVehicle.Model.Name} already in collection.  Clearing tasks.");
                        nearbyVehicle.Driver.Tasks.Clear();
                        vehicle.Path = paths[directDriver.Index].Waypoint[0].Path;
                        vehicle.TotalWaypoints = paths[directDriver.Index].Waypoint.Count;
                        vehicle.CurrentWaypoint = 1;
                        vehicle.DismissNow = true;
                        vehicle.StoppedAtWaypoint = false;
                        vehicle.Redirected = true;
                        GameFiber DirectTaskFiber = new GameFiber(() => TrafficPathing.DirectTask(vehicle, paths[directDriver.Index].Waypoint));
                        DirectTaskFiber.Start();
                    }
                    else
                    {
                        TrafficPathing.collectedVehicles.Add(nearbyVehicle.LicensePlate, new CollectedVehicle(nearbyVehicle, nearbyVehicle.LicensePlate, paths[directDriver.Index].Waypoint[0].Path, paths[directDriver.Index].Waypoint.Count, 1, false, false, true));
                        Game.LogTrivial($"[Direct Driver] {nearbyVehicle.Model.Name} not in collection, adding to collection for path {paths[directDriver.Index].Waypoint[0].Path} with {paths[directDriver.Index].Waypoint.Count} waypoints");

                        GameFiber DirectTaskFiber = new GameFiber(() => TrafficPathing.DirectTask(TrafficPathing.collectedVehicles[nearbyVehicle.LicensePlate], paths[directDriver.Index].Waypoint));
                        DirectTaskFiber.Start();
                    }
                }
            }

            if (selectedItem == dismissDriver)
            {
                var nearbyVehicle = Game.LocalPlayer.Character.GetNearbyVehicles(1).Where(v => v.VehicleAndDriverValid()).SingleOrDefault();
                if (nearbyVehicle)
                {
                    switch (dismissDriver.Index)
                    {
                        case 0:
                            Game.LogTrivial($"Dismiss from path");
                            if (nearbyVehicle.IsInCollectedVehicles())
                            {
                                var controlledVehicle = TrafficPathing.collectedVehicles[nearbyVehicle.LicensePlate];
                                controlledVehicle.DismissNow = true;
                                controlledVehicle.Vehicle.Driver.Tasks.Clear();
                                controlledVehicle.Vehicle.Driver.Dismiss();
                                Game.LogTrivial($"Dismissed driver of {controlledVehicle.Vehicle.Model.Name} from path {controlledVehicle.Path}");
                            }
                            else
                            {
                                goto case 2;
                            }
                            break;

                        case 1:
                            Game.LogTrivial($"Dismiss from waypoint");
                            if (nearbyVehicle.IsInCollectedVehicles())
                            {
                                var controlledVehicle = TrafficPathing.collectedVehicles[nearbyVehicle.LicensePlate];
                                controlledVehicle.StoppedAtWaypoint = false;
                                controlledVehicle.Vehicle.Driver.Tasks.Clear();
                                controlledVehicle.Vehicle.Driver.Dismiss();

                                if (controlledVehicle.CurrentWaypoint == controlledVehicle.TotalWaypoints && !controlledVehicle.StoppedAtWaypoint)
                                {
                                    controlledVehicle.DismissNow = true;
                                    Game.LogTrivial($"Dismissed driver of {controlledVehicle.Vehicle.Model.Name} from final waypoint and ultimately the path");
                                }
                                else
                                {
                                    Game.LogTrivial($"Dismissed driver of {controlledVehicle.Vehicle.Model.Name} from waypoint {controlledVehicle.CurrentWaypoint}");
                                }
                            }
                            else
                            {
                                goto case 2;
                            }
                            break;

                        case 2:
                            Game.LogTrivial($"Dismiss from position");
                            if (nearbyVehicle.IsInCollectedVehicles())
                            {
                                nearbyVehicle.Driver.Tasks.Clear();
                                nearbyVehicle.Driver.Dismiss();
                                Game.LogTrivial($"Dismissed driver of {nearbyVehicle.Model.Name} (in collection)");
                            }
                            else
                            {
                                nearbyVehicle.Driver.Tasks.Clear();
                                nearbyVehicle.Driver.Dismiss();
                                Game.LogTrivial($"Dismissed driver of {nearbyVehicle.Model.Name} (was not in collection)");
                            }
                            break;

                        default:
                            Game.LogTrivial($"dismissDriver index was unexpected");
                            break;
                    }
                }
                else
                {
                    Game.LogTrivial($"There are no vehicles nearby matching the requirements.");
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
                        path.PathDisabled = true;
                        foreach (Waypoint waypoint in path.Waypoint)
                        {
                            waypoint.Blip.Alpha = 0.5f;
                            if (waypoint.CollectorRadiusBlip)
                            {
                                waypoint.CollectorRadiusBlip.Alpha = 0.25f;
                            }
                        }
                    }
                    Game.LogTrivial($"All paths disabled.");
                }
                else
                {
                    foreach (Path path in paths)
                    {
                        path.PathDisabled = false;
                        foreach (Waypoint waypoint in path.Waypoint)
                        {
                            waypoint.Blip.Alpha = 1f;
                            if (waypoint.CollectorRadiusBlip)
                            {
                                waypoint.CollectorRadiusBlip.Alpha = 0.5f;
                            }
                        }
                    }
                    Game.LogTrivial($"All paths enabled.");
                }

            }
        }
    }
}