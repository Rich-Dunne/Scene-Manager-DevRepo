using System;
using Rage;

namespace SceneManager
{
    class Barrier
    {
        public Rage.Object Object { get; set; }
        public Vector3 Position { get; set; }
        public float Rotation { get; set; }

        public Barrier(Rage.Object barrier, Vector3 barrierPosition, float barrierRotation)
        {
            Object = barrier;
            Position = barrierPosition;
            Rotation = barrierRotation;
        }
    }
}
