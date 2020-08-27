using Rage;
using RAGENativeUI.Elements;
using System.Drawing;

namespace SceneManager
{
    class DebugGraphics
    {
        public static void LoopToDrawDebugGraphics(UIMenuCheckboxItem debugGraphics, Path path)
        {
            while (debugGraphics.Checked && path != null)
            {
                for (int i = 0; i < path.Waypoints.Count; i++)
                {
                    DrawSpheresAtWaypoints(path, i);

                    if (i != path.Waypoints.Count - 1)
                    {
                        DrawLinesBetweenWaypoints(path, i);
                    }
                }
                GameFiber.Yield();
            }
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

        private static void DrawSpheresAtWaypoints(Path path, int i)
        {
            if (path.Waypoints[i].IsCollector)
            {
                Debug.DrawSphere(path.Waypoints[i].Position, path.Waypoints[i].CollectorRadius, Color.FromArgb(80, Color.Blue));
            }
            else if (path.Waypoints[i].DrivingFlag == VehicleDrivingFlags.StopAtDestination)
            {
                Debug.DrawSphere(path.Waypoints[i].Position, 1f, Color.FromArgb(80, Color.Red));
            }
            else
            {
                Debug.DrawSphere(path.Waypoints[i].Position, 1f, Color.FromArgb(80, Color.Green));
            }
        }

        public static void DrawSphereOnPlayer(UIMenuCheckboxItem debugGraphics)
        {
            GameFiber.StartNew(() =>
            {
                while (debugGraphics.Checked)
                {
                    if (PathCreationMenu.pathCreationMenu.Visible)
                    {
                        if (PathCreationMenu.collectorWaypoint.Checked)
                        {
                            Debug.DrawSphere(Game.LocalPlayer.Character.Position, PathCreationMenu.collectorRadius.Value, Color.FromArgb(80, Color.Blue));
                        }
                        else if (PathCreationMenu.waypointType.SelectedItem == "Drive To")
                        {
                            Debug.DrawSphere(Game.LocalPlayer.Character.Position, PathCreationMenu.collectorRadius.Value, Color.FromArgb(80, Color.Green));
                        }
                        else
                        {
                            Debug.DrawSphere(Game.LocalPlayer.Character.Position, PathCreationMenu.collectorRadius.Value, Color.FromArgb(80, Color.Red));
                        }
                    }
                    GameFiber.Yield();
                }
            });
        }
    }
}
