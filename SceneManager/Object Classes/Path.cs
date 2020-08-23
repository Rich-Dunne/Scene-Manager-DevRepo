using System.Collections.Generic;

namespace SceneManager
{
    public enum Status
    {
        IsEnabled,
        IsDisabled
    }

    public class Path
    { 
        public int PathNum { get; private set; }
        public bool PathFinished { get; private set; }
        public bool IsEnabled { get; private set; }
        public Status Status { get; private set; }

        public List<Waypoint> Waypoints = new List<Waypoint>();

        public Path(int pathNum, bool pathFinished, bool pathDisabled, List<Waypoint> waypoints)
        {
            PathNum = pathNum;
            PathFinished = pathFinished;
            IsEnabled = pathDisabled;
            Waypoints = waypoints;
        }

        public Path(int pathNum, bool pathFinished)
        {
            PathNum = pathNum;
            PathFinished = pathFinished;
        }

        public void SetPathStatus(bool status)
        {
            IsEnabled = status;
        }

        public void SetPathNumber(int pathNum)
        {
            PathNum = pathNum;
        }

        public void FinishPath()
        {
            PathFinished = true;
        }

        public void DisablePath()
        {
            IsEnabled = false;
            LowerWaypointBlipsOpacity();
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

        public void EnablePath()
        {
            IsEnabled = true;
        }

        private void RestoreWaypointBlipsOpacity()
        {
            foreach (Waypoint wp in Waypoints)
            {
                wp.Blip.Alpha = 1.0f;
                if (wp.CollectorRadiusBlip)
                {
                    wp.CollectorRadiusBlip.Alpha = 0.5f;
                }
            }
        }
    }
}
