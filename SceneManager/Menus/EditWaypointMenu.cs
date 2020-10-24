using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
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
        private static UIMenuNumericScrollerItem<int> changeWaypointSpeed;
        internal static UIMenuCheckboxItem stopWaypointType;
        internal static UIMenuCheckboxItem directWaypointBehavior = new UIMenuCheckboxItem("Drive directly to waypoint?", false, "If checked, vehicles will ignore traffic rules and drive directly to this waypoint.");
        internal static UIMenuCheckboxItem collectorWaypoint;
        internal static UIMenuNumericScrollerItem<int> changeCollectorRadius = new UIMenuNumericScrollerItem<int>("Collection Radius", "The distance from this waypoint (in meters) vehicles will be collected", 1, 50, 1);
        internal static UIMenuNumericScrollerItem<int> changeSpeedZoneRadius = new UIMenuNumericScrollerItem<int>("Speed Zone Radius", "The distance from this collector waypoint (in meters) non-collected vehicles will drive at this waypoint's speed", 5, 200, 5);
        internal static UIMenuCheckboxItem updateWaypointPosition = new UIMenuCheckboxItem("Update Waypoint Position", false, "Updates the waypoint's position to the player's chosen position.  You should turn this on if you're planning on adding this waypoint as a new waypoint.");

        internal static void InstantiateMenu()
        {
            editWaypointMenu.ParentMenu = EditPathMenu.editPathMenu;
            MenuManager.menuPool.Add(editWaypointMenu);

            editWaypointMenu.OnScrollerChange += EditWaypoint_OnScrollerChanged;
            editWaypointMenu.OnCheckboxChange += EditWaypoint_OnCheckboxChanged;
            editWaypointMenu.OnItemSelect += EditWaypoint_OnItemSelected;
            editWaypointMenu.OnMenuOpen += EditWaypoint_OnMouseDown;
        }

        internal static void BuildEditWaypointMenu()
        {
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

                editWaypointMenu.AddItem(stopWaypointType = new UIMenuCheckboxItem("Is this a Stop waypoint?", currentWaypoint.IsStopWaypoint, "If checked, vehicles will drive to this waypoint, then stop."));
                editWaypointMenu.AddItem(directWaypointBehavior);
                if(currentWaypoint.DrivingFlagType == DrivingFlagType.Direct)
                {
                    directWaypointBehavior.Checked = true;
                }

                editWaypointMenu.AddItem(changeWaypointSpeed = new UIMenuNumericScrollerItem<int>("Waypoint Speed", $"How fast the AI will drive to the waypoint in ~b~{SettingsMenu.speedUnits.SelectedItem}", 5, 100, 5));
                changeWaypointSpeed.Value = (int)MathHelper.ConvertMetersPerSecondToMilesPerHour(currentWaypoint.Speed);

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
        }

        private static void UpdateWaypoint(Path currentPath, Waypoint currentWaypoint, DrivingFlagType drivingFlag)
        {
            if (currentPath.Waypoints.Count == 1)
            {
                currentWaypoint.UpdateWaypoint(currentWaypoint, GetMousePositionInWorld(), drivingFlag, stopWaypointType.Checked, SetDriveSpeedForWaypoint(), true, changeCollectorRadius.Value, changeSpeedZoneRadius.Value, updateWaypointPosition.Checked);
            }
            else
            {
                currentWaypoint.UpdateWaypoint(currentWaypoint, GetMousePositionInWorld(), drivingFlag, stopWaypointType.Checked, SetDriveSpeedForWaypoint(), collectorWaypoint.Checked, changeCollectorRadius.Value, changeSpeedZoneRadius.Value, updateWaypointPosition.Checked);
            }

            Game.LogTrivial($"Path {currentPath.Number} Waypoint {currentWaypoint.Number} updated [Driving style: {drivingFlag} | Stop waypoint: {stopWaypointType.Checked} | Speed: {changeWaypointSpeed.Value} | Collector: {currentWaypoint.IsCollector}]");

            updateWaypointPosition.Checked = false;
            Game.DisplayNotification($"~o~Scene Manager ~g~[Success]~w~\nWaypoint {currentWaypoint.Number} updated.");
        }

        private static void RemoveWaypoint(Path currentPath, Waypoint currentWaypoint, DrivingFlagType drivingFlag)
        {
            if (currentPath.Waypoints.Count == 1)
            {
                Game.LogTrivial($"Deleting the last waypoint from the path.");
                PathMainMenu.DeletePath(currentPath, PathMainMenu.Delete.Single);

                editWaypointMenu.Visible = false;
                PathMainMenu.pathMainMenu.Visible = true;
            }
            else
            {
                currentWaypoint.Remove();
                currentPath.Waypoints.Remove(currentWaypoint);
                Game.LogTrivial($"[Path {currentPath.Number}] Waypoint {currentWaypoint.Number} ({currentWaypoint.DrivingFlagType}) removed");

                foreach (Waypoint wp in currentPath.Waypoints)
                {
                    wp.Number = currentPath.Waypoints.IndexOf(wp) + 1;
                }

                editWaypointMenu.Clear();
                BuildEditWaypointMenu();

                if (currentPath.Waypoints.Count == 1)
                {
                    Hints.Display($"~o~Scene Manager ~y~[Hint]~w~\nYour path's first waypoint ~b~must~w~ be a collector.  If it's not, it will automatically be made into one.");
                    Game.LogTrivial($"The path only has 1 waypoint left, this waypoint must be a collector.");
                    currentPath.Waypoints[0].UpdateWaypoint(currentWaypoint, GetMousePositionInWorld(), drivingFlag, stopWaypointType.Checked, SetDriveSpeedForWaypoint(), true, changeCollectorRadius.Value, changeSpeedZoneRadius.Value, updateWaypointPosition.Checked);
                    collectorWaypoint.Checked = true;
                    changeCollectorRadius.Enabled = true;
                    changeSpeedZoneRadius.Enabled = true;
                }
            }
        }

        private static void AddAsNewWaypoint(Path currentPath, DrivingFlagType drivingFlag)
        {
            var pathIndex = PathMainMenu.paths.IndexOf(currentPath);
            var newWaypointBlip = CreateNewWaypointBlip();
            if (!currentPath.IsEnabled)
            {
                newWaypointBlip.Alpha = 0.5f;
            }

            if (collectorWaypoint.Checked)
            {
                currentPath.Waypoints.Add(new Waypoint(currentPath, currentPath.Waypoints.Last().Number + 1, GetMousePositionInWorld(), SetDriveSpeedForWaypoint(), drivingFlag, stopWaypointType.Checked, newWaypointBlip, true, changeCollectorRadius.Value, changeSpeedZoneRadius.Value));
            }
            else
            {
                currentPath.Waypoints.Add(new Waypoint(currentPath, currentPath.Waypoints.Last().Number + 1, GetMousePositionInWorld(), SetDriveSpeedForWaypoint(), drivingFlag, stopWaypointType.Checked, newWaypointBlip));
            }

            editWaypointMenu.RemoveItemAt(0);
            editWaypoint = new UIMenuNumericScrollerItem<int>("Edit Waypoint", "", currentPath.Waypoints.First().Number, currentPath.Waypoints.Last().Number, 1);
            editWaypointMenu.AddItem(editWaypoint, 0);
            editWaypoint.Index = editWaypoint.OptionCount - 1;
            editWaypointMenu.RefreshIndex();
            updateWaypointPosition.Checked = false;
            Game.LogTrivial($"New waypoint (#{currentPath.Waypoints.Last().Number}) added.");

            Blip CreateNewWaypointBlip()
            {
                var spriteNumericalEnum = pathIndex + 17; // 17 because the numerical value of these sprites are always 17 more than the path index
                var blip = new Blip(GetMousePositionInWorld())
                {
                    Scale = 0.5f,
                    Sprite = (BlipSprite)spriteNumericalEnum
                };

                if (collectorWaypoint.Checked)
                {
                    blip.Color = Color.Blue;
                }
                else if (stopWaypointType.Checked)
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

        private static void EditWaypoint_OnScrollerChanged(UIMenu sender, UIMenuScrollerItem scrollerItem, int first, int last)
        {
            var currentPath = PathMainMenu.paths[PathMainMenu.editPath.Index];
            var currentWaypoint = currentPath.Waypoints[editWaypoint.Value - 1];

            if (scrollerItem == editWaypoint)
            {
                changeWaypointSpeed.Value = (int)MathHelper.ConvertMetersPerSecondToMilesPerHour(currentWaypoint.Speed);
                stopWaypointType.Checked = currentWaypoint.IsStopWaypoint;
                directWaypointBehavior.Checked = currentWaypoint.DrivingFlagType == DrivingFlagType.Direct ? true : false;
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
                    while(changeCollectorRadius.Value > changeSpeedZoneRadius.Value)
                    {
                        changeSpeedZoneRadius.ScrollToNextOption();
                    }
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
            DrivingFlagType drivingFlag = directWaypointBehavior.Checked ? DrivingFlagType.Direct : DrivingFlagType.Normal;

            if (selectedItem == updateWaypoint)
            {
                UpdateWaypoint(currentPath, currentWaypoint, drivingFlag);
            }

            if (selectedItem == addAsNewWaypoint)
            {
                AddAsNewWaypoint(currentPath, drivingFlag);
            }

            if (selectedItem == removeWaypoint)
            {
                RemoveWaypoint(currentPath, currentWaypoint, drivingFlag);
            }
        }

        private static void EditWaypoint_OnMouseDown(UIMenu menu)
        {
            GameFiber.StartNew(() =>
            {
                while (menu.Visible)
                {
                    var selectedScroller = menu.MenuItems.Where(x => (x == editWaypoint || x == changeWaypointSpeed || x == changeCollectorRadius || x == changeSpeedZoneRadius) && x.Selected).FirstOrDefault();
                    if (selectedScroller != null)
                    {
                        HandleScrollerItemsWithMouseWheel(selectedScroller);
                    }

                    if (Game.IsKeyDown(Keys.LButton))
                    {
                        OnCheckboxItemClicked();
                        OnMenuItemClicked();
                    }
                    GameFiber.Yield();
                }
            });

            void OnCheckboxItemClicked()
            {
                if (collectorWaypoint.Selected && collectorWaypoint.Enabled)
                {
                    collectorWaypoint.Checked = !collectorWaypoint.Checked;
                    if (collectorWaypoint.Checked)
                    {
                        changeCollectorRadius.Enabled = true;
                        changeSpeedZoneRadius.Enabled = true;
                    }
                    else
                    {
                        changeCollectorRadius.Enabled = false;
                        changeSpeedZoneRadius.Enabled = false;
                    }
                }
                else if (stopWaypointType.Selected)
                {
                    stopWaypointType.Checked = !stopWaypointType.Checked;
                }
                else if (directWaypointBehavior.Selected)
                {
                    directWaypointBehavior.Checked = !directWaypointBehavior.Checked;
                }
                else if (updateWaypointPosition.Selected)
                {
                    updateWaypointPosition.Checked = !updateWaypointPosition.Checked;
                    if (updateWaypointPosition.Checked)
                    {
                        DrawWaypointMarker(GetMousePositionInWorld());
                    }

                }
            }

            void OnMenuItemClicked()
            {
                var currentPath = PathMainMenu.paths[PathMainMenu.editPath.Index];
                var currentWaypoint = currentPath.Waypoints[editWaypoint.Index];
                DrivingFlagType drivingFlag = directWaypointBehavior.Checked ? DrivingFlagType.Direct : DrivingFlagType.Normal;

                if (updateWaypoint.Selected)
                {
                    UpdateWaypoint(currentPath, currentWaypoint, drivingFlag);
                }
                else if (removeWaypoint.Selected)
                {
                    RemoveWaypoint(currentPath, currentWaypoint, drivingFlag);
                }
                else if (addAsNewWaypoint.Selected)
                {
                    AddAsNewWaypoint(currentPath, drivingFlag);
                }
            }

            void HandleScrollerItemsWithMouseWheel(UIMenuItem selectedScroller)
            {
                var menuScrollingDisabled = false;
                var menuItems = menu.MenuItems.Where(x => x != selectedScroller);
                while (Game.IsShiftKeyDownRightNow)
                {
                    menu.ResetKey(Common.MenuControls.Up);
                    menu.ResetKey(Common.MenuControls.Down);
                    menuScrollingDisabled = true;
                    ScrollMenuItem();
                    CompareScrollerValues();
                    GameFiber.Yield();
                }

                if (menuScrollingDisabled)
                {
                    menuScrollingDisabled = false;
                    menu.SetKey(Common.MenuControls.Up, GameControl.CursorScrollUp);
                    menu.SetKey(Common.MenuControls.Up, GameControl.CellphoneUp);
                    menu.SetKey(Common.MenuControls.Down, GameControl.CursorScrollDown);
                    menu.SetKey(Common.MenuControls.Down, GameControl.CellphoneDown);
                }

                void ScrollMenuItem()
                {
                    if (Game.GetMouseWheelDelta() > 0)
                    {
                        if (selectedScroller == changeCollectorRadius)
                        {
                            changeCollectorRadius.ScrollToNextOption();
                        }
                        else if (selectedScroller == changeSpeedZoneRadius)
                        {
                            changeSpeedZoneRadius.ScrollToNextOption();
                        }
                        else if (selectedScroller == changeWaypointSpeed)
                        {
                            changeWaypointSpeed.ScrollToNextOption();
                        }
                        else if(selectedScroller == editWaypoint)
                        {
                            editWaypoint.ScrollToNextOption();

                            var currentPath = PathMainMenu.paths[PathMainMenu.editPath.Index];
                            var currentWaypoint = currentPath.Waypoints[editWaypoint.Value - 1];
                            changeWaypointSpeed.Value = (int)MathHelper.ConvertMetersPerSecondToMilesPerHour(currentWaypoint.Speed);
                            stopWaypointType.Checked = currentWaypoint.IsStopWaypoint;
                            directWaypointBehavior.Checked = currentWaypoint.DrivingFlagType == DrivingFlagType.Direct ? true : false;
                            collectorWaypoint.Checked = currentWaypoint.IsCollector;
                            changeCollectorRadius.Enabled = collectorWaypoint.Checked ? true : false;
                            changeCollectorRadius.Value = (int)currentWaypoint.CollectorRadius;
                            changeSpeedZoneRadius.Enabled = collectorWaypoint.Checked ? true : false;
                            changeSpeedZoneRadius.Value = (int)currentWaypoint.SpeedZoneRadius;
                            updateWaypointPosition.Checked = false;
                        }
                    }
                    else if (Game.GetMouseWheelDelta() < 0)
                    {
                        if (selectedScroller == changeCollectorRadius)
                        {
                            changeCollectorRadius.ScrollToPreviousOption();
                        }
                        else if (selectedScroller == changeSpeedZoneRadius)
                        {
                            changeSpeedZoneRadius.ScrollToPreviousOption();
                        }
                        else if (selectedScroller == changeWaypointSpeed)
                        {
                            changeWaypointSpeed.ScrollToPreviousOption();
                        }
                        else if (selectedScroller == editWaypoint)
                        {
                            editWaypoint.ScrollToPreviousOption();

                            var currentPath = PathMainMenu.paths[PathMainMenu.editPath.Index];
                            var currentWaypoint = currentPath.Waypoints[editWaypoint.Value - 1];
                            changeWaypointSpeed.Value = (int)MathHelper.ConvertMetersPerSecondToMilesPerHour(currentWaypoint.Speed);
                            stopWaypointType.Checked = currentWaypoint.IsStopWaypoint;
                            directWaypointBehavior.Checked = currentWaypoint.DrivingFlagType == DrivingFlagType.Direct ? true : false;
                            collectorWaypoint.Checked = currentWaypoint.IsCollector;
                            changeCollectorRadius.Enabled = collectorWaypoint.Checked ? true : false;
                            changeCollectorRadius.Value = (int)currentWaypoint.CollectorRadius;
                            changeSpeedZoneRadius.Enabled = collectorWaypoint.Checked ? true : false;
                            changeSpeedZoneRadius.Value = (int)currentWaypoint.SpeedZoneRadius;
                            updateWaypointPosition.Checked = false;
                        }
                    }
                }

                void CompareScrollerValues()
                {
                    if (selectedScroller == changeCollectorRadius && changeCollectorRadius.Value > changeSpeedZoneRadius.Value)
                    {
                        while (changeCollectorRadius.Value > changeSpeedZoneRadius.Value)
                        {
                            changeSpeedZoneRadius.ScrollToNextOption();
                        }
                    }
                    if (selectedScroller == changeSpeedZoneRadius && changeSpeedZoneRadius.Value < changeCollectorRadius.Value)
                    {
                        changeCollectorRadius.Value = changeSpeedZoneRadius.Value;
                    }
                }
            }
        }

        private static void DrawWaypointMarker(Vector3 waypointPosition)
        {
            if (SettingsMenu.threeDWaypoints.Checked && collectorWaypoint.Checked)
            {
                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, waypointPosition, 0, 0, 0, 0, 0, 0, (float)PathCreationMenu.collectorRadius.Value * 2, (float)PathCreationMenu.collectorRadius.Value * 2, 1f, 80, 130, 255, 80, false, false, 2, false, 0, 0, false);
                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, waypointPosition, 0, 0, 0, 0, 0, 0, (float)PathCreationMenu.speedZoneRadius.Value * 2, (float)PathCreationMenu.speedZoneRadius.Value * 2, 1f, 255, 185, 80, 80, false, false, 2, false, 0, 0, false);
            }
            else if (stopWaypointType.Checked)
            {
                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, waypointPosition, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 255, 65, 65, 80, false, false, 2, false, 0, 0, false);
            }
            else
            {
                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, waypointPosition, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 65, 255, 65, 80, false, false, 2, false, 0, 0, false);
            }
        }

        private static Vector3 GetMousePositionInWorld()
        {
            HitResult TracePlayerView(float maxTraceDistance = 100f, TraceFlags flags = TraceFlags.IntersectWorld) => TracePlayerView2(out Vector3 v1, out Vector3 v2, maxTraceDistance, flags);

            HitResult TracePlayerView2(out Vector3 start, out Vector3 end, float maxTraceDistance, TraceFlags flags)
            {
                Vector3 direction = GetPlayerLookingDirection(out start);
                end = start + (maxTraceDistance * direction);
                return World.TraceLine(start, end, flags);
            }

            Vector3 GetPlayerLookingDirection(out Vector3 camPosition)
            {
                if (Camera.RenderingCamera)
                {
                    camPosition = Camera.RenderingCamera.Position;
                    return Camera.RenderingCamera.Direction;
                }
                else
                {
                    float pitch = Rage.Native.NativeFunction.Natives.GET_GAMEPLAY_CAM_RELATIVE_PITCH<float>();
                    float heading = Rage.Native.NativeFunction.Natives.GET_GAMEPLAY_CAM_RELATIVE_HEADING<float>();

                    camPosition = Rage.Native.NativeFunction.Natives.GET_GAMEPLAY_CAM_COORD<Vector3>();
                    return (Game.LocalPlayer.Character.Rotation + new Rotator(pitch, 0, heading)).ToVector().ToNormalized();
                }
            }

            return TracePlayerView(100f, TraceFlags.IntersectWorld).HitPosition;
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
