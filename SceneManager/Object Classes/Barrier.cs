using System;
using Rage;

namespace SceneManager
{
    class Barrier
    {
        internal Rage.Object Object { get; }
        internal Model @Model{ get; }
        internal Vector3 Position { get; }
        internal float Rotation { get; }

        internal Barrier(Rage.Object barrier, Vector3 barrierPosition, float barrierRotation)
        {
            Object = barrier;
            @Model = barrier.Model;
            Position = barrierPosition;
            Rotation = barrierRotation;
        }
    }
}
