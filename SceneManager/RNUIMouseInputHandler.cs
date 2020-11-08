using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SceneManager
{
    internal class RNUIMouseInputHandler
    {
        internal delegate void Function();

        internal static void Initialize(UIMenu menu, List<UIMenuScrollerItem> scrollerItems, Dictionary<UIMenuCheckboxItem, Function> checkboxItems, Dictionary<UIMenuItem, Function> selectItems)
        {
            GameFiber.StartNew(() =>
            {
                while (menu.Visible)
                {
                    var selectedScroller = menu.MenuItems.Where(x => scrollerItems.Contains(x) && x.Selected && x.Enabled).FirstOrDefault();
                    if (selectedScroller != null)
                    {
                        OnWheelScroll(menu, selectedScroller, scrollerItems);
                    }

                    if (Game.IsKeyDown(Keys.LButton))
                    {
                        var selectedItem = menu.MenuItems.Where(x => x.Enabled && x.Selected).FirstOrDefault();
                        if (selectedItem != null)
                        {
                            //Game.LogTrivial($"selectedItem: {selectedItem.Text}");
                            //Game.LogTrivial($"scrollerItems contains: {scrollerItems.Contains(selectedItem)}");
                            if (selectItems.ContainsKey(selectedItem))
                            {
                                OnMenuItemClicked(selectItems);
                            }
                            else if (!scrollerItems.Contains(selectedItem) && checkboxItems.ContainsKey((UIMenuCheckboxItem)selectedItem))
                            {
                                OnCheckboxItemClicked(checkboxItems);
                            }
                        }

                        if(menu.SubtitleText == "~o~Main Menu")
                        {
                            menu.Visible = false;
                        }
                    }

                    if (menu.SubtitleText.Contains("Path Creation Menu"))
                    {
                        DrawWaypointMarker();
                    }
                    GameFiber.Yield();
                }
            });
        }

        internal static void OnCheckboxItemClicked(Dictionary<UIMenuCheckboxItem, Function> checkboxItems)
        {
            var checkedItem = checkboxItems.Keys.Where(x => x.Selected && x.Enabled).FirstOrDefault();
            if(checkedItem != null)
            {
                checkedItem.Checked = !checkedItem.Checked;
                if(checkboxItems.TryGetValue(checkedItem, out Function func))
                {
                    func?.Invoke();
                }
            }
        }

        internal static void OnMenuItemClicked(Dictionary<UIMenuItem, Function> selectItems)
        {
            var selectedItem = selectItems.Keys.Where(x => x.Selected && x.Enabled).FirstOrDefault();
            //Game.LogTrivial($"selectedItem: {selectedItem?.Text}");
            if (selectedItem != null)
            {
                if (selectItems.TryGetValue(selectedItem, out Function func))
                {
                    func?.Invoke();
                }
            }
        }

        internal static void OnWheelScroll(UIMenu menu, UIMenuItem selectedScroller, List<UIMenuScrollerItem> scrollerItems)
        {
            var menuScrollingDisabled = false;
            var menuItems = menu.MenuItems.Where(x => x != selectedScroller);

            while (Game.IsShiftKeyDownRightNow)
            {
                menu.ResetKey(Common.MenuControls.Up);
                menu.ResetKey(Common.MenuControls.Down);
                menuScrollingDisabled = true;
                ScrollMenuItem();
                if (menu.SubtitleText.Contains("Path Creation Menu") || menu.SubtitleText.Contains("Edit Waypoint"))
                {
                    CompareScrollerValues();
                }
                if(menu.SubtitleText.Contains("Path Creation Menu"))
                {
                    DrawWaypointMarker();
                }
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
                //Game.LogTrivial($"Selected scroller: {selectedScroller.Text}");
                if (Game.GetMouseWheelDelta() > 0)
                {
                    foreach (var item in scrollerItems)
                    {
                        if (item == selectedScroller)
                        {
                            //Game.LogTrivial($"item text: {item.Text}");
                            item.ScrollToNextOption();
                            if (menu.SubtitleText.ToLower().Contains("barrier"))
                            {
                                HandleBarrierMenuItems(item);
                            }
                            if(item.Text == "Edit Waypoint")
                            {
                                UpdateEditWaypointMenuItems();
                            }
                        }
                    }
                }
                else if (Game.GetMouseWheelDelta() < 0)
                {
                    foreach (var item in scrollerItems)
                    {
                        if (item == selectedScroller)
                        {
                            item.ScrollToPreviousOption();
                            if (menu.SubtitleText.ToLower().Contains("barrier"))
                            {
                                HandleBarrierMenuItems(item);
                            }
                            if (item.Text == "Edit Waypoint")
                            {
                                UpdateEditWaypointMenuItems();
                            }
                        }
                    }
                }

                void HandleBarrierMenuItems(UIMenuItem item)
                {
                    if (item.Text == "Spawn Barrier")
                    {
                        if (BarrierMenu.shadowBarrier)
                        {
                            BarrierMenu.shadowBarrier.Delete();
                        }
                        var changeTextureItem = scrollerItems.Where(x => x.Text == "Change Texture").FirstOrDefault().Index = 0;
                        var listScrollerItem = (UIMenuListScrollerItem<string>)item;
                        if (listScrollerItem.SelectedItem == "Flare")
                        {
                            scrollerItems.Where(x => x.Text == "Rotate Barrier").FirstOrDefault().Enabled = false;
                        }
                        else
                        {
                            scrollerItems.Where(x => x.Text == "Rotate Barrier").FirstOrDefault().Enabled = true;
                        }
                        menu.Width = BarrierMenu.SetMenuWidth();
                    }
                    else if (item.Text == "Rotate Barrier")
                    {
                        BarrierMenu.RotateBarrier();
                    }
                    else if(item.Text == "Change Texture")
                    {
                        var numericScrollerItem = (UIMenuNumericScrollerItem<int>)item;
                        Rage.Native.NativeFunction.Natives.x971DA0055324D033(BarrierMenu.shadowBarrier, numericScrollerItem.Value);
                    }
                }

                void UpdateEditWaypointMenuItems()
                {
                    var currentPath = PathMainMenu.paths[PathMainMenu.editPath.Index];
                    var editWaypoint = (UIMenuNumericScrollerItem<int>)menu.MenuItems.Where(x => x.Text == "Edit Waypoint").FirstOrDefault();
                    var collectorWaypoint = (UIMenuCheckboxItem)menu.MenuItems.Where(x => x.Text == "Collector").FirstOrDefault();
                    var changeCollectorRadius = (UIMenuNumericScrollerItem<int>)menu.MenuItems.Where(x => x.Text == "Collection Radius").FirstOrDefault();
                    var changeSpeedZoneRadius = (UIMenuNumericScrollerItem<int>)menu.MenuItems.Where(x => x.Text == "Speed Zone Radius").FirstOrDefault();
                    var stopWaypointType = (UIMenuCheckboxItem)menu.MenuItems.Where(x => x.Text == "Is this a Stop waypoint?").FirstOrDefault();
                    var directWaypointBehavior = (UIMenuCheckboxItem)menu.MenuItems.Where(x => x.Text == "Drive directly to waypoint?").FirstOrDefault();
                    var changeWaypointSpeed = (UIMenuNumericScrollerItem<int>)menu.MenuItems.Where(x => x.Text == "Waypoint Speed").FirstOrDefault();
                    var updateWaypointPosition = (UIMenuCheckboxItem)menu.MenuItems.Where(x => x.Text == "Update Waypoint Position").FirstOrDefault();
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

            void CompareScrollerValues()
            {
                var collectorRadius = (UIMenuNumericScrollerItem<int>)scrollerItems.Where(x => x.Text == "Collection Radius").FirstOrDefault();
                var speedZoneRadius = (UIMenuNumericScrollerItem<int>)scrollerItems.Where(x => x.Text == "Speed Zone Radius").FirstOrDefault();

                if (selectedScroller.Text == "Collection Radius" || selectedScroller.Text == "Speed Zone Radius")
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

        private static void DrawWaypointMarker()
        {
            var waypointPosition = MousePositionInWorld.GetPosition;
            if (SettingsMenu.threeDWaypoints.Checked && PathCreationMenu.collectorWaypoint.Checked)
            {
                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, waypointPosition, 0, 0, 0, 0, 0, 0, (float)PathCreationMenu.collectorRadius.Value * 2, (float)PathCreationMenu.collectorRadius.Value * 2, 1f, 80, 130, 255, 80, false, false, 2, false, 0, 0, false);
                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, waypointPosition, 0, 0, 0, 0, 0, 0, (float)PathCreationMenu.speedZoneRadius.Value * 2, (float)PathCreationMenu.speedZoneRadius.Value * 2, 1f, 255, 185, 80, 80, false, false, 2, false, 0, 0, false);
            }
            else if (PathCreationMenu.stopWaypointType.Checked)
            {
                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, waypointPosition, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 255, 65, 65, 80, false, false, 2, false, 0, 0, false);
            }
            else
            {
                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, waypointPosition, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 65, 255, 65, 80, false, false, 2, false, 0, 0, false);
            }
        }
    }
}
