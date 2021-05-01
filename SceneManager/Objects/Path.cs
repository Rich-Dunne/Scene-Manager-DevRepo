using Rage;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Serialization;
using SceneManager.Utils;
using SceneManager.Menus;
using System.IO;

namespace SceneManager.Objects
{
    [XmlRoot(ElementName = "Path", Namespace = "")]
    public class Path // Change this to Public for import/export
    {
        internal string Name { get; set; }
        internal int Number { get; set; }
        internal bool IsEnabled { get; set; }
        internal State State { get; set; }

        [XmlArray("Waypoints")]
        [XmlArrayItem("Waypoint")]
        public List<Waypoint> Waypoints { get; set; } = new List<Waypoint>();
        internal List<CollectedPed> CollectedPeds { get; } = new List<CollectedPed>();
        private List<Vehicle> BlacklistedVehicles { get; } = new List<Vehicle>();

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

        internal void Load()
        {
            State = State.Finished;
            EnablePath();
            foreach(Waypoint waypoint in Waypoints)
            {
                waypoint.LoadFromImport(this);
            }
            DrawLinesBetweenWaypoints();
            PathManager.EndPath(this);
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
            if (SettingsMenu.MapBlips.Checked)
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
            if (SettingsMenu.MapBlips.Checked)
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
                    if (SettingsMenu.ThreeDWaypoints.Checked && (State == State.Finished && MenuManager.MenuPool.IsAnyMenuOpen()) || (State == State.Creating && PathCreationMenu.Menu.Visible))
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
            }, "3D Waypoint Line Drawing Fiber");
        }

        internal void LoopForVehiclesToBeDismissed()
        {
            while (PathManager.Paths.Contains(this))
            {
                foreach (CollectedPed cp in CollectedPeds.Where(x => x && x.CurrentVehicle && (!x.CurrentVehicle.IsDriveable || x.CurrentVehicle.IsUpsideDown || !x.CurrentVehicle.HasDriver)))
                {
                    if (cp.CurrentVehicle.HasDriver)
                    {
                        cp.CurrentVehicle.Driver.Dismiss();
                    }
                    cp.CurrentVehicle.Dismiss();
                }

                CollectedPeds.RemoveAll(cp => !cp || !cp.CurrentVehicle);
                BlacklistedVehicles.RemoveAll(v => !v);
                GameFiber.Sleep(60000);
            }
        }

        internal void LoopWaypointCollection()
        {
            uint lastProcessTime = Game.GameTime; // Store the last time the full loop finished; this is a value in ms
            int yieldAfterChecks = 50; // How many calculations to do before yielding
            while (PathManager.Paths.Contains(this))
            {
                if (IsEnabled)
                {
                    int checksDone = 0;
                    try
                    {
                        foreach (Waypoint waypoint in Waypoints.Where(x => x != null && x.IsCollector))
                        {
                            foreach (Vehicle vehicle in World.GetAllVehicles())
                            {
                                if (VehicleIsValidForCollection(vehicle) && VehicleIsNearWaypoint(vehicle, waypoint))
                                {
                                    CollectedPeds.Add(new CollectedPed(vehicle.Driver, this, waypoint));
                                }

                                checksDone++; // Increment the counter inside the vehicle loop
                                if (checksDone % yieldAfterChecks == 0)
                                {
                                    GameFiber.Yield(); // Yield the game fiber after the specified number of vehicles have been checked
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Game.LogTrivial($"Vehicle collection error: {ex}");
                    }
                }
                GameFiber.Sleep((int)Math.Max(1, Game.GameTime - lastProcessTime)); // If the prior lines took more than a second to run, then you'll run again almost immediately, but if they ran fairly quickly, you can sleep the loop until the remainder of the time between checks has passed
                lastProcessTime = Game.GameTime;
            }

            bool VehicleIsNearWaypoint(Vehicle v, Waypoint wp)
            {
                return v.FrontPosition.DistanceTo2D(wp.Position) <= wp.CollectorRadius && Math.Abs(wp.Position.Z - v.Position.Z) < 3;
            }

            bool VehicleIsValidForCollection(Vehicle v)
            {
                if(!v)
                {
                    return false;
                }
                if (v != Game.LocalPlayer.Character.LastVehicle && (v.IsCar || v.IsBike || v.IsBicycle || v.IsQuadBike) && !v.IsSirenOn && v.IsEngineOn && v.IsOnAllWheels && v.Speed > 1 && !CollectedPeds.Any(cp => cp && cp.CurrentVehicle == v) && !BlacklistedVehicles.Contains(v))
                {
                    var vehicleCollectedOnAnotherPath = PathManager.Paths.Any(p => p.Number != Number && p.CollectedPeds.Any(cp => cp && cp.CurrentVehicle == v));
                    if (vehicleCollectedOnAnotherPath)
                    {
                        return false;
                    }
                    if (v.HasDriver && v.Driver)
                    {
                        if(!v.Driver.IsAlive)
                        {
                            Game.LogTrivial($"Vehicle's driver is dead.");
                            BlacklistedVehicles.Add(v);
                            return false;
                        }
                        if (v.IsPoliceVehicle && !v.Driver.IsAmbient())
                        {
                            Game.LogTrivial($"Vehicle's driver not ambient.");
                            BlacklistedVehicles.Add(v);
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

        internal void Delete()
        {
            DismissCollectedDrivers();
            RemoveWaypoints();
            Game.LogTrivial($"Path {Number} deleted.");
        }

        private void DismissCollectedDrivers()
        {
            List<CollectedPed> collectedPedsCopy = CollectedPeds.ToList(); // Have to enumerate over a copied list because you can't delete from the same list you're enumerating through
            foreach (CollectedPed collectedPed in collectedPedsCopy.Where(x => x != null && x && x.CurrentVehicle))
            {
                if (collectedPed.StoppedAtWaypoint)
                {
                    Rage.Native.NativeFunction.Natives.x260BE8F09E326A20(collectedPed.CurrentVehicle, 1f, 1, true);
                }
                if (collectedPed.GetAttachedBlip())
                {
                    collectedPed.GetAttachedBlip().Delete();
                }
                collectedPed.Dismiss();
                collectedPed.CurrentVehicle.IsSirenOn = false;
                collectedPed.CurrentVehicle.IsSirenSilent = true;
                collectedPed.CurrentVehicle.Dismiss();

                CollectedPeds.Remove(collectedPed);
            }
        }

        private void RemoveWaypoints()
        {
            Waypoints.ForEach(x => x.Delete());
            Waypoints.Clear();
        }
    }
}
