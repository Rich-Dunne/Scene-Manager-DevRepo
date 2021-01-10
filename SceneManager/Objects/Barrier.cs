using Rage;
using System.Xml.Serialization;

namespace SceneManager.Objects
{
    [XmlRoot(ElementName = "Barrier", Namespace = "")]
    public class Barrier // Change this and properties to Public for import/export
    {
        public Object Object { get; }
        public Model @Model{ get; }
        public Vector3 Position { get; }
        public float Rotation { get; }
        public bool Invincible { get; }
        public bool Immobile { get; }

        internal Barrier(Object barrier, Vector3 barrierPosition, float barrierRotation, bool invincible, bool immobile)
        {
            Object = barrier;
            @Model = barrier.Model;
            Position = barrierPosition;
            Rotation = barrierRotation;
            Invincible = invincible;
            Immobile = immobile;
        }
    }
}
