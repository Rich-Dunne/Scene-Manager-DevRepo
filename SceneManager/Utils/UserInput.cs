using InputManager;
using Rage;
using Rage.Native;
using RAGENativeUI;
using RAGENativeUI.Elements;
using SceneManager.Managers;
using SceneManager.Menus;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SceneManager.Utils
{
    // The only reason this class should change is to modify how user input is handled
    class UserInput
    {
        private static bool _menuKeysPressed
        {
            get => (Settings.ModifierKey == Keys.None && Game.IsKeyDown(Settings.ToggleKey)) ||
                (Game.IsKeyDownRightNow(Settings.ModifierKey) && Game.IsKeyDown(Settings.ToggleKey));
        }
        private static bool _menuControllerButtonsPressed
        {
            get => (Settings.ModifierButton == ControllerButtons.None && Game.IsControllerButtonDown(Settings.ToggleButton)) ||
                (Game.IsControllerButtonDownRightNow(Settings.ModifierButton) && Game.IsControllerButtonDown(Settings.ToggleButton));
        }
        internal static Vector3 PlayerMousePosition { get => GetMousePositionInWorld(); }
        internal static Vector3 PlayerMousePositionForBarrier { get => GetMousePositionInWorld(Settings.BarrierPlacementDistance); }

        internal static void HandleKeyPress()
        {
            while (true)
            {
                GameFiber.Yield();

                bool isTextEntryOpen = (NativeFunction.Natives.UPDATE_ONSCREEN_KEYBOARD<int>() == 0);
                if (!isTextEntryOpen && MenuKeysPressed())
                {
                    if (MenuManager.MenuPool.Any(x => x.Visible))
                    {
                        foreach (UIMenu menu in MenuManager.MenuPool.Where(x => x.Visible))
                        {
                            menu.Visible = !menu.Visible;
                        }
                        MenuManager.MenuPool.CloseAllMenus();
                        continue;
                    }

                    Menus.MainMenu.DisplayMenu();
                    GameFiber.StartNew(() => MenuManager.ProcessMenus(), "Menu Processing Fiber");
                }

#if DEBUG
                if (MenuManager.MenuPool.IsAnyMenuOpen())
                {
                    Game.DisplaySubtitle($"You are using a test build of ~y~Scene Manager~w~.  Please report any ~r~bugs/crashes ~w~in the ~p~Discord ~w~server.");
                }
#endif
            }
        }

        private static bool MenuKeysPressed()
        {
            if (_menuKeysPressed || _menuControllerButtonsPressed)
            {
                return true;
            }

            return false;
        }

        private static Vector3 GetMousePositionInWorld(float maxDistance = 100f)
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

            return TracePlayerView(maxDistance, TraceFlags.IntersectWorld).HitPosition;
        }

        internal static void InitializeMenuMouseControl(UIMenu menu, List<UIMenuScrollerItem> scrollerItems)
        {
            while (menu.Visible)
            {
                var selectedScroller = menu.MenuItems.FirstOrDefault(x => scrollerItems.Contains(x) && x.Selected && x.Enabled);
                if (selectedScroller != null)
                {
                    OnWheelScroll(menu, selectedScroller, scrollerItems);
                }

                if (Game.IsKeyDown(Keys.LButton) && NativeFunction.Natives.UPDATE_ONSCREEN_KEYBOARD<int>() != 0)
                {
                    Keyboard.KeyDown(Keys.Enter);
                    GameFiber.Wait(1);
                    Keyboard.KeyUp(Keys.Enter);
                }

                if (menu.SubtitleText.Contains("Path Creation Menu"))
                {
                    DrawWaypointMarkerAtMousePosition();
                }
                GameFiber.Yield();
            }
        }

        private static void OnWheelScroll(UIMenu menu, UIMenuItem selectedScroller, List<UIMenuScrollerItem> scrollerItems)
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
                if (menu.SubtitleText.Contains("Path Creation Menu"))
                {
                    DrawWaypointMarkerAtMousePosition();
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

        private static void DrawWaypointMarkerAtMousePosition()
        {
            var waypointPosition = PlayerMousePosition;
            if (SettingsMenu.ThreeDWaypoints.Checked && PathCreationMenu.CollectorWaypoint.Checked)
            {
                NativeFunction.Natives.DRAW_MARKER(1, waypointPosition, 0, 0, 0, 0, 0, 0, (float)PathCreationMenu.CollectorRadius.Value * 2, (float)PathCreationMenu.CollectorRadius.Value * 2, 1f, 80, 130, 255, 80, false, false, 2, false, 0, 0, false);
                NativeFunction.Natives.DRAW_MARKER(1, waypointPosition, 0, 0, 0, 0, 0, 0, (float)PathCreationMenu.SpeedZoneRadius.Value * 2, (float)PathCreationMenu.SpeedZoneRadius.Value * 2, 1f, 255, 185, 80, 80, false, false, 2, false, 0, 0, false);
            }
            else if (PathCreationMenu.StopWaypoint.Checked)
            {
                NativeFunction.Natives.DRAW_MARKER(1, waypointPosition, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 255, 65, 65, 80, false, false, 2, false, 0, 0, false);
            }
            else
            {
                NativeFunction.Natives.DRAW_MARKER(1, waypointPosition, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 65, 255, 65, 80, false, false, 2, false, 0, 0, false);
            }
        }

        internal static string PromptPlayerForFileName(string windowTitle, string defaultText, int maxLength)
        {
            NativeFunction.Natives.DISABLE_ALL_CONTROL_ACTIONS(2);

            NativeFunction.Natives.DISPLAY_ONSCREEN_KEYBOARD(true, windowTitle, 0, defaultText, 0, 0, 0, maxLength);
            Game.DisplayHelp("Enter the filename you would like to save your path as\n~INPUT_FRONTEND_ACCEPT~    Export path\n~INPUT_FRONTEND_CANCEL~    Cancel", true);
            Game.DisplaySubtitle(windowTitle, 100000);

            while (NativeFunction.Natives.UPDATE_ONSCREEN_KEYBOARD<int>() == 0)
            {
                GameFiber.Yield();
            }

            NativeFunction.Natives.ENABLE_ALL_CONTROL_ACTIONS(2);
            Game.DisplaySubtitle("", 5);
            Game.HideHelp();

            return NativeFunction.Natives.GET_ONSCREEN_KEYBOARD_RESULT<string>();
        }

    }
}
