using Rage;
using System.Drawing;
using System.Linq;

namespace SceneManager
{
    public class Waypoint
    {
        internal Path Path { get; set; }
        internal int Number { get; set; }
        internal Vector3 Position { get; set; }
        internal float Speed { get; set; }
        internal VehicleDrivingFlags DrivingFlag { get; set; }
        internal Blip Blip { get; }
        internal bool IsCollector { get; set; }
        internal float CollectorRadius { get; set; }
        internal Blip CollectorRadiusBlip { get; set; }
        internal float SpeedZoneRadius { get; set; }
        internal uint SpeedZone { get; set; }
        internal bool EnableWaypointMarker { get; set; } = true;
        internal bool EnableEditMarker { get; set; }

        internal Waypoint(Path path, int waypointNum, Vector3 waypointPos, float speed, VehicleDrivingFlags drivingFlag, Blip waypointBlip, bool collector = false, float collectorRadius = 1, float speedZoneRadius = 5)
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

        internal void UpdateWaypoint(Waypoint currentWaypoint, VehicleDrivingFlags drivingFlag, float speed, bool collectorWaypointChecked, float collectorRadius, float speedZoneRadius, bool updateWaypointPositionChecked)
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
                    foreach(CollectedVehicle cv in VehicleCollector.collectedVehicles.Where(cv => cv.Vehicle && cv.Path == Path && cv.CurrentWaypoint == this && cv.StoppedAtWaypoint))
                    {
                        Logger.Log($"Setting StoppedAtWaypoint to false for {cv.Vehicle.Model.Name}");
                        cv.Dismiss(DismissOption.FromWaypoint);
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
                        CollectorRadiusBlip.Alpha = 0.5f;
                        CollectorRadiusBlip.Scale = collectorRadius;
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
                if (Blip)
                {
                    Blip.Position = Game.LocalPlayer.Character.Position;
                }
                if (CollectorRadiusBlip)
                {
                    CollectorRadiusBlip.Position = Game.LocalPlayer.Character.Position;
                }
            }
        }

        internal void AddSpeedZone()
        {
            SpeedZone = World.AddSpeedZone(Position, SpeedZoneRadius, Speed);
        }

        internal void RemoveSpeedZone()
        {
            World.RemoveSpeedZone(SpeedZone);
        }

        internal void DrawWaypointMarker()
        {
            // This is called once when the waypoint is created
            GameFiber.StartNew(() =>
            {
                while (SettingsMenu.threeDWaypoints.Checked && EnableWaypointMarker && Path.Waypoints.Contains(this))
                {
                    if (EditWaypointMenu.editWaypointMenu.Visible && PathMainMenu.editPath.Value == Path.Number && EditWaypointMenu.editWaypoint.Value == Number)
                    {
                        if (EditWaypointMenu.collectorWaypoint.Checked)
                        {
                            if (EditWaypointMenu.updateWaypointPosition.Checked)
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.changeCollectorRadius.Value * 2, (float)EditWaypointMenu.changeCollectorRadius.Value * 2, 1f, 80, 130, 255, 100, false, false, 2, false, 0, 0, false);
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.changeSpeedZoneRadius.Value * 2, (float)EditWaypointMenu.changeSpeedZoneRadius.Value * 2, 1f, 255, 185, 80, 100, false, false, 2, false, 0, 0, false);
                            }
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.changeCollectorRadius.Value * 2, (float)EditWaypointMenu.changeCollectorRadius.Value * 2, 2f, 80, 130, 255, 100, false, false, 2, false, 0, 0, false);
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.changeSpeedZoneRadius.Value * 2, (float)EditWaypointMenu.changeSpeedZoneRadius.Value * 2, 2f, 255, 185, 80, 100, false, false, 2, false, 0, 0, false);
                        }
                        else if (EditWaypointMenu.changeWaypointType.SelectedItem == "Stop")
                        {
                            if (EditWaypointMenu.updateWaypointPosition.Checked)
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 255, 65, 65, 100, false, false, 2, false, 0, 0, false);
                            }
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 2f, 255, 65, 65, 100, false, false, 2, false, 0, 0, false);
                        }
                        else
                        {
                            if (EditWaypointMenu.updateWaypointPosition.Checked)
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 65, 255, 65, 100, false, false, 2, false, 0, 0, false);
                            }
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, 2f, 65, 255, 65, 100, false, false, 2, false, 0, 0, false);
                        }
                    }
                    else if ((Path.State == State.Finished && MenuManager.menuPool.IsAnyMenuOpen()) || (Path.State == State.Creating && PathCreationMenu.pathCreationMenu.Visible))
                    {
                        float markerHeight = 1f;
                        if (PathMainMenu.editPath.Selected && PathMainMenu.editPath.Value == Path.Number)
                        {
                            markerHeight = 2f;
                        }
                        if (IsCollector)
                        {
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, CollectorRadius * 2, CollectorRadius * 2, markerHeight, 80, 130, 255, 100, false, false, 2, false, 0, 0, false);
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, SpeedZoneRadius * 2, SpeedZoneRadius * 2, markerHeight, 255, 185, 80, 100, false, false, 2, false, 0, 0, false);
                        }
                        else if (DrivingFlag == VehicleDrivingFlags.StopAtDestination)
                        {
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, markerHeight, 255, 65, 65, 100, false, false, 2, false, 0, 0, false);
                        }
                        else
                        {
                            Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position.X, Position.Y, Position.Z - 1, 0, 0, 0, 0, 0, 0, 1f, 1f, markerHeight, 65, 255, 65, 100, false, false, 2, false, 0, 0, false);
                        }
                    }

                    GameFiber.Yield();
                }
            });
        }

        internal void EnableBlip()
        {
            if(!PathMainMenu.paths.Where(p => p == Path).First().IsEnabled)
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

        internal void DisableBlip()
        {
            Blip.Alpha = 0;
            CollectorRadiusBlip.Alpha = 0;
        }
    }
}
