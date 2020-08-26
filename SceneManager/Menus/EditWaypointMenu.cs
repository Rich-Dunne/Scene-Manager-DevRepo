﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    class EditWaypointMenu
    {
        private static VehicleDrivingFlags[] drivingFlags = new VehicleDrivingFlags[] { VehicleDrivingFlags.Normal, VehicleDrivingFlags.StopAtDestination };
        private static string[] waypointTypes = new string[] { "Drive To", "Stop" };
        public static UIMenu editWaypointMenu { get; private set; }
        public static UIMenuItem editUpdateWaypoint { get; private set; }
        public static UIMenuItem editRemoveWaypoint { get; private set; }
        public static UIMenuNumericScrollerItem<int> editWaypoint;
        private static UIMenuListScrollerItem<string> changeWaypointType = new UIMenuListScrollerItem<string>("New Waypoint Type", "", waypointTypes);
        private static UIMenuNumericScrollerItem<int> changeWaypointSpeed;
        private static UIMenuNumericScrollerItem<int> changeCollectorRadius = new UIMenuNumericScrollerItem<int>("New Collection Radius", "The distance from this waypoint in meters vehicles will be collected", 1, 50, 1);
        private static UIMenuCheckboxItem collectorWaypoint = new UIMenuCheckboxItem("Collector", true, "If this waypoint will collect vehicles to follow the path");
        private static UIMenuCheckboxItem updateWaypointPosition;

        internal static void InstantiateMenu()
        {
            editWaypointMenu = new UIMenu("Scene Manager", "~o~Edit Waypoint");
            editWaypointMenu.ParentMenu = EditPathMenu.editPathMenu;
            MenuManager.menuPool.Add(editWaypointMenu);
        }

        public static void BuildEditWaypointMenu()
        {
            // Need to unsubscribe from these or else there will be duplicate firings if the user left the menu, then re-entered
            editWaypointMenu.OnItemSelect -= EditWaypoint_OnItemSelected;
            editWaypointMenu.OnCheckboxChange -= EditWaypoint_OnCheckboxChanged;
            editWaypointMenu.OnScrollerChange -= EditWaypoint_OnScrollerChanged;

            var currentPath = PathMainMenu.GetPaths()[PathMainMenu.editPath.Index];

            editWaypoint = new UIMenuNumericScrollerItem<int>("Edit Waypoint", "", PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints.First().Number, PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints.Last().Number, 1);

            editWaypointMenu.Clear();
            editWaypointMenu.AddItem(editWaypoint);
            editWaypoint.Index = 0;

            editWaypointMenu.AddItem(changeWaypointType);
            changeWaypointType.Index = Array.IndexOf(drivingFlags, PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints[editWaypoint.Index].DrivingFlag);

            //Game.LogTrivial($"Waypoint speed: {PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints[editWaypoint.Index].Speed}");
            editWaypointMenu.AddItem(changeWaypointSpeed = new UIMenuNumericScrollerItem<int>("New Waypoint Speed", $"How fast the AI will drive to the waypoint in ~b~{SettingsMenu.speedUnits.SelectedItem}", 5, 80, 5));
            changeWaypointSpeed.Value = (int)MathHelper.ConvertMetersPerSecondToMilesPerHour(PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints[editWaypoint.Index].Speed);

            editWaypointMenu.AddItem(collectorWaypoint = new UIMenuCheckboxItem("Collector Waypoint", PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints[editWaypoint.Index].IsCollector));

            editWaypointMenu.AddItem(changeCollectorRadius);
            changeCollectorRadius.Value = PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints[editWaypoint.Index].CollectorRadius != 0
                ? (int)PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints[editWaypoint.Index].CollectorRadius
                : changeCollectorRadius.Minimum;

            editWaypointMenu.AddItem(updateWaypointPosition = new UIMenuCheckboxItem("Update Waypoint Position", false));
            editWaypointMenu.AddItem(editUpdateWaypoint = new UIMenuItem("Update Waypoint"));
            editUpdateWaypoint.ForeColor = Color.Gold;
            editWaypointMenu.AddItem(editRemoveWaypoint = new UIMenuItem("Remove Waypoint"));
            editRemoveWaypoint.ForeColor = Color.Gold;

            EditPathMenu.editPathMenu.Visible = false;
            editWaypointMenu.RefreshIndex();
            editWaypointMenu.Visible = true;

            editWaypointMenu.OnScrollerChange += EditWaypoint_OnScrollerChanged;
            editWaypointMenu.OnCheckboxChange += EditWaypoint_OnCheckboxChanged;
            editWaypointMenu.OnItemSelect += EditWaypoint_OnItemSelected;
        }

        private static void EditWaypoint_OnScrollerChanged(UIMenu sender, UIMenuScrollerItem scrollerItem, int first, int last)
        {
            if(scrollerItem == editWaypoint)
            {
                changeWaypointType.Index = Array.IndexOf(drivingFlags, PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints[editWaypoint.Index].DrivingFlag);
                //changeWaypointSpeed.Index = Array.IndexOf(waypointSpeeds, MathHelper.ConvertMetersPerSecondToMilesPerHour(PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints[editWaypoint.Index].Speed));
                changeWaypointSpeed.Value = (int)MathHelper.ConvertMetersPerSecondToMilesPerHour(PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints[editWaypoint.Index].Speed);
                collectorWaypoint.Checked = PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints[editWaypoint.Index].IsCollector;
                changeCollectorRadius.Enabled = collectorWaypoint.Checked ? true : false;
            }
        }

        private static void EditWaypoint_OnCheckboxChanged(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            if (checkboxItem == collectorWaypoint)
            {
                changeCollectorRadius.Enabled = collectorWaypoint.Checked ? true : false;
            }
        }

        private static void EditWaypoint_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            var currentPath = PathMainMenu.GetPaths()[PathMainMenu.editPath.Index];
            var currentWaypoint = currentPath.Waypoints[editWaypoint.Index];

            if (selectedItem == editUpdateWaypoint)
            {
                currentWaypoint.UpdateWaypoint(currentWaypoint, drivingFlags[changeWaypointType.Index], SetDriveSpeedForWaypoint(), collectorWaypoint.Checked, changeCollectorRadius.Value, updateWaypointPosition.Checked);
                Game.LogTrivial($"Updated path {currentPath.PathNum} waypoint {currentWaypoint.Number}: Driving flag is {drivingFlags[changeWaypointType.Index].ToString()}, speed is {changeWaypointSpeed.Value}, collector is {currentWaypoint.IsCollector}");

                if (currentPath.Waypoints.Count < 2 && currentPath.Waypoints[0].DrivingFlag == VehicleDrivingFlags.StopAtDestination)
                {
                    Game.LogTrivial($"The remaining waypoint was updated to be a stop waypoint.  Enabling/disabling the path is no longer locked.");
                    EditPathMenu.togglePath.Enabled = true;
                }

                Game.DisplayNotification($"~o~Scene Manager\n~g~[Success]~w~ Waypoint {currentWaypoint.Number} updated.");
            }

            if (selectedItem == editRemoveWaypoint)
            {
                Game.LogTrivial($"[Path {currentPath.PathNum}] Waypoint {currentWaypoint.Number} ({currentWaypoint.DrivingFlag}) removed");
                if (currentPath.Waypoints.Count == 1)
                {
                    Game.LogTrivial($"Deleting the last waypoint from the path.");
                    PathMainMenu.DeletePath(currentPath, currentPath.PathNum - 1, PathMainMenu.Delete.Single);

                    editWaypointMenu.Visible = false;
                    PathMainMenu.pathMainMenu.Visible = true;
                }
                else
                {
                    currentWaypoint.Blip.Delete();
                    if (currentWaypoint.CollectorRadiusBlip)
                    {
                        currentWaypoint.CollectorRadiusBlip.Delete();
                    }
                    currentPath.Waypoints.Remove(currentWaypoint);

                    foreach (Waypoint wp in currentPath.Waypoints)
                    {
                        wp.UpdateWaypointNumber(currentPath.Waypoints.IndexOf(wp) + 1);
                        Game.LogTrivial($"Waypoint at index {currentPath.Waypoints.IndexOf(wp)} is now waypoint #{wp.Number}");
                    }

                    BuildEditWaypointMenu();

                    if (currentPath.Waypoints.Count == 1 && currentPath.Waypoints[0].DrivingFlag != VehicleDrivingFlags.StopAtDestination)
                    {
                        Game.LogTrivial($"The path only has 1 waypoint left, and the waypoint is not a stop waypoint.  Disabling the path.");
                        currentPath.DisablePath();
                        EditPathMenu.togglePath.Checked = true;
                        EditPathMenu.togglePath.Enabled = false;
                    }
                }
            }
        }

        private static float SetDriveSpeedForWaypoint()
        {
            float convertedSpeed;
            if (SettingsMenu.speedUnits.SelectedItem == SettingsMenu.SpeedUnitsOfMeasure.MPH)
            {
                //Game.LogTrivial($"Original speed: {waypointSpeeds[waypointSpeed.Index]}{SettingsMenu.speedUnits.SelectedItem}");
                convertedSpeed = MathHelper.ConvertMilesPerHourToMetersPerSecond(changeWaypointSpeed.Value);
                //Game.LogTrivial($"Converted speed: {convertedSpeed}m/s");
            }
            else
            {
                //Game.LogTrivial($"Original speed: {waypointSpeeds[waypointSpeed.Index]}{SettingsMenu.speedUnits.SelectedItem}");
                convertedSpeed = MathHelper.ConvertKilometersPerHourToMetersPerSecond(changeWaypointSpeed.Value);
                //Game.LogTrivial($"Converted speed: {convertedSpeed}m/s");
            }

            return convertedSpeed;
        }
    }
}
