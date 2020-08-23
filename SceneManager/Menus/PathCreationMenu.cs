using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    class PathCreationMenu
    {
        public static UIMenu pathCreationMenu { get; private set; }
        private static UIMenuItem trafficAddWaypoint, trafficRemoveWaypoint, trafficEndPath;
        private static UIMenuListScrollerItem<string> waypointType;
        private static UIMenuListScrollerItem<float> waypointSpeed;
        private static UIMenuListScrollerItem<float> collectorRadius;
        private static UIMenuCheckboxItem collectorWaypoint;

        private static List<string> waypointTypes = new List<string>() { "Drive To", "Stop" };
        private static List<float> waypointSpeeds = new List<float>() { 5f, 10f, 15f, 20f, 30f, 40f, 50f, 60f, 70f };
        private static List<float> collectorRadii = new List<float>() { 3f, 5f, 10f, 15f, 20f, 30f, 40f, 50f };
        private static VehicleDrivingFlags[] drivingFlags = new VehicleDrivingFlags[] { VehicleDrivingFlags.Normal, VehicleDrivingFlags.StopAtDestination }; // Implement custom driving flag for normal

        internal static void InstantiateMenu()
        {
            pathCreationMenu = new UIMenu("Scene Menu", "~o~Path Creation");
            pathCreationMenu.ParentMenu = PathMainMenu.pathMainMenu;
            MenuManager.menuPool.Add(pathCreationMenu);
        }

        public static void BuildPathCreationMenu()
        {
            pathCreationMenu.AddItem(waypointType = new UIMenuListScrollerItem<string>("Waypoint Type", "", waypointTypes));
            pathCreationMenu.AddItem(waypointSpeed = new UIMenuListScrollerItem<float>($"Waypoint Speed (in {SettingsMenu.speedUnits.SelectedItem})", "", waypointSpeeds));
            pathCreationMenu.AddItem(collectorWaypoint = new UIMenuCheckboxItem("Collector", true)); // true if path's first waypoint
            pathCreationMenu.AddItem(collectorRadius = new UIMenuListScrollerItem<float>("Collection Radius", "", collectorRadii));
            pathCreationMenu.AddItem(trafficAddWaypoint = new UIMenuItem("Add waypoint"));
            trafficAddWaypoint.ForeColor = Color.Gold;
            pathCreationMenu.AddItem(trafficRemoveWaypoint = new UIMenuItem("Remove last waypoint"));
            trafficRemoveWaypoint.ForeColor = Color.Gold;
            trafficRemoveWaypoint.Enabled = false;
            pathCreationMenu.AddItem(trafficEndPath = new UIMenuItem("End path creation"));
            trafficEndPath.ForeColor = Color.Gold;
            trafficEndPath.Enabled = false;

            pathCreationMenu.RefreshIndex();
            pathCreationMenu.OnItemSelect += PathCreation_OnItemSelected;
            pathCreationMenu.OnCheckboxChange += PathCreation_OnCheckboxChange;
        }

        private static void PathCreation_OnCheckboxChange(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            if(checkboxItem == collectorWaypoint)
            {
                collectorRadius.Enabled = collectorWaypoint.Checked ? true : false;
            }
        }

        private static void PathCreation_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        { 
            // Do I need to implement a distance restriction?  Idiots place waypoints unnecessarily close, possibly causing AI to drive in circles
            if (selectedItem == trafficAddWaypoint)
            {
                var firstNonNullPath = PathMainMenu.GetPaths().Where(p => p != null && !p.PathFinished).First();
                var pathIndex = PathMainMenu.GetPaths().IndexOf(firstNonNullPath);
                var currentPath = pathIndex + 1;
                var currentWaypoint = PathMainMenu.GetPaths()[pathIndex].Waypoints.Count + 1;
                var drivingFlag = drivingFlags[waypointType.Index];
                var blip = CreateWaypointBlip(pathIndex);

                if (collectorWaypoint.Checked)
                {
                    var yieldZone = SettingsMenu.speedUnits.SelectedItem == SettingsMenu.SpeedUnitsOfMeasure.MPH
                        ? World.AddSpeedZone(Game.LocalPlayer.Character.Position, 50f, MathHelper.ConvertMilesPerHourToMetersPerSecond(waypointSpeeds[waypointSpeed.Index]))
                        : World.AddSpeedZone(Game.LocalPlayer.Character.Position, 50f, MathHelper.ConvertKilometersPerHourToMetersPerSecond(waypointSpeeds[waypointSpeed.Index]));

                    PathMainMenu.GetPaths()[pathIndex].Waypoints.Add(new Waypoint(currentPath, currentWaypoint, Game.LocalPlayer.Character.Position, SetDriveSpeedForWaypoint(), drivingFlag, blip, true, collectorRadii[collectorRadius.Index], yieldZone));
                }
                else
                {
                    PathMainMenu.GetPaths()[pathIndex].Waypoints.Add(new Waypoint(currentPath, currentWaypoint, Game.LocalPlayer.Character.Position, SetDriveSpeedForWaypoint(), drivingFlag, blip));
                }
                Game.LogTrivial($"[Path {currentPath}] Waypoint {currentWaypoint} ({drivingFlag.ToString()}) added");

                ToggleTrafficEndPathMenuItem(pathIndex);

                // Refresh the trafficMenu after a waypoint is added in order to show Continue Creating Current Path instead of Create New Path
                PathMainMenu.RefreshMenu(trafficRemoveWaypoint);
            }

            if (selectedItem == trafficRemoveWaypoint)
            {
                // Loop through each path and find the first one which isn't finished, then delete the path's last waypoint and corresponding blip
                for (int i = 0; i < PathMainMenu.GetPaths().Count; i++)
                {
                    if (PathMainMenu.GetPaths().ElementAtOrDefault(i) != null && !PathMainMenu.GetPaths()[i].PathFinished)
                    {
                        Game.LogTrivial($"[Path {i + 1}] {PathMainMenu.GetPaths()[i].Waypoints.Last().DrivingFlag.ToString()} waypoint removed");
                        PathMainMenu.GetPaths()[i].Waypoints.Last().Blip.Delete();
                        World.RemoveSpeedZone(PathMainMenu.GetPaths()[i].Waypoints.Last().YieldZone);

                        if (PathMainMenu.GetPaths()[i].Waypoints.Last().CollectorRadiusBlip)
                        {
                            PathMainMenu.GetPaths()[i].Waypoints.Last().CollectorRadiusBlip.Delete();
                        }
                        PathMainMenu.GetPaths()[i].Waypoints.RemoveAt(PathMainMenu.GetPaths()[i].Waypoints.IndexOf(PathMainMenu.GetPaths()[i].Waypoints.Last()));

                        ToggleTrafficEndPathMenuItem(i);

                        // If the path has no waypoints, disable the menu option to remove a waypoint
                        if (PathMainMenu.GetPaths()[i].Waypoints.Count == 0)
                        {
                            trafficRemoveWaypoint.Enabled = false;
                            trafficEndPath.Enabled = false;
                        }
                    }
                }
            }

            if (selectedItem == trafficEndPath)
            {
                // Loop through each path and find the first one which isn't finished
                for (int i = 0; i < PathMainMenu.GetPaths().Count; i++)
                {
                    var currentPath = PathMainMenu.GetPaths()[i];
                    if (PathMainMenu.GetPaths().ElementAtOrDefault(i) != null && !currentPath.PathFinished)
                    {
                        // If the path has one stop waypoint or at least two waypoints, finish the path and start the vehicle collector loop, else show user the error and delete any waypoints they made and clear the invalid path
                        if (currentPath.Waypoints.Count >= 2 || (currentPath.Waypoints.Count == 1 && currentPath.Waypoints[0].DrivingFlag == VehicleDrivingFlags.StopAtDestination))
                        {
                            Game.LogTrivial($"[Path Creation] Path {i + 1} finished with {currentPath.Waypoints.Count} waypoints.");
                            Game.DisplayNotification($"~o~Scene Manager\n~g~[Success]~w~ Path {i + 1} complete.");
                            currentPath.Waypoints.Last().Blip.Color = Color.OrangeRed;
                            if (currentPath.Waypoints.Last().CollectorRadiusBlip)
                            {
                                currentPath.Waypoints.Last().CollectorRadiusBlip.Color = Color.OrangeRed;
                            }
                            currentPath.FinishPath();
                            currentPath.EnablePath();
                            currentPath.SetPathNumber(i + 1);
                            PathMainMenu.AddPathToPathCountList(i, currentPath.PathNum);

                            //GameFiber InitialWaypointVehicleCollectorFiber = new GameFiber(() => TrafficPathing.InitialWaypointVehicleCollector(paths[i]));
                            //InitialWaypointVehicleCollectorFiber.Start();

                            // For each waypoint in the path's WaypointData, start a collector game fiber and loop while the path and waypoint exist, and while the path is enabled
                            foreach (Waypoint wd in PathMainMenu.GetPaths()[i].Waypoints)
                            {
                                GameFiber WaypointVehicleCollectorFiber = new GameFiber(() => TrafficPathing.StartCollectingAtWaypoint(PathMainMenu.GetPaths(), PathMainMenu.GetPaths()[i], wd));
                                WaypointVehicleCollectorFiber.Start();

                                //GameFiber AssignStopForVehiclesFlagFiber = new GameFiber(() => TrafficPathing.AssignStopForVehiclesFlag(PathMainMenu.GetPaths(), PathMainMenu.GetPaths()[i], wd));
                                //AssignStopForVehiclesFlagFiber.Start();
                            }

                            MenuManager.menuPool.CloseAllMenus();
                            PathMainMenu.pathMainMenu.Clear();
                            PathMainMenu.BuildPathMenu();
                            PathMainMenu.pathMainMenu.Visible = true;
                            break;
                        }
                        else
                        {
                            Game.LogTrivial($"[Path Error] A minimum of 2 waypoints is required.");
                            Game.DisplayNotification($"~o~Scene Manager\n~r~[Error]~w~ A minimum of 2 waypoints or one stop waypoint is required to create a path.");
                            foreach (Waypoint wp in PathMainMenu.GetPaths()[i].Waypoints)
                            {
                                wp.Blip.Delete();
                                if (wp.CollectorRadiusBlip)
                                {
                                    wp.CollectorRadiusBlip.Delete();
                                }
                            }
                            PathMainMenu.GetPaths()[i].Waypoints.Clear();
                            PathMainMenu.GetPaths().RemoveAt(i);
                            break;
                        }
                    }
                }

                // "Refresh" the menu to reflect the new path
                //TrafficMenu.RebuildTrafficMenu();
            }
        }

        private static void ToggleTrafficEndPathMenuItem(int pathIndex)
        {
            if ((PathMainMenu.GetPaths()[pathIndex].Waypoints.Count == 1 && PathMainMenu.GetPaths()[pathIndex].Waypoints.First().DrivingFlag == VehicleDrivingFlags.StopAtDestination) || (PathMainMenu.GetPaths()[pathIndex].Waypoints.Count > 1 && PathMainMenu.GetPaths()[pathIndex].Waypoints.Any(p => p.DrivingFlag != VehicleDrivingFlags.StopAtDestination)))
            {
                trafficEndPath.Enabled = true;
            }
            else
            {
                trafficEndPath.Enabled = false;
            }
        }

        private static float SetDriveSpeedForWaypoint()
        {
            float speed;
            if (SettingsMenu.speedUnits.SelectedItem == SettingsMenu.SpeedUnitsOfMeasure.MPH)
            {
                //Game.LogTrivial($"Original speed: {waypointSpeeds[waypointSpeed.Index]}{SettingsMenu.speedUnits.SelectedItem}");
                speed = MathHelper.ConvertMilesPerHourToMetersPerSecond(waypointSpeeds[waypointSpeed.Index]);
                //Game.LogTrivial($"Converted speed: {speed}m/s");
            }
            else
            {
                //Game.LogTrivial($"Original speed: {waypointSpeeds[waypointSpeed.Index]}{SettingsMenu.speedUnits.SelectedItem}");
                speed = MathHelper.ConvertKilometersPerHourToMetersPerSecond(waypointSpeeds[waypointSpeed.Index]);
                //Game.LogTrivial($"Converted speed: {speed}m/s");
            }

            return speed;
        }

        private static Blip CreateWaypointBlip(int pathIndex)
        {
            var spriteNumericalEnum = pathIndex + 17; // 17 because the numerical value of these sprites are always 17 more than the path index
            var blip = new Blip(Game.LocalPlayer.Character.Position)
            {
                Scale = 0.5f,
                Sprite = (BlipSprite)spriteNumericalEnum
            };

            if (PathMainMenu.GetPaths()[pathIndex].Waypoints.Count == 0)
            {
                blip.Color = Color.Orange;
            }
            else
            {
                blip.Color = Color.Yellow;
            }

            return blip;
        }

        public static void AddNewPathToPathsCollection(List<Path> paths, int pathIndex)
        {
            var pathNum = pathIndex + 1;
            Game.LogTrivial($"Creating path {pathNum}");
            Game.DisplayNotification($"~o~Scene Manager\n~y~[Creating]~w~ Path {pathNum} started.");
            paths.Insert(pathIndex, new Path(pathNum, false));
            trafficRemoveWaypoint.Enabled = false;
        }

        //private static void RefreshPathMainMenu()
        //{
        //    trafficRemoveWaypoint.Enabled = true;
        //    MenuManager.pathMenu.Clear();
        //    //MenuManager.pathMenu.AddItem(PathMainMenu.AddNewMenuItem())
        //    MenuManager.pathMenu.AddItem(PathMainMenu.createNewPath = new UIMenuItem("Continue Creating Current Path"));
        //    MenuManager.pathMenu.AddItem(PathMainMenu.deleteAllPaths = new UIMenuItem("Delete All Paths"));
        //    MenuManager.pathMenu.AddItem(PathMainMenu.directDriver = new UIMenuListScrollerItem<int>("Direct nearest driver to path", ""));
        //    MenuManager.pathMenu.AddItem(PathMainMenu.dismissDriver = new UIMenuListScrollerItem<string>("Dismiss nearest driver", ""));

        //    if (PathMainMenu.GetPaths().Count == 8)
        //    {
        //        PathMainMenu.createNewPath.Enabled = false;
        //    }
        //    if (PathMainMenu.GetPaths().Count == 0)
        //    {
        //        PathMainMenu.editPath.Enabled = false;
        //        PathMainMenu.deleteAllPaths.Enabled = false;
        //        PathMainMenu.disableAllPaths.Enabled = false;
        //        PathMainMenu.directDriver.Enabled = false;
        //    }
        //}
    }
}
