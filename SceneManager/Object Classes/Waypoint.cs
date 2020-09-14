using Rage;
using System.Drawing;
using System.Linq;

namespace SceneManager
{
    public class Waypoint
    {
        public Path Path { get; set; }
        public int Number { get; set; }
        public Vector3 Position { get; set; }
        public float Speed { get; set; }
        public VehicleDrivingFlags DrivingFlag { get; set; }
        public Blip Blip { get; }
        public bool IsCollector { get; set; }
        public float CollectorRadius { get; set; }
        public Blip CollectorRadiusBlip { get; set; }
        public float SpeedZoneRadius { get; set; }
        public uint SpeedZone { get; set; }
        public bool EnableWaypointMarker { get; set; }
        public bool EnableEditMarker { get; set; }

        public Waypoint(Path path, int waypointNum, Vector3 waypointPos, float speed, VehicleDrivingFlags drivingFlag, Blip waypointBlip, bool collector = false, float collectorRadius = 1, float speedZoneRadius = 0)
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
            AddSpeedZone();
            CollectorRadiusBlip = new Blip(waypointBlip.Position, collectorRadius)
            {
                Color = waypointBlip.Color,
            };
            if (SettingsMenu.mapBlips.Checked)
            {
                CollectorRadiusBlip.Alpha = 0.5f;
            }
            else
            {
                CollectorRadiusBlip.Alpha = 0f;
            }
            DrawWaypointMarker();
        }

