using System.Collections.Generic;

namespace SceneManager
{
    public class PathData
    { 
        public int PathNum;
        public bool PathFinished;
        public List<WaypointData> WaypointData = new List<WaypointData>() { };

        public PathData(int pathNum, bool pathFinished, List<WaypointData> waypointData)
        {
            PathNum = pathNum;
            PathFinished = pathFinished;
            WaypointData = waypointData;
        }

        public PathData(int pathNum, bool pathFinished)
        {
            PathNum = pathNum;
            PathFinished = pathFinished;
        }
    }
}
