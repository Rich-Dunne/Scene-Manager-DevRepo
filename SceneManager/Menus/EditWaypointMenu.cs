using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    class EditWaypointMenu
    {
        #pragma warning disable CS0618 // Type or member is obsolete, clear NUI squiggles
        private static UIMenuItem editUpdateWaypoint, editRemoveWaypoint;
        private static UIMenuListItem editWaypoint, changeWaypointType, changeWaypointSpeed, changeCollectorRadius;
        private static UIMenuCheckboxItem collectorWaypoint, updateWaypointPosition;

        private static List<dynamic> pathWaypoints = new List<dynamic>() { };
        private static List<dynamic> waypointSpeeds = new List<dynamic>() { 5f, 10f, 15f, 20f, 30f, 40f, 50f, 60f, 70f };
        private static List<dynamic> waypointTypes = new List<dynamic>() { "Drive To", "Stop" };
        private static List<dynamic> collectorRadii = new List<dynamic>() { 3f, 5f, 10f, 15f, 20f, 30f, 40f, 50f };
        private static VehicleDrivingFlags[] drivingFlags = new VehicleDrivingFlags[] { VehicleDrivingFlags.Normal, VehicleDrivingFlags.StopAtDestination };

        public static void BuildEditWaypointMenu()
        {
            // Need to unsubscribe from these or else there will be duplicate firings if the user left the menu, then re-entered
            MenuManager.editWaypointMenu.OnItemSelect -= EditWaypoint_OnItemSelected;
            MenuManager.editWaypointMenu.OnListChange -= EditWaypoint_OnListChanged;

            var currentPath = TrafficMenu.paths[TrafficMenu.editPath.Index];

            // Populating menu list so user can select which waypoint to edit by index
            pathWaypoints.Clear();
            for (int i = 0; i < currentPath.Waypoints.Count; i++)
            {
                pathWaypoints.Add(i + 1);
            }

            MenuManager.editWaypointMenu.Clear();
            MenuManager.editWaypointMenu.AddItem(editWaypoint = new UIMenuListItem("Edit Waypoint", pathWaypoints, 0));
            MenuManager.editWaypointMenu.AddItem(changeWaypointType = new UIMenuListItem("Change Waypoint Type", waypointTypes, Array.IndexOf(drivingFlags, currentPath.Waypoints[editWaypoint.Index].DrivingFlag)));
            MenuManager.editWaypointMenu.AddItem(changeWaypointSpeed = new UIMenuListItem("Change Waypoint Speed", waypointSpeeds, waypointSpeeds.IndexOf(currentPath.Waypoints[editWaypoint.Index].Speed)));
            MenuManager.editWaypointMenu.AddItem(collectorWaypoint = new UIMenuCheckboxItem("Collector Waypoint", TrafficMenu.paths[TrafficMenu.editPath.Index].Waypoints[editWaypoint.Index].Collector));
            MenuManager.editWaypointMenu.AddItem(changeCollectorRadius = new UIMenuListItem("Change Collection Radius", collectorRadii, collectorRadii.IndexOf(currentPath.Waypoints[editWaypoint.Index].CollectorRadius)));
            MenuManager.editWaypointMenu.AddItem(updateWaypointPosition = new UIMenuCheckboxItem("Update Waypoint Position", false));
            MenuManager.editWaypointMenu.AddItem(editUpdateWaypoint = new UIMenuItem("Update Waypoint"));
            MenuManager.editWaypointMenu.AddItem(editRemoveWaypoint = new UIMenuItem("Remove Waypoint"));

            MenuManager.editPathMenu.Visible = false;
            MenuManager.editWaypointMenu.RefreshIndex();
            MenuManager.editWaypointMenu.Visible = true;

            MenuManager.editWaypointMenu.OnItemSelect += EditWaypoint_OnItemSelected;
            MenuManager.editWaypointMenu.OnListChange += EditWaypoint_OnListChanged;
        }

        private static void EditWaypoint_OnListChanged(UIMenu sender, UIMenuListItem listItem, int index)
        {
            var currentPath = TrafficMenu.paths[TrafficMenu.editPath.Index];
            var currentWaypoint = currentPath.Waypoints[editWaypoint.Index];

            if (listItem == editWaypoint)
            {
                while (MenuManager.editWaypointMenu.MenuItems.Count > 1)
                {
                    MenuManager.editWaypointMenu.RemoveItemAt(1);
                    GameFiber.Yield();
                }

                MenuManager.editWaypointMenu.AddItem(changeWaypointType = new UIMenuListItem("Change Waypoint Type", waypointTypes, Array.IndexOf(drivingFlags, currentWaypoint.DrivingFlag)));
                MenuManager.editWaypointMenu.AddItem(changeWaypointSpeed = new UIMenuListItem("Change Waypoint Speed", waypointSpeeds, waypointSpeeds.IndexOf(currentWaypoint.Speed)));
                MenuManager.editWaypointMenu.AddItem(collectorWaypoint = new UIMenuCheckboxItem("Attractor Waypoint", currentWaypoint.Collector));
                MenuManager.editWaypointMenu.AddItem(changeCollectorRadius = new UIMenuListItem("Change Collection Radius", collectorRadii, collectorRadii.IndexOf(currentPath.Waypoints[editWaypoint.Index].CollectorRadius)));
                MenuManager.editWaypointMenu.AddItem(updateWaypointPosition = new UIMenuCheckboxItem("Update Waypoint Position", false));
                MenuManager.editWaypointMenu.AddItem(editUpdateWaypoint = new UIMenuItem("Update Waypoint"));
                MenuManager.editWaypointMenu.AddItem(editRemoveWaypoint = new UIMenuItem("Remove Waypoint"));
                MenuManager.editWaypointMenu.RefreshIndex();
            }
        }

        private static void EditWaypoint_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            var currentPath = TrafficMenu.paths[TrafficMenu.editPath.Index];
            var currentWaypoint = currentPath.Waypoints[editWaypoint.Index];

            if (selectedItem == editUpdateWaypoint)
            {
                currentWaypoint.UpdateWaypoint(currentWaypoint, drivingFlags[changeWaypointType.Index], waypointSpeeds[changeWaypointSpeed.Index], collectorWaypoint.Checked, collectorRadii[changeCollectorRadius.Index], updateWaypointPosition.Checked);
                Game.LogTrivial($"Updated path {currentPath.PathNum} waypoint {currentWaypoint.Number}: Driving flag is {drivingFlags[changeWaypointType.Index].ToString()}, speed is {waypointSpeeds[changeWaypointSpeed.Index].ToString()}, collector is {currentWaypoint.Collector}");

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
                    TrafficMenu.DeletePath(currentPath, currentPath.PathNum - 1, "Single");

                    MenuManager.editWaypointMenu.Visible = false;
                    MenuManager.pathMenu.Visible = true;
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
