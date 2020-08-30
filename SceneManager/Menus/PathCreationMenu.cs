using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    class PathCreationMenu
    {

        private static VehicleDrivingFlags[] drivingFlags = new VehicleDrivingFlags[] { VehicleDrivingFlags.Normal, VehicleDrivingFlags.StopAtDestination }; // Implement custom driving flag for normal
        private static string[] waypointTypes = new string[] { "Drive To", "Stop" };
        public static UIMenu pathCreationMenu { get; private set; }
        private static UIMenuItem trafficAddWaypoint, trafficRemoveWaypoint, trafficEndPath;
        public static UIMenuListScrollerItem<string> waypointType = new UIMenuListScrollerItem<string>("Waypoint Type", "", waypointTypes);
        private static UIMenuNumericScrollerItem<int> waypointSpeed;
        public static UIMenuCheckboxItem collectorWaypoint = new UIMenuCheckboxItem("Collector", true, "If this waypoint will collect vehicles to follow the path");
        public static UIMenuNumericScrollerItem<int> collectorRadius = new UIMenuNumericScrollerItem<int>("Collection Radius", "The distance from this waypoint (in meters) vehicles will be collected", 1, 50, 1);
        public static UIMenuNumericScrollerItem<int> speedZoneRadius = new UIMenuNumericScrollerItem<int>("Speed Zone Radius", "The distance from this collector waypoint (in meters) non-collected vehicles will drive at this waypoint's speed", 1, 50, 1);

        internal static void InstantiateMenu()
        {
            pathCreationMenu = new UIMenu("Scene Menu", "~o~Path Creation");
            pathCreationMenu.ParentMenu = PathMainMenu.pathMainMenu;
            MenuManager.menuPool.Add(pathCreationMenu);
        }

        public static void BuildPathCreationMenu()
        {
            pathCreationMenu.AddItem(waypointType);
            pathCreationMenu.AddItem(waypointSpeed = new UIMenuNumericScrollerItem<int>("Waypoint Speed", $"How fast the AI will drive to the waypoint in ~b~{SettingsMenu.speedUnits.SelectedItem}", 5, 80, 5));
            waypointSpeed.Index = 0;
            pathCreationMenu.AddItem(collectorWaypoint);
            pathCreationMenu.AddItem(collectorRadius);
            collectorRadius.Index = 0;
            pathCreationMenu.AddItem(speedZoneRadius);
            speedZoneRadius.Index = 0;
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
            pathCreationMenu.OnCheckboxChange += PathCreation_OnCheckboxChanged;
        }

        private static void PathCreation_OnCheckboxChanged(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            if(checkboxItem == collectorWaypoint)
            {
                collectorRadius.Enabled = collectorWaypoint.Checked ? true : false;
                speedZoneRadius.Enabled = collectorWaypoint.Checked ? true : false;
            }
        }

        private static void PathCreation_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        { 
            // Do I need to implement a distance restriction?  Idiots place waypoints unnecessarily close, possibly causing AI to drive in circles
            if (selectedItem == trafficAddWaypoint)
            {
                var anyPathsExist = PathMainMenu.GetPaths().Count > 0;

                // If no paths exist, then add a new path to the collection at index 0.  If paths do exist, then we want to add a new path at the first null index if there are no non-null paths where pathFinished = false
                if (!anyPathsExist)
                {
                    AddNewPathToPathsCollection(PathMainMenu.GetPaths(), 0);

                    if (SettingsMenu.debugGraphics.Checked)
                    {
                        DebugGraphics.LoopToDrawDebugGraphics(SettingsMenu.debugGraphics, PathMainMenu.GetPaths()[0]);
                    }
                }
                else if(anyPathsExist && !PathMainMenu.GetPaths().Any(p => p != null && p.State == State.Creating))
                {
                    AddNewPathToPathsCollection(PathMainMenu.GetPaths(), PathMainMenu.GetPaths().IndexOf(PathMainMenu.GetPaths().Where(p => p.State == State.Finished).First()) + 1);

                    if (SettingsMenu.debugGraphics.Checked)
                    {
                        DebugGraphics.LoopToDrawDebugGraphics(SettingsMenu.debugGraphics, PathMainMenu.GetPaths().Where(p => p != null && p.State == State.Creating).First());
                    }
                }

                var firstNonNullPath = PathMainMenu.GetPaths().Where(p => p != null && p.State == State.Creating).First();
                var pathIndex = PathMainMenu.GetPaths().IndexOf(firstNonNullPath);
                var currentPath = firstNonNullPath.PathNum;
                var currentWaypoint = PathMainMenu.GetPaths()[pathIndex].Waypoints.Count + 1;
                var drivingFlag = drivingFlags[waypointType.Index];
                var blip = CreateWaypointBlip(pathIndex);

                if (collectorWaypoint.Checked)
                {
                    var yieldZone = SettingsMenu.speedUnits.SelectedItem == SettingsMenu.SpeedUnitsOfMeasure.MPH
                        ? World.AddSpeedZone(Game.LocalPlayer.Character.Position, speedZoneRadius.Value, MathHelper.ConvertMilesPerHourToMetersPerSecond(waypointSpeed.Value))
                        : World.AddSpeedZone(Game.LocalPlayer.Character.Position, speedZoneRadius.Value, MathHelper.ConvertKilometersPerHourToMetersPerSecond(waypointSpeed.Value));
                    PathMainMenu.GetPaths()[pathIndex].Waypoints.Add(new Waypoint(currentPath, currentWaypoint, Game.LocalPlayer.Character.Position, SetDriveSpeedForWaypoint(), drivingFlag, blip, true, collectorRadius.Value, speedZoneRadius.Value, yieldZone));
                }
                else
                {
                    PathMainMenu.GetPaths()[pathIndex].Waypoints.Add(new Waypoint(currentPath, currentWaypoint, Game.LocalPlayer.Character.Position, SetDriveSpeedForWaypoint(), drivingFlag, blip));
                }
                Game.LogTrivial($"[Path {currentPath}] Waypoint {currentWaypoint} ({drivingFlag.ToString()}) added");

                ToggleTrafficEndPathMenuItem(pathIndex);
                trafficRemoveWaypoint.Enabled = true;
                PathMainMenu.createNewPath.Text = $"Continue Creating Path {currentPath}";
            }

            if (selectedItem == trafficRemoveWaypoint)
            {
                // Loop through each path and find the first one which isn't finished, then delete the path's last waypoint and corresponding blip
                for (int i = 0; i < PathMainMenu.GetPaths().Count; i++)
                {
                    if (PathMainMenu.GetPaths().ElementAtOrDefault(i) != null && PathMainMenu.GetPaths()[i].State == State.Creating)
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
                    if (PathMainMenu.GetPaths().ElementAtOrDefault(i) != null && currentPath.State == State.Creating)
                    {
                        Game.LogTrivial($"[Path Creation] Path {currentPath.PathNum} finished with {currentPath.Waypoints.Count} waypoints.");
                        Game.DisplayNotification($"~o~Scene Manager\n~g~[Success]~w~ Path {i + 1} complete.");
                        currentPath.Waypoints.Last().Blip.Color = Color.OrangeRed;
                        if (currentPath.Waypoints.Last().CollectorRadiusBlip)
                        {
                            currentPath.Waypoints.Last().CollectorRadiusBlip.Color = Color.OrangeRed;
                        }
                        currentPath.State = State.Finished;
                        //currentPath.FinishPath();
                        currentPath.EnablePath();
                        currentPath.SetPathNumber(i + 1);
                        PathMainMenu.AddPathToPathCountList(i, currentPath.PathNum);

                        // For each waypoint in the path's WaypointData, start a collector game fiber and loop while the path and waypoint exist, and while the path is enabled
                        foreach (Waypoint waypoint in PathMainMenu.GetPaths()[i].Waypoints)
                        {
                            GameFiber WaypointVehicleCollectorFiber = new GameFiber(() => VehicleCollector.StartCollectingAtWaypoint(PathMainMenu.GetPaths(), PathMainMenu.GetPaths()[i], waypoint));
                            WaypointVehicleCollectorFiber.Start();
                        }

                        MenuManager.menuPool.CloseAllMenus();
                        //pathCreationMenu.Reset(true, true); // Trying to see if we can get away with resetting the menu instead of rebuilding it
                        PathMainMenu.createNewPath.Text = "Create New Path";
                        PathMainMenu.pathMainMenu.Clear();
                        PathMainMenu.BuildPathMenu();
                        trafficEndPath.Enabled = false;
                        PathMainMenu.pathMainMenu.Visible = true;
                        break;
                    }
                }
            }
        }

        private static void ToggleTrafficEndPathMenuItem(int pathIndex)
        {
            if (PathMainMenu.GetPaths()[pathIndex].Waypoints.Count > 0)
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
            float convertedSpeed;
            if (SettingsMenu.speedUnits.SelectedItem == SettingsMenu.SpeedUnitsOfMeasure.MPH)
            {
                //Game.LogTrivial($"Original speed: {waypointSpeeds[waypointSpeed.Index]}{SettingsMenu.speedUnits.SelectedItem}");
                convertedSpeed = MathHelper.ConvertMilesPerHourToMetersPerSecond(waypointSpeed.Value);
                //Game.LogTrivial($"Converted speed: {convertedSpeed}m/s");
            }
            else
            {
                //Game.LogTrivial($"Original speed: {waypointSpeeds[waypointSpeed.Index]}{SettingsMenu.speedUnits.SelectedItem}");
                convertedSpeed = MathHelper.ConvertKilometersPerHourToMetersPerSecond(waypointSpeed.Value);
                //Game.LogTrivial($"Converted speed: {convertedSpeed}m/s");
            }

            return convertedSpeed;
        }

        public static Blip CreateWaypointBlip(int pathIndex)
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
            paths.Insert(pathIndex, new Path(pathNum, State.Creating));
            trafficRemoveWaypoint.Enabled = false;
            trafficEndPath.Enabled = false;
        }
    }
}
