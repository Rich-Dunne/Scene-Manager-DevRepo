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
            //var currentWaypoint = currentPath.WaypointData[editWaypoint.Index]; // Can't use this before the menu is created, will this be a problem elsewhere?

            // Populating menu list so user can select which waypoint to edit by index
            pathWaypoints.Clear();
            for (int i = 0; i < currentPath.Waypoint.Count; i++)
            {
                pathWaypoints.Add(i + 1);
            }

            MenuManager.editWaypointMenu.Clear();
            MenuManager.editWaypointMenu.AddItem(editWaypoint = new UIMenuListItem("Edit Waypoint", pathWaypoints, 0));
            MenuManager.editWaypointMenu.AddItem(changeWaypointType = new UIMenuListItem("Change Waypoint Type", waypointTypes, Array.IndexOf(drivingFlags, currentPath.Waypoint[editWaypoint.Index].DrivingFlag)));
            MenuManager.editWaypointMenu.AddItem(changeWaypointSpeed = new UIMenuListItem("Change Waypoint Speed", waypointSpeeds, waypointSpeeds.IndexOf(currentPath.Waypoint[editWaypoint.Index].Speed)));
            MenuManager.editWaypointMenu.AddItem(collectorWaypoint = new UIMenuCheckboxItem("Collector Waypoint", TrafficMenu.paths[TrafficMenu.editPath.Index].Waypoint[editWaypoint.Index].Collector));
            MenuManager.editWaypointMenu.AddItem(changeCollectorRadius = new UIMenuListItem("Change Collection Radius", collectorRadii, collectorRadii.IndexOf(currentPath.Waypoint[editWaypoint.Index].CollectorRadius)));
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
            var currentWaypoint = currentPath.Waypoint[editWaypoint.Index];

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
                MenuManager.editWaypointMenu.AddItem(changeCollectorRadius = new UIMenuListItem("Change Collection Radius", collectorRadii, collectorRadii.IndexOf(currentPath.Waypoint[editWaypoint.Index].CollectorRadius)));
                MenuManager.editWaypointMenu.AddItem(updateWaypointPosition = new UIMenuCheckboxItem("Update Waypoint Position", false));
                MenuManager.editWaypointMenu.AddItem(editUpdateWaypoint = new UIMenuItem("Update Waypoint"));
                MenuManager.editWaypointMenu.AddItem(editRemoveWaypoint = new UIMenuItem("Remove Waypoint"));
                MenuManager.editWaypointMenu.RefreshIndex();
            }
        }

        // Crashed here updating waypoint position for waypoint 2/2
        private static void EditWaypoint_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            var currentPath = TrafficMenu.paths[TrafficMenu.editPath.Index];
            var currentWaypoint = currentPath.Waypoint[editWaypoint.Index];

            if (selectedItem == editUpdateWaypoint)
            {
                currentWaypoint.DrivingFlag = drivingFlags[changeWaypointType.Index];
                currentWaypoint.Speed = waypointSpeeds[changeWaypointSpeed.Index];
                if (updateWaypointPosition.Checked)
                {
                    currentWaypoint.WaypointPos = Game.LocalPlayer.Character.Position;
                    currentWaypoint.WaypointBlip.Position = Game.LocalPlayer.Character.Position;
                    if (currentWaypoint.CollectorRadiusBlip)
                    {
                        currentWaypoint.CollectorRadiusBlip.Position = Game.LocalPlayer.Character.Position;
                    }
                }

                if (collectorWaypoint.Checked)
                {
                    currentWaypoint.Collector = true;
                    var yieldZone = World.AddSpeedZone(Game.LocalPlayer.Character.Position, 50f, currentWaypoint.Speed);
                    currentWaypoint.YieldZone = yieldZone;
                    if (currentWaypoint.CollectorRadiusBlip)
                    {
                        //currentWaypoint.CollectorRadiusBlip.Color = currentWaypoint.WaypointBlip.Color;
                        currentWaypoint.CollectorRadiusBlip.Alpha = 0.5f;
                        currentWaypoint.CollectorRadiusBlip.Scale = collectorRadii[changeCollectorRadius.Index];
                    }
                    else
                    {
                        currentWaypoint.CollectorRadiusBlip = new Blip(currentWaypoint.WaypointBlip.Position, collectorRadii[changeCollectorRadius.Index])
                        {
                            Color = currentWaypoint.WaypointBlip.Color,
                            Alpha = 0.5f
                        };
                    }
                    currentWaypoint.CollectorRadius = collectorRadii[changeCollectorRadius.Index];
                }
                else
                {
                    currentWaypoint.Collector = false;
                    World.RemoveSpeedZone(currentWaypoint.YieldZone);
                    currentWaypoint.YieldZone = 0;
                    if (currentWaypoint.CollectorRadiusBlip)
                    {
                        //currentWaypoint.CollectorRadiusBlip.Color = currentWaypoint.WaypointBlip.Color;
                        currentWaypoint.CollectorRadiusBlip.Alpha = 0.25f;
                    }
                }
                Game.LogTrivial($"Updated path {currentPath.PathNum} waypoint {currentWaypoint.WaypointNum}: Driving flag is {drivingFlags[changeWaypointType.Index].ToString()}, speed is {waypointSpeeds[changeWaypointSpeed.Index].ToString()}, collector is {currentWaypoint.Collector}");

                if (currentPath.Waypoint.Count < 2 && currentPath.Waypoint[0].DrivingFlag == VehicleDrivingFlags.StopAtDestination)
                {
                    Game.LogTrivial($"The remaining waypoint was updated to be a stop waypoint.  Enabling/disabling the path is no longer locked.");
                    EditPathMenu.togglePath.Enabled = true;
                }

                Game.DisplayNotification($"~o~Scene Manager\n~g~[Success]~w~ Waypoint {currentWaypoint.WaypointNum} updated.");
            }

            if (selectedItem == editRemoveWaypoint)
            {
                Game.LogTrivial($"[Path {currentPath.PathNum}] Waypoint {currentWaypoint.WaypointNum} ({currentWaypoint.DrivingFlag}) removed");
                if (currentPath.Waypoint.Count == 1)
                {
                    Game.LogTrivial($"Deleting the last waypoint from the path.");
                    TrafficMenu.DeletePath(currentPath, currentPath.PathNum - 1, "Single");
                    //pathWaypoints.Clear();
                    //editPathMenu.Clear();
                    MenuManager.editWaypointMenu.Visible = false;
                    MenuManager.pathMenu.Visible = true;
                }
                else
                {
                    currentWaypoint.WaypointBlip.Delete(); // Delete the waypoint's blip
                    if (currentWaypoint.CollectorRadiusBlip)
                    {
                        currentWaypoint.CollectorRadiusBlip.Delete();
                    }
                    currentPath.Waypoint.Remove(currentWaypoint); // Delete the waypoint's data object
                    pathWaypoints.RemoveAt(editWaypoint.Index); // Remove the waypoint from the menu list

                    // Will this have adverse affects on vehicles currently following the path?
                    // Update waypoint number for each waypoint in the path's waypoint data
                    foreach (Waypoint wp in currentPath.Waypoint)
                    {
                        wp.WaypointNum = currentPath.Waypoint.IndexOf(wp) + 1;
                        Game.LogTrivial($"Waypoint at index {currentPath.Waypoint.IndexOf(wp)} is now waypoint #{wp.WaypointNum}");
                    }

                    BuildEditWaypointMenu();

                    if (currentPath.Waypoint.Count == 1 && currentPath.Waypoint[0].DrivingFlag != VehicleDrivingFlags.StopAtDestination)
                    {
                        Game.LogTrivial($"The path only has 1 waypoint left, and the waypoint is not a stop waypoint.  Disabling the path.");
                        currentPath.PathDisabled = true;
                        EditPathMenu.togglePath.Checked = true;
                        EditPathMenu.togglePath.Enabled = false;
                    }
                }
            }
        }
    }
}
