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
                for (int i = 0; i < path.Waypoint.Count; i++)
                {
                    DrawSpheresAtWaypoints(path, i);

                    if (i != path.Waypoint.Count - 1)
                    {
                        DrawLinesBetweenWaypoints(path, i);
                    }
                }
                GameFiber.Yield();
            }
        }

        public static void DrawLinesBetweenWaypoints(Path path, int i)
        {
            if (path.Waypoint[i + 1].DrivingFlag == VehicleDrivingFlags.StopAtDestination)
            {
                Debug.DrawLine(path.Waypoint[i].Position, path.Waypoint[i + 1].Position, Color.Orange);
            }
            else
            {
                Debug.DrawLine(path.Waypoint[i].Position, path.Waypoint[i + 1].Position, Color.Green);
            }
        }

        public static void DrawSpheresAtWaypoints(Path path, int i)
        {
            if (path.Waypoint[i].Collector)
            {
                Debug.DrawSphere(path.Waypoint[i].Position, path.Waypoint[i].CollectorRadius, Color.FromArgb(80, Color.Blue));
            }
            else if (path.Waypoint[i].DrivingFlag == VehicleDrivingFlags.StopAtDestination)
            {
                Debug.DrawSphere(path.Waypoint[i].Position, 1f, Color.FromArgb(80, Color.Red));
            }
            else
            {
                Debug.DrawSphere(path.Waypoint[i].Position, 1f, Color.FromArgb(80, Color.Green));
            }
        }
    }
}
