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
        public static UIMenu pathMainMenu { get; private set; }
        public static UIMenuItem createNewPath { get; private set; }
        private static UIMenuItem deleteAllPaths;
        public static UIMenuListScrollerItem<int> editPath { get; private set; }
        public static UIMenuListScrollerItem<string> directOptions { get; private set; }
        public static UIMenuListScrollerItem<int> directDriver { get; private set; }
        public static UIMenuListScrollerItem<string> dismissDriver { get; private set; }
        public static UIMenuCheckboxItem disableAllPaths { get; private set; }

        private static List<int> pathsNum = new List<int>();
        private static List<Path> paths = new List<Path>() { };
        private static List<string> dismissOptions = new List<string>() { "From path", "From waypoint", "From position" };
        public enum Delete
        {
            Single,
            All
        }

        internal static void InstantiateMenu()
        {
            pathMainMenu = new UIMenu("Scene Menu", "~o~Path Manager Main Menu");
            pathMainMenu.ParentMenu = MainMenu.mainMenu;
            MenuManager.menuPool.Add(pathMainMenu);
        }

        public static void BuildPathMenu()
        {
            // New stuff to mitigate Rebuild method
            pathMainMenu.OnItemSelect -= PathMenu_OnItemSelected;
            pathMainMenu.OnCheckboxChange -= PathMenu_OnCheckboxChange;
            MenuManager.menuPool.CloseAllMenus();
            pathMainMenu.Clear();

            pathMainMenu.AddItem(createNewPath = new UIMenuItem("Create New Path"));
            createNewPath.ForeColor = Color.Gold;
            pathMainMenu.AddItem(editPath = new UIMenuListScrollerItem<int>("Edit Path", "", pathsNum));
            editPath.ForeColor = Color.Gold;
            pathMainMenu.AddItem(disableAllPaths = new UIMenuCheckboxItem("Disable All Paths", false));
            pathMainMenu.AddItem(deleteAllPaths = new UIMenuItem("Delete All Paths"));
            deleteAllPaths.ForeColor = Color.Gold;
            pathMainMenu.AddItem(directOptions = new UIMenuListScrollerItem<string>("Direct driver to path's", "", new[] { "First waypoint", "Nearest waypoint" }));
            pathMainMenu.AddItem(directDriver = new UIMenuListScrollerItem<int>("Direct nearest driver to path", "", pathsNum));
            directDriver.ForeColor = Color.Gold;
            pathMainMenu.AddItem(dismissDriver = new UIMenuListScrollerItem<string>("Dismiss nearest driver", "", dismissOptions));
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
            pathMainMenu.OnItemSelect += PathMenu_OnItemSelected;
            pathMainMenu.OnCheckboxChange += PathMenu_OnCheckboxChange;

            // New stuff to mitigate Rebuild method
            MenuManager.menuPool.RefreshIndex();
        }

        public static ref List<Path> GetPaths()
        {
            return ref paths;
        }

        public static void AddPathToPathCountList(int indexToInsertAt, int pathNum)
        {
            pathsNum.Insert(indexToInsertAt, pathNum);
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
            if (v && VehicleCollector.collectedVehicles.ContainsKey(v.LicensePlate))
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

        public static void DeletePath(Path path, int index, Delete pathsToDelete)
        {
            // Before deleting a path, we need to dismiss any vehicles controlled by that path and remove the vehicles from ControlledVehicles
            //Game.LogTrivial($"Deleting path {index+1}");
            Game.LogTrivial($"Deleting path {path.PathNum}");
            var pathVehicles = VehicleCollector.collectedVehicles.Where(cv => cv.Value.Path == path.PathNum).ToList();

            Game.LogTrivial($"Removing all vehicles on the path");
            foreach (KeyValuePair<string, CollectedVehicle> cv in pathVehicles.Where(cv => cv.Value.Vehicle && cv.Value.Vehicle.Driver))
            {
                cv.Value.SetDismissNow(true);
                cv.Value.Vehicle.Driver.Tasks.Clear();
                cv.Value.Vehicle.Driver.Dismiss();
                cv.Value.Vehicle.Driver.IsPersistent = false;
                cv.Value.Vehicle.Dismiss();
                cv.Value.Vehicle.IsPersistent = false;

                //Game.LogTrivial($"{cv.vehicle.Model.Name} cleared from path {cv.path}");
                VehicleCollector.collectedVehicles.Remove(cv.Value.LicensePlate);
            }

            // Remove the speed zone so cars don't continue to be affected after the path is deleted
            Game.LogTrivial($"Removing yield zone and waypoint blips");
            foreach (Waypoint waypoint in path.Waypoints)
            {
                if (waypoint.YieldZone != 0)
                {
                    World.RemoveSpeedZone(waypoint.YieldZone);
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
                paths.RemoveAt(index);
                //Game.LogTrivial("pathsNum count: " + pathsNum.Count);
                //Game.LogTrivial("index: " + index);
                pathsNum.RemoveAt(index);
                BuildPathMenu();
                pathMainMenu.Visible = true;
                Game.LogTrivial($"Path {path.PathNum} deleted.");
                Game.DisplayNotification($"~o~Scene Manager\n~w~Path {path.PathNum} deleted.");
            }

            EditPathMenu.editPathMenu.Reset(true, true);
            EditPathMenu.togglePath.Enabled = true;
        }

        private static void PathMenu_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == createNewPath)
            {
                pathMainMenu.Visible = false;
                PathCreationMenu.pathCreationMenu.Visible = true;

                // For each element in paths, determine if the element exists but is not finished yet, or if it doesn't exist, create it.
                for (int i = 0; i <= paths.Count; i++)
                {
                    if (paths.ElementAtOrDefault(i) != null && paths[i].PathFinished == false)
                    {
                        //Game.LogTrivial($"pathFinished: {paths[i].PathFinished}");
                        Game.LogTrivial($"Resuming path {paths[i].PathNum}");
                        Game.DisplayNotification($"~o~Scene Manager\n~y~[Creating]~w~ Resuming path {paths[i].PathNum}");
                        break;
                    }
                    //else if (paths.ElementAtOrDefault(i) == null)
                    //{
                    //    Do we only want to do this once the first waypoint is added ?
                    //   PathCreationMenu.AddNewPathToPathsCollection(paths, i);

                    //    if (SettingsMenu.debugGraphics.Checked)
                    //    {
                    //        GameFiber.StartNew(() =>
                    //        {
                    //            DebugGraphics.LoopToDrawDebugGraphics(SettingsMenu.debugGraphics, paths[i]);
                    //        });
                    //    }
                    //    break;
                    //}
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
                    DeletePath(paths[i], i, Delete.All);
                }
                foreach (Path path in paths)
                {
                    foreach(Waypoint waypoint in path.Waypoints.Where(wp => wp.YieldZone != 0))
                    {
                        World.RemoveSpeedZone(waypoint.YieldZone);
                    }
                    path.Waypoints.Clear();
                }
                paths.Clear();
                pathsNum.Clear();
                BuildPathMenu();
                pathMainMenu.Visible = true;
                Game.LogTrivial($"All paths deleted");
                Game.DisplayNotification($"~o~Scene Manager\n~w~All paths deleted.");
            }

            // This needs big refactor
            if (selectedItem == directDriver)
            {
                var nearbyVehicle = Game.LocalPlayer.Character.GetNearbyVehicles(1).Where(v => v.VehicleAndDriverValid()).SingleOrDefault();
                var firstWaypoint = paths[directDriver.Index].Waypoints.First();
                var pathNum = paths[directDriver.Index].Waypoints[0].Path;
                var totalPathWaypoints = paths[directDriver.Index].Waypoints.Count;

                if (nearbyVehicle)
                {
                    var nearestWaypoint = paths[directDriver.Index].Waypoints.Where(wp => wp.Position.DistanceTo2D(nearbyVehicle.FrontPosition) < wp.Position.DistanceTo2D(nearbyVehicle.RearPosition)).OrderBy(wp => wp.Position.DistanceTo2D(nearbyVehicle)).ToArray();

                    VehicleCollector.SetVehicleAndDriverPersistence(nearbyVehicle);
                    if (nearbyVehicle.IsInCollectedVehicles())
                    {
                        var vehicle = VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate];

                        Game.LogTrivial($"[Direct Driver] {nearbyVehicle.Model.Name} already in collection.  Clearing tasks.");
                        nearbyVehicle.Driver.Tasks.Clear();
                        vehicle.AssignPropertiesFromDirectedTask(pathNum, totalPathWaypoints, 1, tasksAssigned: false, dismiss: true, stoppedAtWaypoint: false);//, redirected: true);

                        if (directOptions.SelectedItem == "First waypoint")
                        {
                            GameFiber.StartNew(() =>
                            {
                                nearbyVehicle.Driver.Tasks.DriveToPosition(firstWaypoint.Position, firstWaypoint.Speed, (VehicleDrivingFlags)263043, 2f).WaitForCompletion();
                            });
                        }
                        else
                        {
                            GameFiber.StartNew(() =>
                            {
                                nearbyVehicle.Driver.Tasks.DriveToPosition(nearestWaypoint[0].Position, nearestWaypoint[0].Speed, (VehicleDrivingFlags)263043, 2f).WaitForCompletion();
                            });
                        }
                    }
                    else
                    {
                        VehicleCollector.collectedVehicles.Add(nearbyVehicle.LicensePlate, new CollectedVehicle(nearbyVehicle, nearbyVehicle.LicensePlate, paths[directDriver.Index].Waypoints[0].Path, paths[directDriver.Index].Waypoints.Count, 1, false, false, true));
                        Game.LogTrivial($"[Direct Driver] {nearbyVehicle.Model.Name} not in collection, adding to collection for path {paths[directDriver.Index].PathNum} with {paths[directDriver.Index].Waypoints.Count} waypoints");

                        if (directOptions.SelectedItem == "First waypoint")
                        {
                            GameFiber.StartNew(() =>
                            {
                                VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate].Vehicle.Driver.Tasks.DriveToPosition(firstWaypoint.Position, firstWaypoint.Speed, (VehicleDrivingFlags)263043, 2f).WaitForCompletion();

                                for (int nextWaypoint = firstWaypoint.Number; nextWaypoint < paths[directDriver.Index].Waypoints.Count; nextWaypoint++)
                                {
                                    if (paths[directDriver.Index].Waypoints.ElementAtOrDefault(nextWaypoint) == null)
                                    {
                                        Game.LogTrivial($"Waypoint is null");
                                        break;
                                    }

                                    Game.LogTrivial($"{VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate].Vehicle.Model.Name} is driving to waypoint {paths[directDriver.Index].Waypoints[nextWaypoint].Number}");
                                    if (paths[directDriver.Index].Waypoints.ElementAtOrDefault(nextWaypoint) != null && !VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate].StoppedAtWaypoint)
                                    {
                                        VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate].Vehicle.Driver.Tasks.DriveToPosition(paths[directDriver.Index].Waypoints[nextWaypoint].Position, paths[directDriver.Index].Waypoints[nextWaypoint].Speed, (VehicleDrivingFlags)263043, 2f).WaitForCompletion();
                                    }

                                    if (paths[directDriver.Index].Waypoints.ElementAtOrDefault(nextWaypoint) != null && paths[directDriver.Index].Waypoints[nextWaypoint].DrivingFlag == VehicleDrivingFlags.StopAtDestination)
                                    {
                                        Game.LogTrivial($"{VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate].Vehicle.Model.Name} stopping at waypoint.");
                                        VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate].Vehicle.Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraightBraking);
                                        VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate].SetStoppedAtWaypoint(true);
                                    }
                                }
                                Game.LogTrivial($"{VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate].Vehicle.Model.Name} all tasks complete.");
                                AITasking.DismissDriver(VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate]);
                            });
                        }
                        else
                        {
                            GameFiber.StartNew(() =>
                            {
                                VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate].Vehicle.Driver.Tasks.DriveToPosition(nearestWaypoint[0].Position, nearestWaypoint[0].Speed, (VehicleDrivingFlags)263043, 2f).WaitForCompletion();

                                for (int nextWaypoint = nearestWaypoint[0].Number; nextWaypoint < paths[directDriver.Index].Waypoints.Count; nextWaypoint++)
                                {
                                    if (paths[directDriver.Index].Waypoints.ElementAtOrDefault(nextWaypoint) == null)
                                    {
                                        Game.LogTrivial($"Waypoint is null");
                                        break;
                                    }

                                    Game.LogTrivial($"{VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate].Vehicle.Model.Name} is driving to waypoint {paths[directDriver.Index].Waypoints[nextWaypoint].Number}");
                                    if (paths[directDriver.Index].Waypoints.ElementAtOrDefault(nextWaypoint) != null && !VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate].StoppedAtWaypoint)
                                    {
                                        VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate].Vehicle.Driver.Tasks.DriveToPosition(paths[directDriver.Index].Waypoints[nextWaypoint].Position, paths[directDriver.Index].Waypoints[nextWaypoint].Speed, (VehicleDrivingFlags)263043, 2f).WaitForCompletion();
                                    }

                                    if (paths[directDriver.Index].Waypoints.ElementAtOrDefault(nextWaypoint) != null && paths[directDriver.Index].Waypoints[nextWaypoint].DrivingFlag == VehicleDrivingFlags.StopAtDestination)
                                    {
                                        Game.LogTrivial($"{VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate].Vehicle.Model.Name} stopping at waypoint.");
                                        VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate].Vehicle.Driver.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraightBraking);
                                        VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate].SetStoppedAtWaypoint(true);
                                    }
                                }
                                Game.LogTrivial($"{VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate].Vehicle.Model.Name} all tasks complete.");
                                AITasking.DismissDriver(VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate]);
                            });
                        }
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
                                var controlledVehicle = VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate];
                                controlledVehicle.SetDismissNow(true);
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
                                var controlledVehicle = VehicleCollector.collectedVehicles[nearbyVehicle.LicensePlate];
                                controlledVehicle.SetStoppedAtWaypoint(false);
                                controlledVehicle.Vehicle.Driver.Tasks.Clear();
                                controlledVehicle.Vehicle.Driver.Dismiss();

                                if (controlledVehicle.CurrentWaypoint == controlledVehicle.TotalWaypoints && !controlledVehicle.StoppedAtWaypoint)
                                {
                                    controlledVehicle.SetDismissNow(true);
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
                        path.DisablePath();
                        foreach (Waypoint waypoint in path.Waypoints)
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
                        path.EnablePath();
                        foreach (Waypoint waypoint in path.Waypoints)
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
