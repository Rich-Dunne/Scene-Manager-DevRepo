using Rage;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SceneManager
{
    public class Path
    {
        internal int Number { get; set; }
        internal bool IsEnabled { get; set; }
        internal State State { get; set; }
        internal List<Waypoint> Waypoints = new List<Waypoint>();
        internal List<CollectedVehicle> CollectedVehicles = new List<CollectedVehicle>();

        internal Path(int pathNum, State pathState)
        {
            Number = pathNum;
            State = pathState;
            DrawLinesBetweenWaypoints();
        }

        private void LowerWaypointBlipsOpacity()
        {
            foreach (Waypoint wp in Waypoints)
            {
                wp.Blip.Alpha = 0.5f;
                if (wp.CollectorRadiusBlip)
                {
                    wp.CollectorRadiusBlip.Alpha = 0.25f;
                }
            }
        }

        private void RestoreWaypointBlipsOpacity()
        {
            foreach (Waypoint wp in Waypoints)
            {
                if (wp.Blip)
                {
                    wp.Blip.Alpha = 1.0f;
                    if (wp.CollectorRadiusBlip)
                    {
                        wp.CollectorRadiusBlip.Alpha = 0.5f;
                    }
                }
            }
        }

        internal void DisablePath()
        {
            IsEnabled = false;
            foreach(Waypoint wp in Waypoints)
            {
                wp.RemoveSpeedZone();
            }
            if (SettingsMenu.mapBlips.Checked)
            {
                LowerWaypointBlipsOpacity();
            }
        }

        internal void EnablePath()
        {
            IsEnabled = true;
            foreach (Waypoint wp in Waypoints)
            {
                if (wp.IsCollector)
                {
                    wp.AddSpeedZone();
                }
            }
            if (SettingsMenu.mapBlips.Checked)
            {
                RestoreWaypointBlipsOpacity();
            }
        }

        internal void DrawLinesBetweenWaypoints()
        {
            GameFiber.StartNew(() =>
            {
                while (SettingsMenu.threeDWaypoints.Checked)
                {
                    if (MenuManager.menuPool.IsAnyMenuOpen())
                    {
                        for (int i = 0; i < Waypoints.Count; i++)
                        {
                            if (i != Waypoints.Count - 1)
                            {
                                if (Waypoints[i + 1].IsStopWaypoint)
                                {
                                    Debug.DrawLine(Waypoints[i].Position, Waypoints[i + 1].Position, Color.Orange);
                                }
                                else
                                {
                                    Debug.DrawLine(Waypoints[i].Position, Waypoints[i + 1].Position, Color.Green);
                                }
                            }
                        }
                    }
                    GameFiber.Yield();
                }
            });
        }

        internal void LoopForVehiclesToBeDismissed()
        {
            GameFiber.StartNew(() =>
            {
                while (PathMainMenu.paths.Contains(this))
                {
                    //Logger.Log($"Dismissing unused vehicles for cleanup");
                    foreach (CollectedVehicle cv in CollectedVehicles.Where(cv => cv.Vehicle))
                    {
                        if (!cv.Vehicle.IsDriveable || cv.Vehicle.IsUpsideDown || !cv.Vehicle.HasDriver)
                        {
                            if (cv.Vehicle.HasDriver)
                            {
                                cv.Vehicle.Driver.Dismiss();
                            }
                            cv.Vehicle.Dismiss();
                        }
                    }

                    CollectedVehicles.RemoveAll(cv => !cv.Vehicle);
                    GameFiber.Sleep(60000);
                }
            });
        }

        internal void LoopWaypointCollection()
        {
            uint lastProcessTime = Game.GameTime; // Store the last time the full loop finished; this is a value in ms
            int timeBetweenChecks = 1000; // How many ms to wait between outer loops
            int yieldAfterChecks = 50; // How many calculations to do before yielding
            while (PathMainMenu.paths.Contains(this))
            {
                if (IsEnabled)
                {
                    int checksDone = 0;
                    try
                    {
                        foreach (Waypoint waypoint in Waypoints)
                        {
                            if (waypoint != null & waypoint.IsCollector)
                            {
                                foreach (Vehicle v in World.GetAllVehicles())
                                {
                                    if (IsNearWaypoint(v, waypoint) && IsValidForCollection(v))
                                    {
                                        CollectedVehicle newCollectedVehicle = AddVehicleToCollection(v);
                                        GameFiber AssignTasksFiber = new GameFiber(() => AITasking.AssignWaypointTasks(newCollectedVehicle, this, waypoint));
                                        AssignTasksFiber.Start();
                                    }

                                    checksDone++; // Increment the counter inside the vehicle loop
                                    if (checksDone % yieldAfterChecks == 0)
                                    {
                                        GameFiber.Yield(); // Yield the game fiber after the specified number of vehicles have been checked
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        //return;
                    }
                }
                GameFiber.Sleep((int)Math.Max(1, Game.GameTime - lastProcessTime)); // If the prior lines took more than a second to run, then you'll run again almost immediately, but if they ran fairly quickly, you can sleep the loop until the remainder of the time between checks has passed
                lastProcessTime = Game.GameTime;
            }

            CollectedVehicle AddVehicleToCollection(Vehicle vehicle)
            {
                var collectedVehicle = new CollectedVehicle(vehicle, this);
                CollectedVehicles.Add(collectedVehicle);
                Logger.Log($"Added {vehicle.Model.Name} to collection from path {Number} waypoint {1}.");
                return collectedVehicle;
            }

            bool IsNearWaypoint(Vehicle v, Waypoint wp)
            {
                return v.FrontPosition.DistanceTo2D(wp.Position) <= wp.CollectorRadius && Math.Abs(wp.Position.Z - v.Position.Z) < 3;
            }

            bool IsValidForCollection(Vehicle v)
            {
                if (v && v != Game.LocalPlayer.Character.CurrentVehicle && v != Game.LocalPlayer.Character.LastVehicle && (v.IsCar || v.IsBike || v.IsBicycle || v.IsQuadBike) && !v.IsSirenOn && v.IsEngineOn && v.IsOnAllWheels && v.Speed > 1 && !CollectedVehicles.Any(cv => cv?.Vehicle == v))
                {
                    var vehicleCollectedOnAnotherPath = PathMainMenu.paths.Any(p => p.Number != Number && p.CollectedVehicles.Any(cv => cv.Vehicle == v));
                    if (vehicleCollectedOnAnotherPath)
                    {
                        return false;
                    }
                    if (v.HasDriver && v.Driver && !v.Driver.IsAlive)
                    {
                        return false;
                    }
                    if (!v.HasDriver)
                    {
                        v.CreateRandomDriver();
                        while (!v.HasDriver)
                        {
                            GameFiber.Yield();
                        }
                        if (v && v.Driver)
                        {
                            v.Driver.IsPersistent = true;
                            v.Driver.BlockPermanentEvents = true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
