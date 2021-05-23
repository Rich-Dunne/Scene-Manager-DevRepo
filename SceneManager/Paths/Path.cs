using Rage;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Serialization;
using SceneManager.Utils;
using SceneManager.Menus;
using System.IO;
using SceneManager.Managers;
using SceneManager.Barriers;
using SceneManager.Waypoints;
using SceneManager.CollectedPeds;

namespace SceneManager.Paths
{
    public class Path // Change this to Public for import/export
    {
        public string Name { get; set; }
        internal int Number { get => Array.IndexOf(PathManager.Paths, this) + 1; set { } }
        internal bool IsEnabled { get; set; }
        internal State State { get; set; }

        [XmlArray("Waypoints")]
        [XmlArrayItem("Waypoint")]
        public List<Waypoint> Waypoints { get; set; } = new List<Waypoint>();
        [XmlArray("Barriers")]
        [XmlArrayItem("Barrier")]
        public List<Barrier> Barriers { get; set; } = new List<Barrier>();
        internal List<CollectedPed> CollectedPeds { get; } = new List<CollectedPed>();
        internal List<Vehicle> BlacklistedVehicles { get; } = new List<Vehicle>();

        internal Path()
        {
            State = State.Creating;
            DrawLinesBetweenWaypoints();
        }

        internal void Save()
        {
            var GAME_DIRECTORY = Directory.GetCurrentDirectory();
            var SAVED_PATHS_DIRECTORY = GAME_DIRECTORY + "/plugins/SceneManager/Saved Paths/";
            if (!Directory.Exists(SAVED_PATHS_DIRECTORY))
            {
                Directory.CreateDirectory(SAVED_PATHS_DIRECTORY);
                Game.LogTrivial($"New directory created at '/plugins/SceneManager/Saved Paths'");
            }

            var overrides = DefineOverridesForCombinedPath();
            Serializer.SaveItemToXML(this, SAVED_PATHS_DIRECTORY + Name + ".xml", overrides);
            Game.LogTrivial($"Saved {Name}.xml");

            Game.DisplayNotification($"~o~Scene Manager ~g~[Success]\n~w~Path ~b~{Name} ~w~exported.");
        }

        internal void Load()
        {
            foreach(Waypoint waypoint in Waypoints)
            {
                waypoint.LoadFromImport(this);
            }

            Game.LogTrivial($"This path has {Barriers.Count} barriers");
            for(int i = 0; i < Barriers.Count(); i++)
            {
                var barrier = new Barrier(Barriers[i], Barriers[i].Invincible, Barriers[i].Immobile, Barriers[i].TextureVariation, Barriers[i].LightsEnabled);
                barrier.Path = this;
                Barriers[i] = barrier;
                BarrierManager.Barriers.Add(barrier);
            }

            DrawLinesBetweenWaypoints();
            Finish();
        }

        internal void AddWaypoint()
        {
            DrivingFlagType drivingFlag = PathCreationMenu.DirectWaypoint.Checked ? DrivingFlagType.Direct : DrivingFlagType.Normal;
            float speed = HelperMethods.ConvertDriveSpeedForWaypoint(PathCreationMenu.WaypointSpeed.Value);

            if (PathCreationMenu.CollectorWaypoint.Checked)
            {
                new Waypoint(this, UserInput.PlayerMousePosition, speed, drivingFlag, PathCreationMenu.StopWaypoint.Checked, true, PathCreationMenu.CollectorRadius.Value, PathCreationMenu.SpeedZoneRadius.Value);
            }
            else
            {
                new Waypoint(this, UserInput.PlayerMousePosition, speed, drivingFlag, PathCreationMenu.StopWaypoint.Checked);
            }
        }

        internal void RemoveWaypoint()
        {
            Waypoint lastWaypoint = Waypoints.Last();
            lastWaypoint.Delete();
            Waypoints.Remove(lastWaypoint);
        }

        internal void Finish()
        {
            Game.LogTrivial($"[Path Creation] Path {Name} finished with {Waypoints.Count} waypoints.");
            Game.DisplayNotification($"~o~Scene Manager ~g~[Success]\n~w~Path ~b~{Name} ~w~complete.");
            State = State.Finished;
            IsEnabled = true;
            Waypoints.ForEach(x => x.EnableBlip());
            GameFiber.StartNew(() => LoopForVehiclesToBeDismissed(), "Vehicle Cleanup Loop Fiber");
            GameFiber.StartNew(() => LoopWaypointCollection(), "Waypoint Collection Loop Fiber");

            PathMainMenu.CreateNewPath.Text = "Create New Path";
            PathMainMenu.Build();
            PathMainMenu.Menu.Visible = true;

            MainMenu.Build();
            DriverMenu.Build();
            PathCreationMenu.Build();
            ExportPathMenu.Build();
            BarrierMenu.Build();
        }

