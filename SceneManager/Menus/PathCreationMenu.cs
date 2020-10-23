using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    class PathCreationMenu
    {
        internal static UIMenu pathCreationMenu = new UIMenu("Scene Manager", "~o~Path Creation Menu");
        private static UIMenuItem trafficAddWaypoint = new UIMenuItem("Add waypoint"), trafficRemoveWaypoint = new UIMenuItem("Remove last waypoint"), trafficEndPath = new UIMenuItem("End path creation");
        internal static UIMenuNumericScrollerItem<int> waypointSpeed;
        internal static UIMenuCheckboxItem stopWaypointType = new UIMenuCheckboxItem("Is this a Stop waypoint?", false, "If checked, vehicles will drive to this waypoint, then stop.");
        internal static UIMenuCheckboxItem directWaypointBehavior = new UIMenuCheckboxItem("Drive directly to waypoint?", false, "If checked, vehicles will ignore traffic rules and drive directly to this waypoint.");
        internal static UIMenuCheckboxItem collectorWaypoint = new UIMenuCheckboxItem("Collector", true, "If checked, this waypoint will collect vehicles to follow the path.  Your path's first waypoint ~b~must~w~ be a collector.");
        internal static UIMenuNumericScrollerItem<int> collectorRadius = new UIMenuNumericScrollerItem<int>("Collection Radius", "The distance from this waypoint (in meters) vehicles will be collected", 1, 50, 1);
        internal static UIMenuNumericScrollerItem<int> speedZoneRadius = new UIMenuNumericScrollerItem<int>("Speed Zone Radius", "The distance from this collector waypoint (in meters) non-collected vehicles will drive at this waypoint's speed", 5, 200, 5);
        private static List<UIMenuItem> menuItems = new List<UIMenuItem> {collectorWaypoint, collectorRadius, speedZoneRadius, stopWaypointType, directWaypointBehavior, waypointSpeed, trafficAddWaypoint, trafficRemoveWaypoint, trafficEndPath };

        internal static void InstantiateMenu()
        {
            pathCreationMenu.ParentMenu = PathMainMenu.pathMainMenu;
            MenuManager.menuPool.Add(pathCreationMenu);
            pathCreationMenu.OnItemSelect += PathCreation_OnItemSelected;
            pathCreationMenu.OnCheckboxChange += PathCreation_OnCheckboxChanged;
            pathCreationMenu.OnScrollerChange += PathCreation_OnScrollerChanged;
            pathCreationMenu.OnMenuOpen += PathCreation_OnMouseDown;
        }

        internal static void BuildPathCreationMenu()
        {
            pathCreationMenu.AddItem(collectorWaypoint);
            collectorWaypoint.Enabled = false;
            collectorWaypoint.Checked = true;

            pathCreationMenu.AddItem(collectorRadius);
            collectorRadius.Index = Settings.CollectorRadius - 1;
            collectorRadius.Enabled = true;

            pathCreationMenu.AddItem(speedZoneRadius);
            speedZoneRadius.Index = (Settings.SpeedZoneRadius / 5) - 1;
            speedZoneRadius.Enabled = true;

            pathCreationMenu.AddItem(stopWaypointType);
            stopWaypointType.Checked = Settings.StopWaypoint;
            pathCreationMenu.AddItem(directWaypointBehavior);
            directWaypointBehavior.Checked = Settings.DirectDrivingBehavior;

            pathCreationMenu.AddItem(waypointSpeed = new UIMenuNumericScrollerItem<int>("Waypoint Speed", $"How fast the AI will drive to this waypoint in ~b~{SettingsMenu.speedUnits.SelectedItem}", 5, 100, 5));
            waypointSpeed.Index = (Settings.WaypointSpeed / 5) - 1;

            pathCreationMenu.AddItem(trafficAddWaypoint);
            trafficAddWaypoint.ForeColor = Color.Gold;

            pathCreationMenu.AddItem(trafficRemoveWaypoint);
            trafficRemoveWaypoint.ForeColor = Color.Gold;
            trafficRemoveWaypoint.Enabled = false;

            pathCreationMenu.AddItem(trafficEndPath);
            trafficEndPath.ForeColor = Color.Gold;
            trafficEndPath.Enabled = false;

            pathCreationMenu.RefreshIndex();
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
            if (selectedItem == trafficAddWaypoint)
            {
                AddNewWaypoint(GetMousePositionInWorld());
            }

            if (selectedItem == trafficRemoveWaypoint)
            {
                RemoveWaypoint();
            }

            if (selectedItem == trafficEndPath)
            {
                EndPath();
            }
        }

        private static void PathCreation_OnScrollerChanged(UIMenu sender, UIMenuScrollerItem scrollerItem, int first, int last)
        {
            if (scrollerItem == collectorRadius)
            {
                if (collectorRadius.Value > speedZoneRadius.Value)
                {
                    while (collectorRadius.Value > speedZoneRadius.Value)
                    {
                        speedZoneRadius.ScrollToNextOption();
                    }
                }
            }

            if (scrollerItem == speedZoneRadius)
            {
                if (speedZoneRadius.Value < collectorRadius.Value)
                {
                    collectorRadius.Value = speedZoneRadius.Value;
                }
            }
        }

        private static void PathCreation_OnMouseDown(UIMenu menu)
        {
            GameFiber.StartNew(() =>
            {
                while (menu.Visible)
                {
                    var selectedScroller = menu.MenuItems.Where(x => (x == collectorRadius || x == speedZoneRadius || x == waypointSpeed) && x.Selected).FirstOrDefault();
                    if (selectedScroller != null)
                    {
                        HandleScrollerItemsWithMouseWheel(selectedScroller);
                    }

                    // Draw marker at mouse position
                    DrawWaypointMarker(GetMousePositionInWorld());

                    // Add waypoint if menu item is selected and user left clicks
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
                }
                else if (stopWaypointType.Selected)
                {
                    stopWaypointType.Checked = !stopWaypointType.Checked;
                }
                else if (directWaypointBehavior.Selected)
                {
                    directWaypointBehavior.Checked = !directWaypointBehavior.Checked;
                }
            }

            void OnMenuItemClicked()
            {
                if (trafficAddWaypoint.Selected)
                {
                    AddNewWaypoint(GetMousePositionInWorld());
                }
                else if (trafficRemoveWaypoint.Selected)
                {
                    RemoveWaypoint();
                }
                else if (trafficEndPath.Selected)
                {
                    EndPath();
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
                    DrawWaypointMarker(GetMousePositionInWorld());
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
                        if (selectedScroller == collectorRadius)
                        {
                            collectorRadius.ScrollToNextOption();
                        }
                        else if (selectedScroller == speedZoneRadius)
                        {
                            speedZoneRadius.ScrollToNextOption();
                        }
                        else if (selectedScroller == waypointSpeed)
                        {
                            waypointSpeed.ScrollToNextOption();
                        }
                    }
                    else if (Game.GetMouseWheelDelta() < 0)
                    {
                        if (selectedScroller == collectorRadius)
                        {
                            collectorRadius.ScrollToPreviousOption();
                        }
                        else if (selectedScroller == speedZoneRadius)
                        {
                            speedZoneRadius.ScrollToPreviousOption();
                        }
                        else if (selectedScroller == waypointSpeed)
                        {
                            waypointSpeed.ScrollToPreviousOption();
                        }
                    }
                }

                void CompareScrollerValues()
                {
                    if (selectedScroller == collectorRadius && collectorRadius.Value > speedZoneRadius.Value)
                    {
                        while (collectorRadius.Value > speedZoneRadius.Value)
                        {
                            speedZoneRadius.ScrollToNextOption();
                        }
                    }
                    if (selectedScroller == speedZoneRadius && speedZoneRadius.Value < collectorRadius.Value)
                    {
                        collectorRadius.Value = speedZoneRadius.Value;
                    }
                }
            }
        }

        private static void AddNewWaypoint(Vector3 waypointPosition)
        {
            var anyPathsExist = PathMainMenu.paths.Count > 0;

            if (!anyPathsExist)
            {
                AddNewPathToPathsCollection(PathMainMenu.paths, 0);
            }
            else if (anyPathsExist && !PathMainMenu.paths.Any(p => p != null && p.State == State.Creating))
            {
                AddNewPathToPathsCollection(PathMainMenu.paths, PathMainMenu.paths.IndexOf(PathMainMenu.paths.Where(p => p.State == State.Finished).Last()) + 1);
            }

            var firstNonNullPath = PathMainMenu.paths.Where(p => p != null && p.State == State.Creating).First();
            var pathIndex = PathMainMenu.paths.IndexOf(firstNonNullPath);
            var pathNumber = firstNonNullPath.Number;
            var waypointNumber = PathMainMenu.paths[pathIndex].Waypoints.Count + 1;
            DrivingFlagType drivingFlag = directWaypointBehavior.Checked ? DrivingFlagType.Direct : DrivingFlagType.Normal;

            if (collectorWaypoint.Checked)
            {
                PathMainMenu.paths[pathIndex].Waypoints.Add(new Waypoint(firstNonNullPath, waypointNumber, waypointPosition, SetDriveSpeedForWaypoint(), drivingFlag, stopWaypointType.Checked, CreateWaypointBlip(), true, collectorRadius.Value, speedZoneRadius.Value));
            }
            else
            {
                PathMainMenu.paths[pathIndex].Waypoints.Add(new Waypoint(firstNonNullPath, waypointNumber, waypointPosition, SetDriveSpeedForWaypoint(), drivingFlag, stopWaypointType.Checked, CreateWaypointBlip()));
            }
            Game.LogTrivial($"Path {pathNumber} Waypoint {waypointNumber} added [Driving style: {drivingFlag} | Stop waypoint: {stopWaypointType.Checked} | Speed: {waypointSpeed.Value} | Collector: {collectorWaypoint.Checked}]");

            ToggleTrafficEndPathMenuItem(pathIndex);
            collectorWaypoint.Enabled = true;
            collectorWaypoint.Checked = false;
            trafficRemoveWaypoint.Enabled = true;
            PathMainMenu.createNewPath.Text = $"Continue Creating Path {pathNumber}";

            float SetDriveSpeedForWaypoint()
            {
                float convertedSpeed;
                if (SettingsMenu.speedUnits.SelectedItem == SpeedUnits.MPH)
                {
                    //Logger.Log($"Original speed: {waypointSpeeds[waypointSpeed.Index]}{SettingsMenu.speedUnits.SelectedItem}");
                    convertedSpeed = MathHelper.ConvertMilesPerHourToMetersPerSecond(waypointSpeed.Value);
                    //Logger.Log($"Converted speed: {convertedSpeed}m/s");
                }
                else
                {
                    //Logger.Log($"Original speed: {waypointSpeeds[waypointSpeed.Index]}{SettingsMenu.speedUnits.SelectedItem}");
                    convertedSpeed = MathHelper.ConvertKilometersPerHourToMetersPerSecond(waypointSpeed.Value);
                    //Logger.Log($"Converted speed: {convertedSpeed}m/s");
                }

                return convertedSpeed;
            }

            Blip CreateWaypointBlip()
            {
                var spriteNumericalEnum = pathIndex + 17; // 17 because the numerical value of these sprites are always 17 more than the path index
                var blip = new Blip(waypointPosition)
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

        private static void RemoveWaypoint()
        {
            for (int i = 0; i < PathMainMenu.paths.Count; i++)
            {
                if (PathMainMenu.paths.ElementAtOrDefault(i) != null && PathMainMenu.paths[i].State == State.Creating)
                {
                    Game.LogTrivial($"[Path {i + 1}] {PathMainMenu.paths[i].Waypoints.Last().DrivingFlagType} waypoint removed");
                    PathMainMenu.paths[i].Waypoints.Last().Blip.Delete();
                    PathMainMenu.paths[i].Waypoints.Last().RemoveSpeedZone();

                    if (PathMainMenu.paths[i].Waypoints.Last().CollectorRadiusBlip)
                    {
                        PathMainMenu.paths[i].Waypoints.Last().CollectorRadiusBlip.Delete();
                    }
                    PathMainMenu.paths[i].Waypoints.RemoveAt(PathMainMenu.paths[i].Waypoints.IndexOf(PathMainMenu.paths[i].Waypoints.Last()));

                    ToggleTrafficEndPathMenuItem(i);

                    // If the path has no waypoints, disable the menu option to remove a waypoint
                    if (PathMainMenu.paths[i].Waypoints.Count == 0)
                    {
                        collectorWaypoint.Checked = true;
                        collectorWaypoint.Enabled = false;
                        speedZoneRadius.Enabled = true;
                        collectorRadius.Enabled = true;
                        trafficRemoveWaypoint.Enabled = false;
                        trafficEndPath.Enabled = false;
                    }
                }
            }
        }

        private static void EndPath()
        {
            for (int i = 0; i < PathMainMenu.paths.Count; i++)
            {
                var currentPath = PathMainMenu.paths[i];
                if (PathMainMenu.paths.ElementAtOrDefault(i) != null && currentPath.State == State.Creating)
                {
                    Game.LogTrivial($"[Path Creation] Path {currentPath.Number} finished with {currentPath.Waypoints.Count} waypoints.");
                    Game.DisplayNotification($"~o~Scene Manager ~g~[Success]\n~w~Path {i + 1} complete.");
                    currentPath.State = State.Finished;
                    currentPath.IsEnabled = true;
                    currentPath.Number = i + 1;
                    currentPath.LoopForVehiclesToBeDismissed();

                    GameFiber.StartNew(() =>
                    {
                        currentPath.LoopWaypointCollection();
                    });

                    PathMainMenu.createNewPath.Text = "Create New Path";
                    PathMainMenu.BuildPathMenu();
                    PathMainMenu.pathMainMenu.RefreshIndex();
                    pathCreationMenu.Clear();
                    PathMainMenu.pathMainMenu.Visible = true;
                    break;
                }
            }
        }

        private static void ToggleTrafficEndPathMenuItem(int pathIndex)
        {
            if (PathMainMenu.paths[pathIndex].Waypoints.Count > 0)
            {
                trafficEndPath.Enabled = true;
            }
            else
            {
                trafficEndPath.Enabled = false;
            }
        }

        internal static void AddNewPathToPathsCollection(List<Path> paths, int pathIndex)
        {
            var pathNum = pathIndex + 1;
            Game.LogTrivial($"Creating path {pathNum}");
            Game.DisplayNotification($"~o~Scene Manager\n~y~[Creating]~w~ Path {pathNum} started.");
            paths.Insert(pathIndex, new Path(pathNum, State.Creating));
            trafficRemoveWaypoint.Enabled = false;
            trafficEndPath.Enabled = false;
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
    }
}
