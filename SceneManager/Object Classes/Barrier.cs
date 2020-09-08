using System;
using Rage;

namespace SceneManager
{
    class Barrier
    {
        private Rage.Object _barrier { get; set; }
        private Vector3 _barrierPosition { get; set; }
        private float _barrierRotation { get; set; }

        public Rage.Object Object { get { return _barrier; } set { _barrier = value; } }
        public Vector3 Position { get { return _barrierPosition; } set { _barrierPosition = value; } }
        public float Rotation { get { return _barrierRotation; } set { _barrierRotation = value; } }

        public Barrier(Rage.Object barrier, Vector3 barrierPosition, float barrierRotation)
        {
            _barrier = barrier;
            _barrierPosition = barrierPosition;
            _barrierRotation = barrierRotation;
        }
    }
}
