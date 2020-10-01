using System;
using System.Drawing;
using System.Linq;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    class EditWaypointMenu
    {
        private static VehicleDrivingFlags[] drivingFlags = new VehicleDrivingFlags[] { VehicleDrivingFlags.Normal, VehicleDrivingFlags.IgnorePathFinding, VehicleDrivingFlags.StopAtDestination };
        private static string[] waypointTypes = new string[] { "Drive To (Normal)", "Drive To (Direct)", "Stop" };
        internal static UIMenu editWaypointMenu = new UIMenu("Scene Manager", "~o~Edit Waypoint");
        internal static UIMenuItem updateWaypoint = new UIMenuItem("Update Waypoint");
        internal static UIMenuItem removeWaypoint = new UIMenuItem("Remove Waypoint");
        internal static UIMenuItem addAsNewWaypoint = new UIMenuItem("Add as New Waypoint", "Adds a new waypoint to the end of the path with these settings");
        internal static UIMenuNumericScrollerItem<int> editWaypoint;
        internal static UIMenuListScrollerItem<string> changeWaypointType = new UIMenuListScrollerItem<string>("Waypoint Type", "", waypointTypes);
        private static UIMenuNumericScrollerItem<int> changeWaypointSpeed;
        internal static UIMenuCheckboxItem collectorWaypoint;
        internal static UIMenuNumericScrollerItem<int> changeCollectorRadius = new UIMenuNumericScrollerItem<int>("Collection Radius", "The distance from this waypoint (in meters) vehicles will be collected", 1, 50, 1);
        internal static UIMenuNumericScrollerItem<int> changeSpeedZoneRadius = new UIMenuNumericScrollerItem<int>("Speed Zone Radius", "The distance from this collector waypoint (in meters) non-collected vehicles will drive at this waypoint's speed", 5, 200, 5);
        internal static UIMenuCheckboxItem updateWaypointPosition = new UIMenuCheckboxItem("Update Waypoint Position", false, "Updates the waypoint's position to the player's current position.  You should turn this on if you're planning on adding this waypoint as a new waypoint.");

        internal static void InstantiateMenu()
        {
            editWaypointMenu.ParentMenu = EditPathMenu.editPathMenu;
            MenuManager.menuPool.Add(editWaypointMenu);
        }

        internal static void BuildEditWaypointMenu()
        {
            // Need to unsubscribe from these or else there will be duplicate firings if the user left the menu, then re-entered
            ResetEventHandlerSubscriptions();

            var currentPath = PathMainMenu.paths[PathMainMenu.editPath.Value-1];
            //Logger.Log($"Current path: {currentPath.Number}");

            editWaypoint = new UIMenuNumericScrollerItem<int>("Edit Waypoint", "", currentPath.Waypoints.First().Number, currentPath.Waypoints.Last().Number, 1);
            editWaypointMenu.Clear();
            editWaypointMenu.AddItem(editWaypoint);
            editWaypoint.Index = 0;

            var currentWaypoint = currentPath.Waypoints.Where(wp => wp.Number == editWaypoint.Value).FirstOrDefault();
            //Logger.Log($"Current waypoint: {currentWaypoint.Number}, Driving flag: {currentWaypoint.DrivingFlag.ToString()}");
            if(currentWaypoint != null)
            {
                editWaypointMenu.AddItem(changeWaypointType);
                changeWaypointType.Index = Array.IndexOf(drivingFlags, currentWaypoint.DrivingFlag);

                editWaypointMenu.AddItem(changeWaypointSpeed = new UIMenuNumericScrollerItem<int>("Waypoint Speed", $"How fast the AI will drive to the waypoint in ~b~{SettingsMenu.speedUnits.SelectedItem}", 5, 100, 5));
                changeWaypointSpeed.Value = (int)MathHelper.ConvertMetersPerSecondToMilesPerHour(currentWaypoint.Speed);

                editWaypointMenu.AddItem(collectorWaypoint = new UIMenuCheckboxItem("Collector", currentWaypoint.IsCollector, "If this waypoint will collect vehicles to follow the path"));

                editWaypointMenu.AddItem(changeCollectorRadius);
                changeCollectorRadius.Value = currentWaypoint.CollectorRadius != 0
                    ? (int)currentWaypoint.CollectorRadius
                    : changeCollectorRadius.Minimum;

                editWaypointMenu.AddItem(changeSpeedZoneRadius);
                changeSpeedZoneRadius.Value = currentWaypoint.CollectorRadius != 0
                    ? (int)currentWaypoint.SpeedZoneRadius
                    : changeSpeedZoneRadius.Minimum;

                changeCollectorRadius.Enabled = collectorWaypoint.Checked ? true : false;
                changeSpeedZoneRadius.Enabled = collectorWaypoint.Checked ? true : false;

                editWaypointMenu.AddItem(updateWaypointPosition);
                editWaypointMenu.AddItem(updateWaypoint);
                updateWaypoint.ForeColor = Color.Gold;
                editWaypointMenu.AddItem(removeWaypoint);
                removeWaypoint.ForeColor = Color.Gold;
                editWaypointMenu.AddItem(addAsNewWaypoint);
                addAsNewWaypoint.ForeColor = Color.Gold;

                EditPathMenu.editPathMenu.Visible = false;
                editWaypointMenu.RefreshIndex();
                editWaypointMenu.Visible = true;
            }
            

            void ResetEventHandlerSubscriptions()
            {
                editWaypointMenu.OnItemSelect -= EditWaypoint_OnItemSelected;
                editWaypointMenu.OnCheckboxChange -= EditWaypoint_OnCheckboxChanged;
                editWaypointMenu.OnScrollerChange -= EditWaypoint_OnScrollerChanged;

                editWaypointMenu.OnScrollerChange += EditWaypoint_OnScrollerChanged;
                editWaypointMenu.OnCheckboxChange += EditWaypoint_OnCheckboxChanged;
                editWaypointMenu.OnItemSelect += EditWaypoint_OnItemSelected;
            }
        }

        private static void EditWaypoint_OnScrollerChanged(UIMenu sender, UIMenuScrollerItem scrollerItem, int first, int last)
        {
            var currentPath = PathMainMenu.paths[PathMainMenu.editPath.Index];
            var currentWaypoint = currentPath.Waypoints[editWaypoint.Value - 1];

            if (scrollerItem == editWaypoint)
            {
                changeWaypointType.Index = Array.IndexOf(drivingFlags, currentWaypoint.DrivingFlag);
                changeWaypointSpeed.Value = (int)MathHelper.ConvertMetersPerSecondToMilesPerHour(currentWaypoint.Speed);
                collectorWaypoint.Checked = currentWaypoint.IsCollector;
                changeCollectorRadius.Enabled = collectorWaypoint.Checked ? true : false;
                changeCollectorRadius.Value = (int)currentWaypoint.CollectorRadius;
                changeSpeedZoneRadius.Enabled = collectorWaypoint.Checked ? true : false;
                changeSpeedZoneRadius.Value = (int)currentWaypoint.SpeedZoneRadius;
                updateWaypointPosition.Checked = false;
            }

            if (scrollerItem == changeCollectorRadius)
            {
                if (changeCollectorRadius.Value > changeSpeedZoneRadius.Value)
                {
                    changeSpeedZoneRadius.ScrollToNextOption();
                }
            }

            if (scrollerItem == changeSpeedZoneRadius)
            {
                if (changeSpeedZoneRadius.Value < changeCollectorRadius.Value)
                {
                    changeCollectorRadius.Value = changeSpeedZoneRadius.Value;
                }
            }
        }

        private static void EditWaypoint_OnCheckboxChanged(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            if (checkboxItem == collectorWaypoint)
            {
                changeCollectorRadius.Enabled = collectorWaypoint.Checked ? true : false;
                changeSpeedZoneRadius.Enabled = collectorWaypoint.Checked ? true : false;
            }
        }

        private static void EditWaypoint_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            var currentPath = PathMainMenu.paths[PathMainMenu.editPath.Index];
            var currentWaypoint = currentPath.Waypoints[editWaypoint.Index];

            if (selectedItem == updateWaypoint)
            {
                if(currentPath.Waypoints.Count == 1)
                {
                    currentWaypoint.UpdateWaypoint(currentWaypoint, drivingFlags[changeWaypointType.Index], SetDriveSpeedForWaypoint(), true, changeCollectorRadius.Value, changeSpeedZoneRadius.Value, updateWaypointPosition.Checked);
                }
                else
                {
                    currentWaypoint.UpdateWaypoint(currentWaypoint, drivingFlags[changeWaypointType.Index], SetDriveSpeedForWaypoint(), collectorWaypoint.Checked, changeCollectorRadius.Value, changeSpeedZoneRadius.Value, updateWaypointPosition.Checked);
                }
                
                Logger.Log($"Updated path {currentPath.Number} waypoint {currentWaypoint.Number}: Driving flag is {drivingFlags[changeWaypointType.Index].ToString()}, speed is {changeWaypointSpeed.Value}, collector is {currentWaypoint.IsCollector}");

                updateWaypointPosition.Checked = false;
                Game.DisplayNotification($"~o~Scene Manager\n~g~[Success]~w~ Waypoint {currentWaypoint.Number} updated.");

                BuildEditWaypointMenu();
            }

            if (selectedItem == addAsNewWaypoint)
            {
                var pathIndex = PathMainMenu.paths.IndexOf(currentPath);
                var drivingFlag = drivingFlags[changeWaypointType.Index];
                var newWaypointBlip = CreateNewWaypointBlip();
                if (!currentPath.IsEnabled)
                {
                    newWaypointBlip.Alpha = 0.5f;
                }

                if (collectorWaypoint.Checked)
                {
                    currentPath.Waypoints.Add(new Waypoint(currentPath, currentPath.Waypoints.Last().Number + 1, Game.LocalPlayer.Character.Position, SetDriveSpeedForWaypoint(), drivingFlag, newWaypointBlip, true, changeCollectorRadius.Value, changeSpeedZoneRadius.Value));
                }
                else
                {
                    currentPath.Waypoints.Add(new Waypoint(currentPath, currentPath.Waypoints.Last().Number + 1, Game.LocalPlayer.Character.Position, SetDriveSpeedForWaypoint(), drivingFlag, newWaypointBlip));
                }

                editWaypointMenu.RemoveItemAt(0);
                editWaypoint = new UIMenuNumericScrollerItem<int>("Edit Waypoint", "", currentPath.Waypoints.First().Number, currentPath.Waypoints.Last().Number, 1);
                editWaypointMenu.AddItem(editWaypoint, 0);
                editWaypoint.Index = editWaypoint.OptionCount - 1;
                editWaypointMenu.RefreshIndex();
                updateWaypointPosition.Checked = false;
                Logger.Log($"New waypoint (#{currentWaypoint.Number + 1}) added.");

                Blip CreateNewWaypointBlip()
                {
                    var spriteNumericalEnum = pathIndex + 17; // 17 because the numerical value of these sprites are always 17 more than the path index
                    var blip = new Blip(Game.LocalPlayer.Character.Position)
                    {
                        Scale = 0.5f,
                        Sprite = (BlipSprite)spriteNumericalEnum
                    };

                    if (collectorWaypoint.Checked)
                    {
                        blip.Color = Color.Blue;
                    }
                    else if (drivingFlag == VehicleDrivingFlags.StopAtDestination)
                    {
                        blip.Color = Color.Red;
                    }
                    else
                    {
                        blip.Color = Color.Green;
                    }

                    if (!SettingsMenu.mapBlips.Checked)
                    {
                        blip.Alpha = 0f;
                    }

                    return blip;
                }
            }

            if (selectedItem == removeWaypoint)
            {
                if (currentPath.Waypoints.Count == 1)
                {
                    Logger.Log($"Deleting the last waypoint from the path.");
                    PathMainMenu.DeletePath(currentPath, PathMainMenu.Delete.Single);

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
                    Logger.Log($"[Path {currentPath.Number}] Waypoint {currentWaypoint.Number} ({currentWaypoint.DrivingFlag}) removed");

                    foreach (Waypoint wp in currentPath.Waypoints)
                    {
                        wp.Number = currentPath.Waypoints.IndexOf(wp) + 1;
                        Logger.Log($"Waypoint at index {currentPath.Waypoints.IndexOf(wp)} is now waypoint #{wp.Number}");
                    }

                    editWaypointMenu.Clear();
                    BuildEditWaypointMenu();

                    if (currentPath.Waypoints.Count == 1)
                    {
                        Hints.Display($"~o~Scene Manager\n~y~[Hint]~w~ Your path's first waypoint ~b~must~w~ be a collector.  If it's not, it will automatically be made into one.");
                        Logger.Log($"The path only has 1 waypoint left, this waypoint must be a collector.");
                        currentPath.Waypoints[0].UpdateWaypoint(currentWaypoint, drivingFlags[changeWaypointType.Index], SetDriveSpeedForWaypoint(), true, changeCollectorRadius.Value, changeSpeedZoneRadius.Value, updateWaypointPosition.Checked);
                        collectorWaypoint.Checked = true;
                        changeCollectorRadius.Enabled = true;
                        changeSpeedZoneRadius.Enabled = true;
                    }
                }
            }
        }

        private static float SetDriveSpeedForWaypoint()
        {
            float convertedSpeed;
            if (SettingsMenu.speedUnits.SelectedItem == SpeedUnits.MPH)
            {
                //Logger.Log($"Original speed: {waypointSpeeds[waypointSpeed.Index]}{SettingsMenu.speedUnits.SelectedItem}");
                convertedSpeed = MathHelper.ConvertMilesPerHourToMetersPerSecond(changeWaypointSpeed.Value);
                //Logger.Log($"Converted speed: {convertedSpeed}m/s");
            }
            else
            {
                //Logger.Log($"Original speed: {waypointSpeeds[waypointSpeed.Index]}{SettingsMenu.speedUnits.SelectedItem}");
                convertedSpeed = MathHelper.ConvertKilometersPerHourToMetersPerSecond(changeWaypointSpeed.Value);
                //Logger.Log($"Converted speed: {convertedSpeed}m/s");
            }

            return convertedSpeed;
        }
    }
}
