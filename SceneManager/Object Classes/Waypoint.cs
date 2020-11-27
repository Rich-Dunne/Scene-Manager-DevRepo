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
        internal DrivingFlagType DrivingFlagType { get; private set; }
        internal bool IsStopWaypoint { get; set; }
        internal Blip Blip { get; }
        internal bool IsCollector { get; set; }
        internal float CollectorRadius { get; set; }
        internal Blip CollectorRadiusBlip { get; set; }
        internal float SpeedZoneRadius { get; set; }
        internal uint SpeedZone { get; set; }
        internal bool EnableWaypointMarker { get; set; } = true;

        internal Waypoint(Path path, int waypointNum, Vector3 waypointPos, float speed, DrivingFlagType drivingFlag, bool stopWaypoint, Blip waypointBlip, bool collector = false, float collectorRadius = 1, float speedZoneRadius = 5)
        {
            Path = path;
            Number = waypointNum;
            Position = waypointPos;
            Speed = speed;
            DrivingFlagType = drivingFlag;
            IsStopWaypoint = stopWaypoint;
            Blip = waypointBlip;
            IsCollector = collector;
            CollectorRadius = collectorRadius;
            SpeedZoneRadius = speedZoneRadius;
            if (collector)
            {
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
            }
            DrawWaypointMarker();
        }

        internal void UpdateWaypoint(Waypoint currentWaypoint, Vector3 newWaypointPosition, DrivingFlagType drivingFlag, bool stopWaypoint, float speed, bool collectorWaypointChecked, float collectorRadius, float speedZoneRadius, bool updateWaypointPositionChecked)
        {
            if(IsStopWaypoint != stopWaypoint)
            {
                UpdateIfStopWaypoint();
            }
            DrivingFlagType = drivingFlag;
            UpdateWaypointSpeed(speed);
            UpdateCollectorOptions();
            if (updateWaypointPositionChecked)
            {
                UpdateWaypointPosition();
            }

            void UpdateIfStopWaypoint()
            {
                if (IsStopWaypoint && !stopWaypoint)
                {
                    Blip.Color = Color.Green;
                    foreach(CollectedVehicle cv in Path.CollectedVehicles.Where(cv => cv.Vehicle && cv.Path == Path && cv.CurrentWaypoint == this && cv.StoppedAtWaypoint))
                    {
                       // Logger.Log($"Setting StoppedAtWaypoint to false for {cv.Vehicle.Model.Name}");
                        cv.Dismiss(DismissOption.FromWaypoint);
                    }
                }
                else if(stopWaypoint && !IsStopWaypoint)
                {
                    Blip.Color = Color.Red;
                }
                IsStopWaypoint = stopWaypoint;
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
                    SpeedZone = World.AddSpeedZone(currentWaypoint.Position, speedZoneRadius, speed);
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
                    if (IsStopWaypoint)
                    {
                        Blip.Color = Color.Red;
                    }
                    else
                    {
                        Blip.Color = Color.Green;
                    }
                    RemoveSpeedZone();
                    if (CollectorRadiusBlip)
                    {
                        CollectorRadiusBlip.Delete();
                    }
                }
            }

            void UpdateWaypointPosition()
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

        internal void Remove()
        {
            if (Blip)
            {
                Blip.Delete();
            }
            if (CollectorRadiusBlip)
            {
                CollectorRadiusBlip.Delete();
            }
            RemoveSpeedZone();
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
                while (true)
                {
                    if(SettingsMenu.threeDWaypoints.Checked && EnableWaypointMarker && Path.Waypoints.Contains(this))
                    {
                        if (EditWaypointMenu.editWaypointMenu.Visible && PathMainMenu.editPath.Value == Path.Number && EditWaypointMenu.editWaypoint.Value == Number)
                        {
                            if (EditWaypointMenu.collectorWaypoint.Checked)
                            {
                                if (EditWaypointMenu.updateWaypointPosition.Checked)
                                {
                                    Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, GetMousePositionInWorld(), 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.changeCollectorRadius.Value * 2, (float)EditWaypointMenu.changeCollectorRadius.Value * 2, 1f, 80, 130, 255, 100, false, false, 2, false, 0, 0, false);
                                    Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, GetMousePositionInWorld(), 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.changeSpeedZoneRadius.Value * 2, (float)EditWaypointMenu.changeSpeedZoneRadius.Value * 2, 1f, 255, 185, 80, 100, false, false, 2, false, 0, 0, false);
                                }
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position, 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.changeCollectorRadius.Value * 2, (float)EditWaypointMenu.changeCollectorRadius.Value * 2, 2f, 80, 130, 255, 100, false, false, 2, false, 0, 0, false);
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position, 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.changeSpeedZoneRadius.Value * 2, (float)EditWaypointMenu.changeSpeedZoneRadius.Value * 2, 2f, 255, 185, 80, 100, false, false, 2, false, 0, 0, false);
                            }
                            else if (EditWaypointMenu.stopWaypointType.Checked)
                            {
                                if (EditWaypointMenu.updateWaypointPosition.Checked)
                                {
                                    Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, GetMousePositionInWorld(), 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 255, 65, 65, 100, false, false, 2, false, 0, 0, false);
                                }
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position, 0, 0, 0, 0, 0, 0, 1f, 1f, 2f, 255, 65, 65, 100, false, false, 2, false, 0, 0, false);
                            }
                            else
                            {
                                if (EditWaypointMenu.updateWaypointPosition.Checked)
                                {
                                    Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, GetMousePositionInWorld(), 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 65, 255, 65, 100, false, false, 2, false, 0, 0, false);
                                }
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position, 0, 0, 0, 0, 0, 0, 1f, 1f, 2f, 65, 255, 65, 100, false, false, 2, false, 0, 0, false);
                            }
                        }
                        else if ((Path.State == State.Finished && MenuManager.menuPool.IsAnyMenuOpen()) || (Path.State == State.Creating && PathCreationMenu.pathCreationMenu.Visible))
                        {
                            float markerHeight = 1f;
                            if ((PathMainMenu.directDriver.Selected && PathMainMenu.directDriver.Value == Path.Number) || PathMainMenu.editPath.Selected && PathMainMenu.editPath.Value == Path.Number && (PathMainMenu.pathMainMenu.Visible || EditPathMenu.editPathMenu.Visible))
                            {
                                markerHeight = 2f;
                            }
                            if (IsCollector)
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position, 0, 0, 0, 0, 0, 0, CollectorRadius * 2, CollectorRadius * 2, markerHeight, 80, 130, 255, 100, false, false, 2, false, 0, 0, false);
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position, 0, 0, 0, 0, 0, 0, SpeedZoneRadius * 2, SpeedZoneRadius * 2, markerHeight, 255, 185, 80, 100, false, false, 2, false, 0, 0, false);
                            }
                            else if (IsStopWaypoint)
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position, 0, 0, 0, 0, 0, 0, 1f, 1f, markerHeight, 255, 65, 65, 100, false, false, 2, false, 0, 0, false);
                            }
                            else
                            {
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position, 0, 0, 0, 0, 0, 0, 1f, 1f, markerHeight, 65, 255, 65, 100, false, false, 2, false, 0, 0, false);
                            }
                        }
                    }

                    GameFiber.Yield();
                }
            });
        }

        internal void EnableBlip()
        {
            if(!Path.IsEnabled)
            {
                if (Blip)
                {
                    Blip.Alpha = 0.5f;
                }
                if (CollectorRadiusBlip)
                {
                    CollectorRadiusBlip.Alpha = 0.25f;
                }
            }
            else
            {
                if (Blip)
                {
                    Blip.Alpha = 1.0f;
                }
                if (CollectorRadiusBlip)
                {
                    CollectorRadiusBlip.Alpha = 0.5f;
                }
            }

        }

        internal void DisableBlip()
        {
            if (Blip)
            {
                Blip.Alpha = 0;
            }
            if (CollectorRadiusBlip)
            {
                CollectorRadiusBlip.Alpha = 0;
            }
        }

        private static Vector3 GetMousePositionInWorld()
        {
            HitResult TracePlayerView(float maxTraceDistance = 100f, TraceFlags flags = TraceFlags.IntersectWorld) => TracePlayerView2(out Vector3 v1, out Vector3 v2, maxTraceDistance, flags);

            HitResult TracePlayerView2(out Vector3 start, out Vector3 end, float maxTraceDistance, TraceFlags flags)
            {
                Vector3 direction = GetPlayerLookingDirection(out start);
                end = start + (maxTraceDistance * direction);
                return World.TraceLine(start, end, flags);
            }

            Vector3 GetPlayerLookingDirection(out Vector3 camPosition)
            {
                if (Camera.RenderingCamera)
                {
                    camPosition = Camera.RenderingCamera.Position;
                    return Camera.RenderingCamera.Direction;
                }
                else
                {
                    float pitch = Rage.Native.NativeFunction.Natives.GET_GAMEPLAY_CAM_RELATIVE_PITCH<float>();
                    float heading = Rage.Native.NativeFunction.Natives.GET_GAMEPLAY_CAM_RELATIVE_HEADING<float>();

                    camPosition = Rage.Native.NativeFunction.Natives.GET_GAMEPLAY_CAM_COORD<Vector3>();
                    return (Game.LocalPlayer.Character.Rotation + new Rotator(pitch, 0, heading)).ToVector().ToNormalized();
                }
            }

            return TracePlayerView(100f, TraceFlags.IntersectWorld).HitPosition;
        }
    }
}
