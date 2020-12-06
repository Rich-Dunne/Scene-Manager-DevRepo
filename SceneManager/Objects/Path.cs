using Rage;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Serialization;
using SceneManager.Utils;
using System.IO;

namespace SceneManager.Objects
{
    internal class Path // Change this to Public for import/export
    {
        internal int Number { get; set; }
        internal bool IsEnabled { get; set; }
        internal State State { get; set; }

        [XmlArray("Waypoints")]
        [XmlArrayItem("Waypoint")]
        public List<Waypoint> Waypoints { get; set; } = new List<Waypoint>();

        internal List<CollectedVehicle> CollectedVehicles = new List<CollectedVehicle>();
        private List<Vehicle> _blacklistedVehicles = new List<Vehicle>();

        private Path() { }

        internal Path(int pathNum, State pathState)
        {
            Number = pathNum;
            State = pathState;
            DrawLinesBetweenWaypoints();
        }

        internal void Save(string filename)
        {
            var GAME_DIRECTORY = Directory.GetCurrentDirectory();
            var SAVED_PATHS_DIRECTORY = GAME_DIRECTORY + "/plugins/SceneManager/Saved Paths/";
            if (!Directory.Exists(SAVED_PATHS_DIRECTORY))
            {
                Directory.CreateDirectory(SAVED_PATHS_DIRECTORY);
                Game.LogTrivial($"New directory created at '/plugins/SceneManager/Saved Paths'");
            }
            PathXMLManager.SaveItemToXML(this, SAVED_PATHS_DIRECTORY + filename);
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
                while(true)
                {
                    if (SettingsMenu.threeDWaypoints.Checked && (State == State.Finished && MenuManager.menuPool.IsAnyMenuOpen()) || (State == State.Creating && PathCreationMenu.pathCreationMenu.Visible))
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
                    _blacklistedVehicles.RemoveAll(v => !v);
                    GameFiber.Sleep(60000);
                }
            });
        }

        internal void LoopWaypointCollection()
        {
            uint lastProcessTime = Game.GameTime; // Store the last time the full loop finished; this is a value in ms
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
                                    if (VehicleIsNearWaypoint(v, waypoint) && VehicleIsValidForCollection(v))
                                    {
                                        CollectedVehicle newCollectedVehicle = AddVehicleToCollection(v);
                                        GameFiber AssignTasksFiber = new GameFiber(() => newCollectedVehicle.AssignWaypointTasks(this, waypoint));
                                        //GameFiber AssignTasksFiber = new GameFiber(() => AITasking.AssignWaypointTasks(newCollectedVehicle, this, waypoint));
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
                Game.LogTrivial($"Added {vehicle.Model.Name} to collection from path {Number} waypoint {1}.");
                return collectedVehicle;
            }

            bool VehicleIsNearWaypoint(Vehicle v, Waypoint wp)
            {
                return v.FrontPosition.DistanceTo2D(wp.Position) <= wp.CollectorRadius && Math.Abs(wp.Position.Z - v.Position.Z) < 3;
            }

            bool VehicleIsValidForCollection(Vehicle v)
            {
                if (v && v != Game.LocalPlayer.Character.LastVehicle && (v.IsCar || v.IsBike || v.IsBicycle || v.IsQuadBike) && !v.IsSirenOn && v.IsEngineOn && v.IsOnAllWheels && v.Speed > 1 && !CollectedVehicles.Any(cv => cv?.Vehicle == v) && !_blacklistedVehicles.Contains(v))
                {
                    var vehicleCollectedOnAnotherPath = PathMainMenu.paths.Any(p => p.Number != Number && p.CollectedVehicles.Any(cv => cv.Vehicle == v));
                    if (vehicleCollectedOnAnotherPath)
                    {
                        return false;
                    }
                    if (v.HasDriver && v.Driver)
                    {
                        if(!v.Driver.IsAlive)
                        {
                            Game.LogTrivial($"Vehicle's driver is dead.");
                            _blacklistedVehicles.Add(v);
                            return false;
                        }
                        if (v.IsPoliceVehicle && !v.Driver.IsAmbient())
                        {
                            _blacklistedVehicles.Add(v);
                            return false;
                        }
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
