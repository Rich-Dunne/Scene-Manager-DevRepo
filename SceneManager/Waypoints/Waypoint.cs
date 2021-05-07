using Rage;
using System.Drawing;
using System.Linq;
using SceneManager.Utils;
using SceneManager.Menus;
using SceneManager.Managers;
using SceneManager.Paths;
using SceneManager.CollectedPeds;

namespace SceneManager.Waypoints
{
    public class Waypoint // Change this and select properties to Public for import/export
    {
        internal Path Path { get; set; }
        public int Number { get; set; }
        public Vector3 Position { get; set; }
        public float Speed { get; set; }
        public DrivingFlagType DrivingFlagType { get; set; }
        public bool IsStopWaypoint { get; set; }
        internal Blip Blip { get; private set; }
        public bool IsCollector { get; set; }
        public float CollectorRadius { get; set; }
        internal Blip CollectorRadiusBlip { get; set; }
        public float SpeedZoneRadius { get; set; }
        internal uint SpeedZone { get; set; }
        internal bool EnableWaypointMarker { get; set; } = true;

        private Waypoint() { }

        internal Waypoint(Path path, int waypointNumber, Vector3 waypointPosition, float speed, DrivingFlagType drivingFlag, bool stopWaypoint, bool collector = false, float collectorRadius = 1, float speedZoneRadius = 5)
        {
            Path = path;
            Number = waypointNumber;
            Position = waypointPosition;
            Speed = speed;
            DrivingFlagType = drivingFlag;
            IsStopWaypoint = stopWaypoint;
            IsCollector = collector;
            CollectorRadius = collectorRadius;
            SpeedZoneRadius = speedZoneRadius;
            CreateBlip();
            if (collector)
            {
                AddSpeedZone();
                CollectorRadiusBlip = new Blip(Blip.Position, collectorRadius)
                {
                    Color = Blip.Color,
                };
                if (SettingsMenu.MapBlips.Checked)
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
                    foreach(CollectedPed cp in Path.CollectedPeds.Where(cp => cp.CurrentVehicle && cp.Path == Path && cp.CurrentWaypoint == this && cp.StoppedAtWaypoint))
                    {
                        cp.Dismiss(Dismiss.FromWaypoint);
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
                    Blip.Position = newWaypointPosition;
                }
                if (CollectorRadiusBlip)
                {
                    CollectorRadiusBlip.Position = newWaypointPosition;
                }
            }
        }

        internal void AddSpeedZone() => SpeedZone = World.AddSpeedZone(Position, SpeedZoneRadius, Speed);

        internal void RemoveSpeedZone() => World.RemoveSpeedZone(SpeedZone);

