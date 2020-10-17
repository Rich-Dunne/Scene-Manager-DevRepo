using Rage;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SceneManager
{
    public class Path
    {
        internal int Number { get; set; }
        internal bool IsEnabled { get; set; }
        internal State State { get; set; }
        internal List<Waypoint> Waypoints = new List<Waypoint>();
        internal List<CollectedVehicle> CollectedVehicles = new List<CollectedVehicle>();

        internal Path(int pathNum, State pathState)
        {
            Number = pathNum;
            State = pathState;
            DrawLinesBetweenWaypoints();
        }

        private void LowerWaypointBlipsOpacity()
        {
            foreach (Waypoint wp in Waypoints)
            {
                wp.Blip.Alpha = 0.5f;
                if (wp.CollectorRadiusBlip)
                {
                    wp.CollectorRadiusBlip.Alpha = 0.25f;
                }
            }
        }

        private void RestoreWaypointBlipsOpacity()
        {
            foreach (Waypoint wp in Waypoints)
            {
                if (wp.Blip)
                {
                    wp.Blip.Alpha = 1.0f;
                    if (wp.CollectorRadiusBlip)
                    {
                        wp.CollectorRadiusBlip.Alpha = 0.5f;
                    }
                }
            }
        }

        internal void DisablePath()
        {
            IsEnabled = false;
            foreach(Waypoint wp in Waypoints)
            {
                wp.RemoveSpeedZone();
            }
            if (SettingsMenu.mapBlips.Checked)
            {
                LowerWaypointBlipsOpacity();
            }
        }

        internal void EnablePath()
        {
            IsEnabled = true;
            foreach (Waypoint wp in Waypoints)
            {
                if (wp.IsCollector)
                {
                    wp.AddSpeedZone();
                }
            }
            if (SettingsMenu.mapBlips.Checked)
            {
                RestoreWaypointBlipsOpacity();
            }
        }

        internal void DrawLinesBetweenWaypoints()
        {
            GameFiber.StartNew(() =>
            {
                while (SettingsMenu.threeDWaypoints.Checked)
                {
                    if (MenuManager.menuPool.IsAnyMenuOpen())
                    {
                        for (int i = 0; i < Waypoints.Count; i++)
                        {
                            if (i != Waypoints.Count - 1)
                            {
                                if (Waypoints[i + 1].IsStopWaypoint)
                                {
                                    Debug.DrawLine(Waypoints[i].Position, Waypoints[i + 1].Position, Color.Orange);
                                }
                                else
                                {
                                    Debug.DrawLine(Waypoints[i].Position, Waypoints[i + 1].Position, Color.Green);
                                }
                            }
                        }
                    }
                    GameFiber.Yield();
                }
            });
        }

        internal void LoopForVehiclesToBeDismissed()
        {
            GameFiber.StartNew(() =>
            {
                while (PathMainMenu.paths.Contains(this))
                {
                    //Logger.Log($"Dismissing unused vehicles for cleanup");
                    foreach (CollectedVehicle cv in CollectedVehicles.Where(cv => cv.Vehicle))
                    {
                        if (!cv.Vehicle.IsDriveable || cv.Vehicle.IsUpsideDown || !cv.Vehicle.HasDriver)
                        {
                            if (cv.Vehicle.HasDriver)
                            {
                                cv.Vehicle.Driver.Dismiss();
                            }
                            cv.Vehicle.Dismiss();
                        }
                    }

                    CollectedVehicles.RemoveAll(cv => !cv.Vehicle);
                    GameFiber.Sleep(60000);
                }
            });
        }
    }
}
