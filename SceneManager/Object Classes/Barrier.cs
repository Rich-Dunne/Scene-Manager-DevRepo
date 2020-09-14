using System;
using Rage;

namespace SceneManager
{
    class Barrier
    {
        internal Rage.Object Object { get; set; }
        internal Vector3 Position { get; set; }
        internal float Rotation { get; set; }

        internal Barrier(Rage.Object barrier, Vector3 barrierPosition, float barrierRotation)
        {
            Object = barrier;
            Position = barrierPosition;
            Rotation = barrierRotation;
        }
    }
}
