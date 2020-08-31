using System;
using Rage;

namespace SceneManager
{
    class Barrier
    {
        private Rage.Object _barrier { get; set; }
        private Vector3 _barrierPosition { get; set; }
        private float _barrierRotation { get; set; }

        public Barrier(Rage.Object barrier, Vector3 barrierPosition, float barrierRotation)
        {
            _barrier = barrier;
            _barrierPosition = barrierPosition;
            _barrierRotation = barrierRotation;
        }

        public Rage.Object GetBarrier()
        {
            return _barrier;
        }

        public Vector3 GetPosition()
        {
            return _barrierPosition;
        }

        public float GetRotation()
        {
            return _barrierRotation;
        }

        internal object DistanceTo(Ped character)
        {
            throw new NotImplementedException();
        }
    }
}
