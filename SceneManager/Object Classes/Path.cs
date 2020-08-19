using System.Collections.Generic;

namespace SceneManager
{
    public class Path
    { 
        public int PathNum;
        public bool PathFinished;
        public bool PathDisabled;
        public List<Waypoint> Waypoint = new List<Waypoint>() { };

        public Path(int pathNum, bool pathFinished, bool pathDisabled, List<Waypoint> waypointData)
        {
            PathNum = pathNum;
            PathFinished = pathFinished;
            PathDisabled = pathDisabled;
            Waypoint = waypointData;
        }

        public Path(int pathNum, bool pathFinished)
        {
            PathNum = pathNum;
            PathFinished = pathFinished;
        }
    }
}
