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
        #pragma warning disable CS0618 // Type or member is obsolete, clear NUI squiggles
        public static UIMenuItem trafficAddWaypoint, trafficRemoveWaypoint, trafficEndPath;
        public static UIMenuListItem waypointType, waypointSpeed, collectorRadius;
        private static UIMenuCheckboxItem collectorWaypoint;

        private static List<dynamic> waypointSpeeds = new List<dynamic>() { 5f, 10f, 15f, 20f, 30f, 40f, 50f, 60f, 70f };
        //private enum waypointTypes {DriveTo, Stop };
        private static List<dynamic> waypointTypes = new List<dynamic>() { "Drive To", "Stop" };
        private static List<dynamic> collectorRadii = new List<dynamic>() { 3f, 5f, 10f, 15f, 20f, 30f, 40f, 50f };
        private static VehicleDrivingFlags[] drivingFlags = new VehicleDrivingFlags[] { VehicleDrivingFlags.Normal, VehicleDrivingFlags.StopAtDestination }; // Implement custom driving flag for normal

        // Called from EditPathMenu
        public static void BuildPathCreationMenu()
        {
            MenuManager.pathCreationMenu.AddItem(waypointType = new UIMenuListItem("Waypoint Type", waypointTypes, 0));
            MenuManager.pathCreationMenu.AddItem(waypointSpeed = new UIMenuListItem($"Waypoint Speed (in {SettingsMenu.speedUnits.SelectedItem})", waypointSpeeds, 0));
            MenuManager.pathCreationMenu.AddItem(collectorWaypoint = new UIMenuCheckboxItem("Collector", true)); // true if path's first waypoint
            MenuManager.pathCreationMenu.AddItem(collectorRadius = new UIMenuListItem("Collection Radius", collectorRadii, 0));
            MenuManager.pathCreationMenu.AddItem(trafficAddWaypoint = new UIMenuItem("Add waypoint"));
            MenuManager.pathCreationMenu.AddItem(trafficRemoveWaypoint = new UIMenuItem("Remove last waypoint"));
            trafficRemoveWaypoint.Enabled = false;
            MenuManager.pathCreationMenu.AddItem(trafficEndPath = new UIMenuItem("End path creation"));

            MenuManager.pathCreationMenu.RefreshIndex();
            MenuManager.pathCreationMenu.OnItemSelect += PathCreation_OnItemSelected;
        }

        private static void PathCreation_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        { 
            // Do I need to implement a distance restriction?  Idiots place waypoints unnecessarily close, possibly causing AI to drive in circles
            if (selectedItem == trafficAddWaypoint)
            {
                var firstNonNullPath = TrafficMenu.paths.Where(p => p != null && !p.PathFinished).First();
                var pathIndex = TrafficMenu.paths.IndexOf(firstNonNullPath);
                var currentPath = pathIndex + 1;
                var currentWaypoint = TrafficMenu.paths[pathIndex].Waypoint.Count + 1;
                var drivingFlag = drivingFlags[waypointType.Index];
                var blip = CreateWaypointBlip(pathIndex);

                if (collectorWaypoint.Checked) // && is path's first waypoint
                {
                    var yieldZone = SettingsMenu.speedUnits.SelectedItem == SettingsMenu.SpeedUnitsOfMeasure.MPH
                        ? (uint)World.AddSpeedZone(Game.LocalPlayer.Character.Position, 50f, MathHelper.ConvertMilesPerHourToMetersPerSecond(waypointSpeeds[waypointSpeed.Index]))
                        : (uint)World.AddSpeedZone(Game.LocalPlayer.Character.Position, 50f, MathHelper.ConvertKilometersPerHourToMetersPerSecond(waypointSpeeds[waypointSpeed.Index]));

                    TrafficMenu.paths[pathIndex].Waypoint.Add(new Waypoint(currentPath, currentWaypoint, Game.LocalPlayer.Character.Position, SetDriveSpeedForWaypoint(), drivingFlag, blip, true, collectorRadii[collectorRadius.Index], yieldZone));
                }
                else
                {
                    TrafficMenu.paths[pathIndex].Waypoint.Add(new Waypoint(currentPath, currentWaypoint, Game.LocalPlayer.Character.Position, SetDriveSpeedForWaypoint(), drivingFlag, blip));
                }
                Game.LogTrivial($"[Path {currentPath}] Waypoint {currentWaypoint} ({drivingFlag.ToString()}) added");

                // Refresh the trafficMenu after a waypoint is added in order to show Continue Creating Current Path instead of Create New Path
                RefreshTrafficMenu();
            }

            if (selectedItem == trafficRemoveWaypoint)
            {
                // Loop through each path and find the first one which isn't finished, then delete the path's last waypoint and corresponding blip
                for (int i = 0; i < TrafficMenu.paths.Count; i++)
                {
                    if (TrafficMenu.paths.ElementAtOrDefault(i) != null && !TrafficMenu.paths[i].PathFinished)
                    {
                        Game.LogTrivial($"[Path {i + 1}] {TrafficMenu.paths[i].Waypoint.Last().DrivingFlag.ToString()} waypoint removed");
                        TrafficMenu.paths[i].Waypoint.Last().Blip.Delete();
                        if (TrafficMenu.paths[i].Waypoint.Last().CollectorRadiusBlip)
                        {
                            TrafficMenu.paths[i].Waypoint.Last().CollectorRadiusBlip.Delete();
                        }
                        TrafficMenu.paths[i].Waypoint.RemoveAt(TrafficMenu.paths[i].Waypoint.IndexOf(TrafficMenu.paths[i].Waypoint.Last()));

                        // If the path has no waypoints, disable the menu option to remove a waypoint
                        if (TrafficMenu.paths[i].Waypoint.Count == 0)
                        {
                            trafficRemoveWaypoint.Enabled = false;
                        }
                    }
                }
            }

            if (selectedItem == trafficEndPath)
            {
                // Loop through each path and find the first one which isn't finished
                for (int i = 0; i < TrafficMenu.paths.Count; i++)
                {
                    if (TrafficMenu.paths.ElementAtOrDefault(i) != null && !TrafficMenu.paths[i].PathFinished)
                    {
                        // If the path has one stop waypoint or at least two waypoints, finish the path and start the vehicle collector loop, else show user the error and delete any waypoints they made and clear the invalid path
                        if (TrafficMenu.paths[i].Waypoint.Count >= 2 || (TrafficMenu.paths[i].Waypoint.Count == 1 && TrafficMenu.paths[i].Waypoint[0].DrivingFlag == VehicleDrivingFlags.StopAtDestination))
                        {
                            Game.LogTrivial($"[Path Creation] Path {i + 1} finished with {TrafficMenu.paths[i].Waypoint.Count} waypoints.");
                            Game.DisplayNotification($"~o~Scene Manager\n~g~[Success]~w~ Path {i + 1} complete.");
                            TrafficMenu.paths[i].Waypoint.Last().Blip.Color = Color.OrangeRed;
                            if (TrafficMenu.paths[i].Waypoint.Last().CollectorRadiusBlip)
                            {
                                TrafficMenu.paths[i].Waypoint.Last().CollectorRadiusBlip.Color = Color.OrangeRed;
                            }
                            TrafficMenu.paths[i].PathFinished = true;
                            TrafficMenu.paths[i].PathDisabled = false;
                            TrafficMenu.paths[i].PathNum = i + 1;
                            TrafficMenu.pathsNum.Insert(i, TrafficMenu.paths[i].PathNum);

                            //GameFiber InitialWaypointVehicleCollectorFiber = new GameFiber(() => TrafficPathing.InitialWaypointVehicleCollector(paths[i]));
                            //InitialWaypointVehicleCollectorFiber.Start();

                            // For each waypoint in the path's WaypointData, start a collector game fiber and loop while the path and waypoint exist, and while the path is enabled
                            foreach (Waypoint wd in TrafficMenu.paths[i].Waypoint)
                            {
                                GameFiber WaypointVehicleCollectorFiber = new GameFiber(() => TrafficPathing.WaypointVehicleCollector(TrafficMenu.paths, TrafficMenu.paths[i], wd));
                                WaypointVehicleCollectorFiber.Start();

                                GameFiber AssignStopForVehiclesFlagFiber = new GameFiber(() => TrafficPathing.AssignStopForVehiclesFlag(TrafficMenu.paths, TrafficMenu.paths[i], wd));
                                AssignStopForVehiclesFlagFiber.Start();
                            }

                            MenuManager.menuPool.CloseAllMenus();
                            MenuManager.pathMenu.Clear();
                            TrafficMenu.BuildPathMenu();
                            MenuManager.pathMenu.Visible = true;
                            break;
                        }
                        else
                        {
                            Game.LogTrivial($"[Path Error] A minimum of 2 waypoints is required.");
                            Game.DisplayNotification($"~o~Scene Manager\n~r~[Error]~w~ A minimum of 2 waypoints or one stop waypoint is required to create a path.");
                            foreach (Waypoint wp in TrafficMenu.paths[i].Waypoint)
                            {
                                wp.Blip.Delete();
                                if (wp.CollectorRadiusBlip)
                                {
                                    wp.CollectorRadiusBlip.Delete();
                                }
                            }
                            TrafficMenu.paths[i].Waypoint.Clear();
                            TrafficMenu.paths.RemoveAt(i);
                            break;
                        }
                    }
                }

                // "Refresh" the menu to reflect the new path
                //TrafficMenu.RebuildTrafficMenu();
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

            if (TrafficMenu.paths[pathIndex].Waypoint.Count == 0)
            {
                blip.Color = Color.Orange;
            }
            else
            {
                blip.Color = Color.Yellow;
            }

            return blip;
        }

        private static void RefreshTrafficMenu()
        {
            trafficRemoveWaypoint.Enabled = true;
            MenuManager.pathMenu.Clear();
            MenuManager.pathMenu.AddItem(TrafficMenu.createNewPath = new UIMenuItem("Continue Creating Current Path"));
            MenuManager.pathMenu.AddItem(TrafficMenu.deleteAllPaths = new UIMenuItem("Delete All Paths"));
            MenuManager.pathMenu.AddItem(TrafficMenu.directDriver = new UIMenuListItem("Direct nearest driver to path", TrafficMenu.pathsNum, 0));
            MenuManager.pathMenu.AddItem(TrafficMenu.dismissDriver = new UIMenuListItem("Dismiss nearest driver", TrafficMenu.dismissOptions, 0));

            if (TrafficMenu.paths.Count == 8)
            {
                TrafficMenu.createNewPath.Enabled = false;
            }
            if (TrafficMenu.paths.Count == 0)
            {
                TrafficMenu.editPath.Enabled = false;
                TrafficMenu.deleteAllPaths.Enabled = false;
                TrafficMenu.disableAllPaths.Enabled = false;
                TrafficMenu.directDriver.Enabled = false;
            }
        }
    }
}
