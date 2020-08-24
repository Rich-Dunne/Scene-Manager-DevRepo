using System;
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
        public static UIMenu editWaypointMenu { get; private set; }
        public static UIMenuItem editUpdateWaypoint { get; private set; }
        public static UIMenuItem editRemoveWaypoint { get; private set; }
        public static UIMenuNumericScrollerItem<int> editWaypoint;
        public static UIMenuListScrollerItem<string> changeWaypointType;
        public static UIMenuListScrollerItem<float> changeWaypointSpeed;
        public static UIMenuListScrollerItem<float> changeCollectorRadius;
        private static UIMenuCheckboxItem collectorWaypoint, updateWaypointPosition;

        private static List<int> pathWaypoints = new List<int>() { };
        private static float[] waypointSpeeds = new float[] { 5f, 10f, 15f, 20f, 30f, 40f, 50f, 60f, 70f };
        private static float[] collectorRadii = new float[] { 3f, 5f, 10f, 15f, 20f, 30f, 40f, 50f };
        private static VehicleDrivingFlags[] drivingFlags = new VehicleDrivingFlags[] { VehicleDrivingFlags.Normal, VehicleDrivingFlags.StopAtDestination };

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

            // Populating menu list so user can select which waypoint to edit by index
            //pathWaypoints.Clear();
            //for (int i = 0; i < currentPath.Waypoints.Count; i++)
            //{
            //    pathWaypoints.Add(i+1);
            //}
            editWaypoint = new UIMenuNumericScrollerItem<int>("Edit Waypoint", "", PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints.First().Number, PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints.Last().Number, 1);
            Game.LogTrivial($"First waypoint number: {PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints.First().Number}");
            Game.LogTrivial($"Last waypoint number: {PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints.Last().Number}");
            editWaypointMenu.Clear();
            editWaypointMenu.AddItem(editWaypoint);
            editWaypoint.Index = 0;

            editWaypointMenu.AddItem(changeWaypointType = new UIMenuListScrollerItem<string>("Change Waypoint Type", "", new [] { "Drive To", "Stop" }));
            changeWaypointType.Index = Array.IndexOf(drivingFlags, PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints[editWaypoint.Index].DrivingFlag);
            editWaypointMenu.AddItem(changeWaypointSpeed = new UIMenuListScrollerItem<float>("Change Waypoint Speed", "", waypointSpeeds));
            changeWaypointSpeed.Index = Array.IndexOf(waypointSpeeds, MathHelper.ConvertMetersPerSecondToMilesPerHour(PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints[editWaypoint.Index].Speed));
            editWaypointMenu.AddItem(collectorWaypoint = new UIMenuCheckboxItem("Collector Waypoint", PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints[editWaypoint.Index].IsCollector));
            editWaypointMenu.AddItem(changeCollectorRadius = new UIMenuListScrollerItem<float>("Change Collection Radius", "", collectorRadii));
            changeCollectorRadius.Index = Array.IndexOf(collectorRadii, PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints[editWaypoint.Index].CollectorRadius);
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
                while (editWaypointMenu.MenuItems.Count > 1)
                {
                    editWaypointMenu.RemoveItemAt(1);
                    GameFiber.Yield();
                }

                editWaypointMenu.AddItem(changeWaypointType = new UIMenuListScrollerItem<string>("Change Waypoint Type", "", new[] { "Drive To", "Stop" }));
                changeWaypointType.Index = Array.IndexOf(drivingFlags, PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints[editWaypoint.Index].DrivingFlag);
                editWaypointMenu.AddItem(changeWaypointSpeed = new UIMenuListScrollerItem<float>("Change Waypoint Speed", "", waypointSpeeds));
                changeWaypointSpeed.Index = Array.IndexOf(waypointSpeeds, MathHelper.ConvertMetersPerSecondToMilesPerHour(PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints[editWaypoint.Index].Speed));
                editWaypointMenu.AddItem(collectorWaypoint = new UIMenuCheckboxItem("Collector Waypoint", PathMainMenu.GetPaths()[PathMainMenu.editPath.Index].Waypoints[editWaypoint.Index].IsCollector));
                editWaypointMenu.AddItem(changeCollectorRadius = new UIMenuListScrollerItem<float>("Change Collection Radius", "", collectorRadii));
                changeCollectorRadius.Enabled = collectorWaypoint.Checked ? true : false;
                editWaypointMenu.AddItem(updateWaypointPosition = new UIMenuCheckboxItem("Update Waypoint Position", false));
                editWaypointMenu.AddItem(editUpdateWaypoint = new UIMenuItem("Update Waypoint"));
                editUpdateWaypoint.ForeColor = Color.Gold;
                editWaypointMenu.AddItem(editRemoveWaypoint = new UIMenuItem("Remove Waypoint"));
                editRemoveWaypoint.ForeColor = Color.Gold;
                //editWaypointMenu.RefreshIndex();
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
                currentWaypoint.UpdateWaypoint(currentWaypoint, drivingFlags[changeWaypointType.Index], changeWaypointSpeed.SelectedItem, collectorWaypoint.Checked, changeCollectorRadius.SelectedItem, updateWaypointPosition.Checked);
                //currentWaypoint.UpdateWaypoint(currentWaypoint, drivingFlags[changeWaypointType.Index], waypointSpeeds[changeWaypointSpeed.Index], collectorWaypoint.Checked, collectorRadii[changeCollectorRadius.Index], updateWaypointPosition.Checked);
                Game.LogTrivial($"Updated path {currentPath.PathNum} waypoint {currentWaypoint.Number}: Driving flag is {drivingFlags[changeWaypointType.Index].ToString()}, speed is {waypointSpeeds[changeWaypointSpeed.Index].ToString()}, collector is {currentWaypoint.IsCollector}");

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
                    pathWaypoints.RemoveAt(editWaypoint.Index);

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
    }
}
