using System.Collections.Generic;

namespace SceneManager
{
    public enum State
    {
        Uninitialized,
        Creating,
        Finished
    }

    public class Path
    { 
        public int PathNum { get; private set; }
        //public bool PathFinished { get; private set; }
        public bool IsEnabled { get; private set; }
        public State State { get; set; }

        public List<Waypoint> Waypoints = new List<Waypoint>();

        public Path(int pathNum, bool pathFinished, bool pathDisabled, List<Waypoint> waypoints)
        {
            PathNum = pathNum;
            //PathFinished = pathFinished;
            IsEnabled = pathDisabled;
            Waypoints = waypoints;
        }

        public Path(int pathNum, State pathState)
        {
            PathNum = pathNum;
            State = pathState;
            //PathFinished = pathFinished;
        }

        public void SetPathNumber(int pathNum)
        {
            PathNum = pathNum;
        }

        //public void FinishPath()
        //{
        //    PathFinished = true;
        //}

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
                wp.Blip.Alpha = 1.0f;
                if (wp.CollectorRadiusBlip)
                {
                    wp.CollectorRadiusBlip.Alpha = 0.5f;
                }
            }
        }

        public void DisablePath()
        {
            IsEnabled = false;
            LowerWaypointBlipsOpacity();
        }

        public void EnablePath()
        {
            IsEnabled = true;
        }




    }
}