        internal void DrawWaypointMarker()
        {
            // This is called once when the waypoint is created
            GameFiber.StartNew(() =>
            {
                while (true)
                {
                    if(SettingsMenu.ThreeDWaypoints.Checked && EnableWaypointMarker && Path.Waypoints.Contains(this))
                    {
                        if (EditWaypointMenu.Menu.Visible && PathMainMenu.EditPath.OptionText == Path.Name && EditWaypointMenu.EditWaypoint.Value == Number)
                        {
                            if (EditWaypointMenu.CollectorWaypoint.Checked)
                            {
                                if (EditWaypointMenu.UpdateWaypointPosition.Checked)
                                {
                                    Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, GetMousePositionInWorld(), 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.ChangeCollectorRadius.Value * 2, (float)EditWaypointMenu.ChangeCollectorRadius.Value * 2, 1f, 80, 130, 255, 100, false, false, 2, false, 0, 0, false);
                                    Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, GetMousePositionInWorld(), 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.ChangeSpeedZoneRadius.Value * 2, (float)EditWaypointMenu.ChangeSpeedZoneRadius.Value * 2, 1f, 255, 185, 80, 100, false, false, 2, false, 0, 0, false);
                                }
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position, 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.ChangeCollectorRadius.Value * 2, (float)EditWaypointMenu.ChangeCollectorRadius.Value * 2, 2f, 80, 130, 255, 100, false, false, 2, false, 0, 0, false);
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position, 0, 0, 0, 0, 0, 0, (float)EditWaypointMenu.ChangeSpeedZoneRadius.Value * 2, (float)EditWaypointMenu.ChangeSpeedZoneRadius.Value * 2, 2f, 255, 185, 80, 100, false, false, 2, false, 0, 0, false);
                            }
                            else if (EditWaypointMenu.StopWaypointType.Checked)
                            {
                                if (EditWaypointMenu.UpdateWaypointPosition.Checked)
                                {
                                    Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, GetMousePositionInWorld(), 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 255, 65, 65, 100, false, false, 2, false, 0, 0, false);
                                }
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position, 0, 0, 0, 0, 0, 0, 1f, 1f, 2f, 255, 65, 65, 100, false, false, 2, false, 0, 0, false);
                            }
                            else
                            {
                                if (EditWaypointMenu.UpdateWaypointPosition.Checked)
                                {
                                    Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, GetMousePositionInWorld(), 0, 0, 0, 0, 0, 0, 1f, 1f, 1f, 65, 255, 65, 100, false, false, 2, false, 0, 0, false);
                                }
                                Rage.Native.NativeFunction.Natives.DRAW_MARKER(1, Position, 0, 0, 0, 0, 0, 0, 1f, 1f, 2f, 65, 255, 65, 100, false, false, 2, false, 0, 0, false);
                            }
                        }
                        else if ((Path.State == State.Finished && MenuManager.MenuPool.IsAnyMenuOpen()) || (Path.State == State.Creating && PathCreationMenu.Menu.Visible))
                        {
                            float markerHeight = 1f;
                            if ((DriverMenu.DirectDriver.Selected && DriverMenu.DirectDriver.OptionText == Path.Name) || PathMainMenu.EditPath.Selected && PathMainMenu.EditPath.OptionText == Path.Name && (PathMainMenu.Menu.Visible || EditPathMenu.Menu.Visible))
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

        internal void Delete()
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

        internal void LoadFromImport(Path path)
        {
            Path = path;
            CreateBlip();
            Game.LogTrivial($"===== WAYPOINT DATA =====");
            Game.LogTrivial($"Path: {Path.Name}");
            Game.LogTrivial($"Number: {Number}");
            Game.LogTrivial($"Position: {Position}");
            Game.LogTrivial($"Speed: {Speed}");
            Game.LogTrivial($"DrivingFlag: {DrivingFlagType}");
            Game.LogTrivial($"Stop Waypoint: {IsStopWaypoint}");
            Game.LogTrivial($"Blip: {Blip}");
            Game.LogTrivial($"Collector: {IsCollector}");
            Game.LogTrivial($"Collector Radius: {CollectorRadius}");
            Game.LogTrivial($"SpeedZone Radius: {SpeedZoneRadius}");
            if (IsCollector)
            {
                CollectorRadiusBlip = new Blip(Position, CollectorRadius)
                {
                    Color = Blip.Color,
                };
                if (SettingsMenu.MapBlips.Checked)
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

        private void CreateBlip()
        {
            var spriteNumericalEnum = Path.Number + 16; // 16 because the numerical value of these sprites are always 16 more than the path index
            Blip = new Blip(Position)
            {
                Scale = 0.5f,
                Sprite = (BlipSprite)spriteNumericalEnum
            };

            if (IsCollector)
            {
                Blip.Color = Color.Blue;
            }
            else if (IsStopWaypoint)
            {
                Blip.Color = Color.Red;
            }
            else
            {
                Blip.Color = Color.Green;
            }

            if (!SettingsMenu.MapBlips.Checked)
            {
                Blip.Alpha = 0f;
            }

            if (!Path.IsEnabled)
            {
                Blip.Alpha = 0.5f;
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
