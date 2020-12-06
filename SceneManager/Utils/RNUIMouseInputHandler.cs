using InputManager;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;


namespace SceneManager.Utils
{
    internal class RNUIMouseInputHandler
    {
        internal delegate void Function();

        internal static void Initialize(UIMenu menu, List<UIMenuScrollerItem> scrollerItems)
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

                    if (Game.IsKeyDown(Keys.LButton) && Rage.Native.NativeFunction.Natives.UPDATE_ONSCREEN_KEYBOARD<int>() != 0)
                    {
                        Keyboard.KeyDown(Keys.Enter);
                        GameFiber.Wait(1);
                        Keyboard.KeyUp(Keys.Enter);
                    }

                    if (menu.SubtitleText.Contains("Path Creation Menu"))
                    {
                        DrawWaypointMarker();
                    }
                    GameFiber.Yield();
                }
            });
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
                if (Game.GetMouseWheelDelta() > 0)
                {
                    Keyboard.KeyDown(Keys.Right);
                    GameFiber.Wait(1);
                    Keyboard.KeyUp(Keys.Right);
                }
                else if (Game.GetMouseWheelDelta() < 0)
                {
                    Keyboard.KeyDown(Keys.Left);
                    GameFiber.Wait(1);
                    Keyboard.KeyUp(Keys.Left);
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
