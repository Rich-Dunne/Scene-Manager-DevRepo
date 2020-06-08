using Rage;

namespace SceneManager
{
    public class WaypointData
    {
        public int Path;
        public Vector3 WaypointPos;
        public float Speed;
        public VehicleDrivingFlags DrivingFlag;
        public Blip WaypointBlip;
        public uint YieldZone;

        public WaypointData(int path, Vector3 waypointPos, float speed, VehicleDrivingFlags drivingFlag, Blip waypointBlip, uint yieldZone)
        {
            Path = path;
            WaypointPos = waypointPos;
            Speed = speed;
            DrivingFlag = drivingFlag;
            WaypointBlip = waypointBlip;
            YieldZone = yieldZone;
        }

        public WaypointData(int path, Vector3 waypointPos, float speed, VehicleDrivingFlags drivingFlag, Blip waypointBlip)
        {
            Path = path;
            WaypointPos = waypointPos;
            Speed = speed;
            DrivingFlag = drivingFlag;
            WaypointBlip = waypointBlip;
        }
    }
}
