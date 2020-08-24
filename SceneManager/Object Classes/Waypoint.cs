using Rage;
using System.Runtime.InteropServices;

namespace SceneManager
{
    public class Waypoint
    {
        public int Path { get; private set; }
        public int Number { get; private set; }
        public Vector3 Position { get; private set; }
        public float Speed { get; private set; }
        public VehicleDrivingFlags DrivingFlag { get; private set; }
        public Blip Blip { get; private set; }
        public uint YieldZone { get; private set; }
        public bool IsCollector { get; private set; }
        public float CollectorRadius { get; private set; }
        public Blip CollectorRadiusBlip { get; private set; }

        public Waypoint(int path, int waypointNum, Vector3 waypointPos, float speed, VehicleDrivingFlags drivingFlag, Blip waypointBlip)
        {
            Path = path;
            Number = waypointNum;
            Position = waypointPos;
            Speed = speed;
            DrivingFlag = drivingFlag;
            Blip = waypointBlip;

        }

        public Waypoint(int path, int waypointNum, Vector3 waypointPos, float speed, VehicleDrivingFlags drivingFlag, Blip waypointBlip, bool collector, float collectorRadius, uint yieldZone)
        {
            Path = path;
            Number = waypointNum;
            Position = waypointPos;
            Speed = speed;
            DrivingFlag = drivingFlag;
            Blip = waypointBlip;
            IsCollector = collector;
            CollectorRadius = collectorRadius;
            YieldZone = yieldZone;
            CollectorRadiusBlip = new Blip(waypointBlip.Position, collectorRadius)
            {
                Color = waypointBlip.Color,
                Alpha = 0.5f
            };
        }

        public void UpdateWaypoint(Waypoint currentWaypoint, VehicleDrivingFlags drivingFlag, float drivingSpeed, bool collectorWaypointChecked, float collectorRadius, bool updateWaypointPositionChecked)
        {
            UpdateDrivingFlag(drivingFlag);
            UpdateWaypointSpeed(drivingSpeed);
            UpdateCollectorOptions(currentWaypoint, drivingSpeed, collectorWaypointChecked, collectorRadius);
            if (updateWaypointPositionChecked)
            {
                UpdateWaypointPosition(Game.LocalPlayer.Character.Position);
            }
        }

        private void UpdateCollectorOptions(Waypoint currentWaypoint, float drivingSpeed, bool collectorWaypointChecked, float collectorRadius)
        {
            if (collectorWaypointChecked)
            {
                IsCollector = true;
                World.RemoveSpeedZone(YieldZone);
                YieldZone = World.AddSpeedZone(Game.LocalPlayer.Character.Position, 50f, drivingSpeed);
                if (CollectorRadiusBlip)
                {
                    currentWaypoint.CollectorRadiusBlip.Alpha = 0.5f;
                    currentWaypoint.CollectorRadiusBlip.Scale = collectorRadius;
                }
                else
                {
                    CollectorRadiusBlip = new Blip(Blip.Position, collectorRadius)
                    {
                        Color = Blip.Color,
                        Alpha = 0.5f
                    };
                }
                CollectorRadius = collectorRadius;
            }
            else
            {
                IsCollector = false;
                World.RemoveSpeedZone(YieldZone);
                if (CollectorRadiusBlip)
                {
                    CollectorRadiusBlip.Delete();
                }
            }
        }

        private void UpdateWaypointPosition(Vector3 newWaypointPosition)
        {
            Position = newWaypointPosition;
            UpdateWaypointBlipPosition();
        }

        private void UpdateWaypointSpeed(float newWaypointSpeed)
        {
            Speed = newWaypointSpeed;
        }

        private void UpdateDrivingFlag(VehicleDrivingFlags newDrivingFlag)
        {
            DrivingFlag = newDrivingFlag;
        }

        private void UpdateWaypointBlipPosition()
        {
            Blip.Position = Game.LocalPlayer.Character.Position;
        }

        public void UpdatePathNumber(int newPathNumber)
        {
            Path = newPathNumber;
        }

        public void UpdateWaypointNumber(int newWaypointNumber)
        {
            Number = newWaypointNumber;
        }
    }
}
