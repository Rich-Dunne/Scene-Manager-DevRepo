using Rage;
using RAGENativeUI.Elements;
using System.Drawing;

namespace SceneManager
{
    class DebugGraphics
    {
        public static void LoopToDrawDebugGraphics(UIMenuCheckboxItem debugGraphics, Path path)
        {
            GameFiber.StartNew(() =>
            {
                while (debugGraphics.Checked)
                {
                    if (MenuManager.menuPool.IsAnyMenuOpen() && path != null)
                    {
                        for (int i = 0; i < path.Waypoints.Count; i++)
                        {
                            //Draw3DMarkersAtWaypoints(path, i);
                            path.Waypoints[i].DrawWaypointMarker();

                            if (i != path.Waypoints.Count - 1)
                            {
                                DrawLinesBetweenWaypoints(path, i);
                            }
                        }
                    }
                    GameFiber.Yield();
                }
            });
        }

        private static void DrawLinesBetweenWaypoints(Path path, int i)
        {
            if (path.Waypoints[i + 1].DrivingFlag == VehicleDrivingFlags.StopAtDestination)
            {
                Debug.DrawLine(path.Waypoints[i].Position, path.Waypoints[i + 1].Position, Color.Orange);
            }
            else
            {
                Debug.DrawLine(path.Waypoints[i].Position, path.Waypoints[i + 1].Position, Color.Green);
            }
        }

        public static void Draw3DWaypointOnPlayer()
        {
            GameFiber.StartNew(() =>
            {
                while (SettingsMenu.debugGraphics.Checked)
                {
                    if (PathCreationMenu.pathCreationMenu.Visible)
                    {
                        if (PathCreationMenu.collectorWaypoint.Checked)
                        {
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, (float)PathCreationMenu.collectorRadius.Value, (float)PathCreationMenu.collectorRadius.Value, 1f, 80, 130, 255, 80, false, false, 2, false, 0, 0, false);
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, (float)PathCreationMenu.speedZoneRadius.Value, (float)PathCreationMenu.speedZoneRadius.Value, 1f, 255, 185, 80, 80, false, false, 2, false, 0, 0, false);
                        }
                        else if (PathCreationMenu.waypointType.SelectedItem == "Drive To")
                        {
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z-1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 65, 255, 65, 80, false, false, 2, false, 0, 0, false);
                        }
                        else
                        {
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z-1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 255, 65, 65, 80, false, false, 2, false, 0, 0, false);
                        }
                    }
                    GameFiber.Yield();
                }
            });
        }
    }
}
