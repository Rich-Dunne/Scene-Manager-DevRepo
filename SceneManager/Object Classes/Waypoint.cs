using Rage;

namespace SceneManager
{
    public class Waypoint
    {
        public int Path;
        public int WaypointNum;
        public Vector3 WaypointPos;
        public float Speed;
        public VehicleDrivingFlags DrivingFlag;
        public Blip WaypointBlip;
        public uint YieldZone;
        public bool Collector;
        public float CollectorRadius;
        public Blip CollectorRadiusBlip;

        // Can this constructor be deleted?
        //public Waypoint(int path, Vector3 waypointPos, float speed, VehicleDrivingFlags drivingFlag, Blip waypointBlip, uint yieldZone)
        //{
        //    Path = path;
        //    WaypointPos = waypointPos;
        //    Speed = speed;
        //    DrivingFlag = drivingFlag;
        //    WaypointBlip = waypointBlip;
        //    YieldZone = yieldZone;
        //}

        public Waypoint(int path, int waypointNum, Vector3 waypointPos, float speed, VehicleDrivingFlags drivingFlag, Blip waypointBlip, bool collector, float collectorRadius, uint yieldZone)
        {
            Path = path;
            WaypointNum = waypointNum;
            WaypointPos = waypointPos;
            Speed = speed;
            DrivingFlag = drivingFlag;
            WaypointBlip = waypointBlip;
            Collector = collector;
            CollectorRadius = collectorRadius;
            if (collector)
            {
                YieldZone = yieldZone;
                CollectorRadiusBlip = new Blip(waypointBlip.Position, collectorRadius)
                {
                    Color = waypointBlip.Color,
                    Alpha = 0.5f
                };


            }
        }
    }
}