        internal void LowerWaypointBlipsOpacity()
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

        internal void Disable()
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
            Game.LogTrivial($"Path {Name} disabled.");
        }

        internal void Enable()
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
            Game.LogTrivial($"Path {Name} enabled.");
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
                GameFiber.SleepUntil(() => IsEnabled, 0);
                
                if(State == State.Deleting)
                {
                    Game.LogTrivial($"Path deleted, ending waypoint collection.");
                    return;
                }

                int checksDone = 0;
                var collectorWaypoints = Waypoints.Where(x => x.IsCollector);

                foreach (Waypoint waypoint in collectorWaypoints.ToList())
                {
                    foreach (Vehicle vehicle in World.GetAllVehicles().Where(x => x))
                    {
                        if (vehicle.IsNearCollectorWaypoint(waypoint) && vehicle.IsValidForPathCollection(this))
                        {
                            while(!vehicle.Driver)
                            {
                                GameFiber.Yield();
                                if (!vehicle)
                                {
                                    break;
                                }
                            }
                            if (!vehicle)
                            {
                                continue;
                            }
                            CollectedPeds.Add(new CollectedPed(vehicle.Driver, this, waypoint));
                        }

                        checksDone++; // Increment the counter inside the vehicle loop
                        if (checksDone % yieldAfterChecks == 0)
                        {
                            GameFiber.Yield(); // Yield the game fiber after the specified number of vehicles have been checked
                            if(State == State.Deleting)
                            {
                                Game.LogTrivial($"Path deleted, ending waypoint collection.");
                                return;
                            }
                        }
                    }
                }

                GameFiber.Sleep((int)Math.Max(1, Game.GameTime - lastProcessTime)); // If the prior lines took more than a second to run, then you'll run again almost immediately, but if they ran fairly quickly, you can sleep the loop until the remainder of the time between checks has passed
                lastProcessTime = Game.GameTime;
            }
        }

        internal void Delete()
        {
            var pathIndex = Array.IndexOf(PathManager.Paths, this);
            State = State.Deleting;
            RemoveAllBarriers();
            DismissCollectedDrivers();
            RemoveAllWaypoints();
            PathManager.Paths[pathIndex] = null;
            Game.LogTrivial($"Path {Name} deleted.");
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

        private void RemoveAllWaypoints()
        {
            Waypoints.ForEach(x => x.Delete());
            Waypoints.Clear();
        }

        private void RemoveAllBarriers()
        {
            Game.LogTrivial($"Deleting barriers.");
            foreach(Barrier barrier in Barriers)
            {
                barrier.Delete();
            }
        }

        internal void ChangeName()
        {
            var pathName = UserInput.PromptPlayerForFileName("Type the name you would like for your path", $"{Name}", 100);
            if (string.IsNullOrWhiteSpace(pathName))
            {
                Game.DisplayHelp($"Invalid path name given.  Name cannot be null, empty, or consist of just white spaces.  Defaulting to ~b~\"{Name}\"");
                Game.LogTrivial($"Invalid path name given.  Name cannot be null, empty, or consist of just white spaces.  Defaulting to \"{Name}\"");
                return;
            }
            if (PathManager.Paths.Any(x => x != null && x.Name == pathName))
            {
                Game.DisplayHelp($"Invalid path name given.  A path with that name already exists.  Defaulting to ~b~\"{Name}\"");
                Game.LogTrivial($"Invalid path name given.  A path with that name already exists.  Defaulting to \"{Name}\"");
                return;
            }

            Name = pathName;
        }

        private static XmlAttributeOverrides DefineOverridesForCombinedPath()
        {
            XmlAttributeOverrides overrides = new XmlAttributeOverrides();
            XmlAttributes attr = new XmlAttributes();
            attr.XmlRoot = new XmlRootAttribute("Paths");
            overrides.Add(typeof(List<Path>), attr);

            return overrides;
        }
    }
}
