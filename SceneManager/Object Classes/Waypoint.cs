using Rage;
using System.Drawing;

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
        public float SpeedZoneRadius { get; private set; }
        public Blip CollectorRadiusBlip { get; private set; }
        private bool _enableWaypointMarker { get; set; }

        public Waypoint(int path, int waypointNum, Vector3 waypointPos, float speed, VehicleDrivingFlags drivingFlag, Blip waypointBlip)
        {
            Path = path;
            Number = waypointNum;
            Position = waypointPos;
            Speed = speed;
            DrivingFlag = drivingFlag;
            Blip = waypointBlip;
            _enableWaypointMarker = true;
        }

        public Waypoint(int path, int waypointNum, Vector3 waypointPos, float speed, VehicleDrivingFlags drivingFlag, Blip waypointBlip, bool collector, float collectorRadius, float speedZoneRadius, uint yieldZone)
        {
            Path = path;
            Number = waypointNum;
            Position = waypointPos;
            Speed = speed;
            DrivingFlag = drivingFlag;
            Blip = waypointBlip;
            IsCollector = collector;
            CollectorRadius = collectorRadius;
            SpeedZoneRadius = speedZoneRadius;
            YieldZone = yieldZone;
            CollectorRadiusBlip = new Blip(waypointBlip.Position, collectorRadius)
            {
                Color = waypointBlip.Color,
                Alpha = 0.5f
            };
            _enableWaypointMarker = true;
        }

        public void UpdateWaypoint(Waypoint currentWaypoint, VehicleDrivingFlags drivingFlag, float drivingSpeed, bool collectorWaypointChecked, float collectorRadius, float speedZoneRadius, bool updateWaypointPositionChecked)
        {
            UpdateDrivingFlag(drivingFlag);
            UpdateWaypointSpeed(drivingSpeed);
            UpdateCollectorOptions(currentWaypoint, drivingSpeed, collectorWaypointChecked, collectorRadius, speedZoneRadius);
            if (updateWaypointPositionChecked)
            {
                UpdateWaypointPosition(Game.LocalPlayer.Character.Position);
            }
        }

        private void UpdateCollectorOptions(Waypoint currentWaypoint, float drivingSpeed, bool collectorWaypointChecked, float collectorRadius, float speedZoneRadius)
        {
            if (collectorWaypointChecked)
            {
                IsCollector = true;
                World.RemoveSpeedZone(YieldZone);
                YieldZone = World.AddSpeedZone(Game.LocalPlayer.Character.Position, SpeedZoneRadius, drivingSpeed);
                Blip.Color = Color.Blue;
                if (CollectorRadiusBlip)
                {
                    currentWaypoint.CollectorRadiusBlip.Position = Game.LocalPlayer.Character.Position;
                    currentWaypoint.CollectorRadiusBlip.Alpha = 0.5f;
                    currentWaypoint.CollectorRadiusBlip.Scale = collectorRadius * 0.5f;
                }
                else
                {
                    CollectorRadiusBlip = new Blip(Blip.Position)
                    {
                        Color = Blip.Color,
                        Alpha = 0.5f
                    };
                }
                CollectorRadius = collectorRadius;
                SpeedZoneRadius = speedZoneRadius;
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
            if (newDrivingFlag == VehicleDrivingFlags.StopAtDestination)
            {
                Blip.Color = Color.Red;
            }
            else
            {
                Blip.Color = Color.Green;
            }
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

        public void EnableWaypointMarker(bool enableMarker)
        {
            // I think this is how we allow re-drawing the markers during waypoint edit
            // Disable the individual waypoint's marker while the editWaypoint menu is open
            // Redraw the marker with a separate function using the editWaypoint menu's options
            _enableWaypointMarker = enableMarker;
        }

        public void DrawWaypointMarker()
        {
            if (_enableWaypointMarker)
            {
                if (IsCollector && CollectorRadius > 0)
                {
                    Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, CollectorRadius, CollectorRadius, 1f, 80, 130, 255, 100, false, false, 2, false, 0, 0, false);
                    if (SpeedZoneRadius > 0)
                    {
                        Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, SpeedZoneRadius, SpeedZoneRadius, 1f, 255, 185, 80, 100, false, false, 2, false, 0, 0, false);
                    }
                }
                else if (DrivingFlag == VehicleDrivingFlags.StopAtDestination)
                {
                    Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 255, 65, 65, 100, false, false, 2, false, 0, 0, false);
                }
                else
                {
                    Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 65, 255, 65, 100, false, false, 2, false, 0, 0, false);
                }
            }
        }

        public void DrawMarkerForEditWaypoint()
        {
            _enableWaypointMarker = false;
            GameFiber.StartNew(() =>
            {
                while (SettingsMenu.debugGraphics.Checked && !_enableWaypointMarker)
                {
                    if (EditWaypointMenu.editWaypointMenu.Visible)
                    {
                        if (EditWaypointMenu.collectorWaypoint.Checked)
                        {
                            if (EditWaypointMenu.updateWaypointPosition.Checked)
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.changeCollectorRadius.Value, (float)EditWaypointMenu.changeCollectorRadius.Value, 1f, 80, 130, 255, 80, false, false, 2, false, 0, 0, false);
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.changeSpeedZoneRadius.Value, (float)EditWaypointMenu.changeSpeedZoneRadius.Value, 1f, 255, 185, 80, 80, false, false, 2, false, 0, 0, false);

                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.changeCollectorRadius.Value, (float)EditWaypointMenu.changeCollectorRadius.Value, 1f, 80, 130, 255, 80, false, false, 2, false, 0, 0, false);
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.changeSpeedZoneRadius.Value, (float)EditWaypointMenu.changeSpeedZoneRadius.Value, 1f, 255, 185, 80, 80, false, false, 2, false, 0, 0, false);
                            }
                            else
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.changeCollectorRadius.Value, (float)EditWaypointMenu.changeCollectorRadius.Value, 1f, 80, 130, 255, 80, false, false, 2, false, 0, 0, false);
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.changeSpeedZoneRadius.Value, (float)EditWaypointMenu.changeSpeedZoneRadius.Value, 1f, 255, 185, 80, 80, false, false, 2, false, 0, 0, false);
                            }
                        }
                        else if (EditWaypointMenu.changeWaypointType.SelectedItem == "Drive To")
                        {
                            if (EditWaypointMenu.updateWaypointPosition.Checked)
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 65, 255, 65, 80, false, false, 2, false, 0, 0, false);
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 65, 255, 65, 80, false, false, 2, false, 0, 0, false);
                            }
                            else
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 65, 255, 65, 80, false, false, 2, false, 0, 0, false);
                            }
                            
                        }
                        else
                        {
                            if (EditWaypointMenu.updateWaypointPosition.Checked)
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 255, 65, 65, 80, false, false, 2, false, 0, 0, false);
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 255, 65, 65, 80, false, false, 2, false, 0, 0, false);
                            }
                            else
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 255, 65, 65, 80, false, false, 2, false, 0, 0, false);
                            }
                        }
                    }
                    else
                    {
                        _enableWaypointMarker = true;
                    }
                    GameFiber.Yield();
                }
            });
        }
    }
}
