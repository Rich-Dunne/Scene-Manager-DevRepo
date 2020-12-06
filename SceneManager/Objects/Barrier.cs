using System;
using System.Collections.Generic;
using System.Linq;
using Rage;
using SceneManager.Utils;

namespace SceneManager.Objects
{
    class Barrier
    {
        internal Rage.Object Object { get; }
        internal Model @Model{ get; }
        internal Vector3 Position { get; }
        internal float Rotation { get; }
        internal bool Invincible { get; }

        internal bool Immobile { get; }

        internal Barrier(Rage.Object barrier, Vector3 barrierPosition, float barrierRotation, bool invincible, bool immobile)
        {
            Object = barrier;
            @Model = barrier.Model;
            Position = barrierPosition;
            Rotation = barrierRotation;
            Invincible = invincible;
            Immobile = immobile;
            //AddBlocker();
        }

        private void AddBlocker()
        {
            var blocker = new Rage.Object("prop_barier_conc_01a", Position, Rotation);
            blocker.AttachTo(Object, 0, new Vector3(0, 0, 0), new Rotator());
            GameFiber.StartNew(() =>
            {
                while (Object)
                {
                    GameFiber.Yield();
                }
                blocker.Delete();
            });
        }

        internal void GoAround()
        {
            GameFiber.StartNew(() =>
            {
                var collected = new List<Vehicle>();
                while (Object)
                {
                    foreach (Vehicle v in World.GetAllVehicles())
                    {
                        if(v && v.IsEngineOn)
                        {
                            if(v.HasDriver && v.Driver && v.Driver.IsAlive)
                            {
                                if (!collected.Contains(v))
                                {
                                    v.Driver.Tasks.Clear();
                                    v.Driver.Tasks.CruiseWithVehicle(5f, (VehicleDrivingFlags)17039872);
                                    v.Driver.KeepTasks = true;
                                    collected.Add(v);
                                }
                            }
                        }
                    }
                    GameFiber.Sleep(1000);
                }
            });
        }
    }
}