        public void UpdateWaypoint(Waypoint currentWaypoint, VehicleDrivingFlags drivingFlag, float speed, bool collectorWaypointChecked, float collectorRadius, float speedZoneRadius, bool updateWaypointPositionChecked)
        {
            UpdateDrivingFlag(drivingFlag);
            UpdateWaypointSpeed(speed);
            UpdateCollectorOptions();
            if (updateWaypointPositionChecked)
            {
                UpdateWaypointPosition(Game.LocalPlayer.Character.Position);
            }

            void UpdateDrivingFlag(VehicleDrivingFlags newDrivingFlag)
            {
                if(DrivingFlag == VehicleDrivingFlags.StopAtDestination && newDrivingFlag != VehicleDrivingFlags.StopAtDestination)
                {
                    foreach(CollectedVehicle cv in VehicleCollector.collectedVehicles.Where(cv => cv.Path == Path && cv.CurrentWaypoint == this && cv.StoppedAtWaypoint))
                    {
                        Logger.Log($"Setting StoppedAtWaypoint to false for {cv.Vehicle.Model.Name}");
                        cv.StoppedAtWaypoint = false;
                    }
                }
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

            void UpdateWaypointSpeed(float newWaypointSpeed)
            {
                Speed = newWaypointSpeed;
            }

            void UpdateCollectorOptions()
            {
                if (collectorWaypointChecked)
                {
                    IsCollector = true;
                    RemoveSpeedZone();
                    SpeedZone = World.AddSpeedZone(Game.LocalPlayer.Character.Position, SpeedZoneRadius, speed);
                    Blip.Color = Color.Blue;
                    if (CollectorRadiusBlip)
                    {
                        CollectorRadiusBlip.Position = Game.LocalPlayer.Character.Position;
                        CollectorRadiusBlip.Alpha = 0.5f;
                        CollectorRadiusBlip.Scale = collectorRadius * 0.5f;
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
                    RemoveSpeedZone();
                    if (CollectorRadiusBlip)
                    {
                        CollectorRadiusBlip.Delete();
                    }
                }
            }

            void UpdateWaypointPosition(Vector3 newWaypointPosition)
            {
                Position = newWaypointPosition;
                RemoveSpeedZone();
                AddSpeedZone();
                UpdateWaypointBlipPosition();
            }

            void UpdateWaypointBlipPosition()
            {
                Blip.Position = Game.LocalPlayer.Character.Position;
            }
        }

        public void AddSpeedZone()
        {
            SpeedZone = World.AddSpeedZone(Position, SpeedZoneRadius, Speed);
        }

        public void RemoveSpeedZone()
        {
            World.RemoveSpeedZone(SpeedZone);
        }

        public void DrawWaypointMarker()
        {
            // This is called once when the waypoint is created
            GameFiber.StartNew((System.Threading.ThreadStart)(() =>
            {
                while (SettingsMenu.threeDWaypoints.Checked && EnableWaypointMarker && Path.Waypoints.Contains(this))
                {
                    if (EditWaypointMenu.editWaypointMenu.Visible && EditWaypointMenu.editWaypoint.Value == Number)
                    {
                        if (EditWaypointMenu.collectorWaypoint.Checked)
                        {
                            if (EditWaypointMenu.updateWaypointPosition.Checked)
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.changeCollectorRadius.Value * 2, (float)EditWaypointMenu.changeCollectorRadius.Value * 2, 1f, 80, 130, 255, 80, false, false, 2, false, 0, 0, false);
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.changeSpeedZoneRadius.Value * 2, (float)EditWaypointMenu.changeSpeedZoneRadius.Value * 2, 1f, 255, 185, 80, 80, false, false, 2, false, 0, 0, false);
                            }
                            else
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.changeCollectorRadius.Value * 2, (float)EditWaypointMenu.changeCollectorRadius.Value * 2, 1f, 80, 130, 255, 100, false, false, 2, false, 0, 0, false);
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.changeSpeedZoneRadius.Value * 2, (float)EditWaypointMenu.changeSpeedZoneRadius.Value * 2, 1f, 255, 185, 80, 100, false, false, 2, false, 0, 0, false);
                            }
                        }
                        else if (EditWaypointMenu.changeWaypointType.SelectedItem == "Stop")
                        {
                            if (EditWaypointMenu.updateWaypointPosition.Checked)
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 255, 65, 65, 100, false, false, 2, false, 0, 0, false);
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 255, 65, 65, 100, false, false, 2, false, 0, 0, false);
                            }
                            else
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 255, 65, 65, 100, false, false, 2, false, 0, 0, false);
                            }

                        }
                        else
                        {
                            if (EditWaypointMenu.updateWaypointPosition.Checked)
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 65, 255, 65, 100, false, false, 2, false, 0, 0, false);
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 65, 255, 65, 100, false, false, 2, false, 0, 0, false);
                            }
                            else
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 65, 255, 65, 100, false, false, 2, false, 0, 0, false);
                            }
                        }
                    }
                    else if ((Path.State == State.Finished && MenuManager.menuPool.IsAnyMenuOpen()) || (Path.State == State.Creating && PathCreationMenu.pathCreationMenu.Visible))
                    {
                        if (IsCollector && CollectorRadius > 0)
                        {
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, CollectorRadius * 2, CollectorRadius * 2, 1f, 80, 130, 255, 80, false, false, 2, false, 0, 0, false);
                            if (SpeedZoneRadius > 0)
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, SpeedZoneRadius * 2, SpeedZoneRadius * 2, 1f, 255, 185, 80, 80, false, false, 2, false, 0, 0, false);
                            }
                        }
                        else if (DrivingFlag == VehicleDrivingFlags.StopAtDestination)
                        {
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 255, 65, 65, 80, false, false, 2, false, 0, 0, false);
                        }
                        else
                        {
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 65, 255, 65, 80, false, false, 2, false, 0, 0, false);
                        }
                    }

                    GameFiber.Yield();
                }
            }));
        }

        public void EnableBlip()
        {
            if(!PathMainMenu.GetPaths().Where(p => p == Path).First().IsEnabled)
            {
                Blip.Alpha = 0.5f;
                CollectorRadiusBlip.Alpha = 0.25f;
            }
            else
            {
                Blip.Alpha = 1.0f;
                CollectorRadiusBlip.Alpha = 0.5f;
            }

        }

        public void DisableBlip()
        {
            Blip.Alpha = 0;
            CollectorRadiusBlip.Alpha = 0;
        }
    }
}
