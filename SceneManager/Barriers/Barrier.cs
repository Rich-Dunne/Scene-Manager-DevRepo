using Rage;
using SceneManager.Managers;
using SceneManager.Paths;
using SceneManager.Utils;

namespace SceneManager.Barriers
{
    public class Barrier : IDeletable, IHandleable, ISpatial
    {
        private Object _object { get; }
        public string ModelName { get; set; }
        public Vector3 Position { get; set; }
        public float Heading { get; set; }
        public bool Invincible { get; set; }
        public bool Immobile { get; set; }
        public bool LightsEnabled { get; set; }
        public int TextureVariation { get; set; }
        internal Path Path { get; set; }

        public PoolHandle Handle => ((IHandleable)_object).Handle;

        private Barrier() { }

        internal Barrier(Barrier obj, bool invincible, bool immobile, int textureVariation = 0, bool lightsEnabled = false)
        {
            ModelName = obj.ModelName;
            Position = obj.Position;
            Heading = obj.Heading;
            Invincible = invincible;
            Immobile = immobile;
            TextureVariation = textureVariation;
            LightsEnabled = lightsEnabled;
            if (obj.IsValid())
            {
                obj.Delete();
            }

            _object = new Object(ModelName, Position, Heading);

            _object.SetPositionWithSnap(Position);
            Rage.Native.NativeFunction.Natives.PLACE_OBJECT_ON_GROUND_PROPERLY<bool>(_object);

            Rage.Native.NativeFunction.Natives.SET_ENTITY_DYNAMIC(_object, true);
            if (Invincible)
            {
                Rage.Native.NativeFunction.Natives.SET_DISABLE_FRAG_DAMAGE(_object, true);
                if (ModelName != "prop_barrier_wat_03a")
                {
                    Rage.Native.NativeFunction.Natives.SET_DISABLE_BREAKING(_object, true);
                }
            }
            _object.IsPositionFrozen = Immobile;

            if (Settings.EnableAdvancedBarricadeOptions)
            {
                SetAdvancedOptions();
            }
        }

        internal Barrier(Object obj, bool invincible, bool immobile, int textureVariation = 0, bool lightsEnabled = false)
        {
            ModelName = obj.Model.Name;
            Position = obj.Position;
            Heading = obj.Heading;
            Invincible = invincible;
            Immobile = immobile;
            TextureVariation = textureVariation;
            LightsEnabled = lightsEnabled;
            if(BarrierManager.PlaceholderBarrier)
            {
                BarrierManager.PlaceholderBarrier.Delete();
            }

            _object = new Object(ModelName, Position, Heading);

            _object.SetPositionWithSnap(Position);
            Rage.Native.NativeFunction.Natives.PLACE_OBJECT_ON_GROUND_PROPERLY<bool>(_object);

            Rage.Native.NativeFunction.Natives.SET_ENTITY_DYNAMIC(_object, true);
            if (Invincible)
            {
                Rage.Native.NativeFunction.Natives.SET_DISABLE_FRAG_DAMAGE(_object, true);
                if (ModelName != "prop_barrier_wat_03a")
                {
                    Rage.Native.NativeFunction.Natives.SET_DISABLE_BREAKING(_object, true);
                }
            }
            _object.IsPositionFrozen = Immobile;

            if (Settings.EnableAdvancedBarricadeOptions)
            {
                SetAdvancedOptions();
            }
        }

        private void SetAdvancedOptions()
        {
            Rage.Native.NativeFunction.Natives.x971DA0055324D033(_object, TextureVariation);
            if (LightsEnabled)
            {
                Rage.Native.NativeFunction.Natives.SET_ENTITY_LIGHTS(_object, false);
            }
            else
            {
                Rage.Native.NativeFunction.Natives.SET_ENTITY_LIGHTS(_object, true);
            }

            //Rage.Native.NativeFunction.Natives.SET_ENTITY_TRAFFICLIGHT_OVERRIDE(barrier, setBarrierTrafficLight.Index);
            _object.IsPositionFrozen = true;
            GameFiber.Sleep(50);
            if (_object && !Immobile)
            {
                _object.IsPositionFrozen = false;
            }
        }

        internal void LoadFromImport(Path path)
        {
            Path = path;
            Game.LogTrivial($"===== BARRIER DATA =====");
            Game.LogTrivial($"Model: {ModelName}");
            Game.LogTrivial($"Position: {Position}");
            Game.LogTrivial($"Heading: {Heading}");
            Game.LogTrivial($"Invincible: {Invincible}");
            Game.LogTrivial($"Immobile: {Immobile}");
            Game.LogTrivial($"LightsEnabled: {LightsEnabled}");
            Game.LogTrivial($"Texture Variation: {TextureVariation}");
            var barrier = new Barrier(this, Invincible, Immobile, TextureVariation, LightsEnabled);
            Path.Barriers.Add(barrier);
            BarrierManager.Barriers.Add(barrier);
        }

        public void Delete()
        {
            if (_object)
            {
                ((IDeletable)_object).Delete();
            }
        }

        public bool IsValid()
        {
            return _object != null;
        }

        public bool Equals(IHandleable other)
        {
            return ((System.IEquatable<IHandleable>)_object).Equals(other);
        }

        public float DistanceTo(Vector3 position)
        {
            return ((ISpatial)_object).DistanceTo(position);
        }

        public float DistanceTo(ISpatial spatialObject)
        {
            return ((ISpatial)_object).DistanceTo(spatialObject);
        }

        public float DistanceTo2D(Vector3 position)
        {
            return ((ISpatial)_object).DistanceTo2D(position);
        }

        public float DistanceTo2D(ISpatial spatialObject)
        {
            return ((ISpatial)_object).DistanceTo2D(spatialObject);
        }

        public float TravelDistanceTo(Vector3 position)
        {
            return ((ISpatial)_object).TravelDistanceTo(position);
        }

        public float TravelDistanceTo(ISpatial spatialObject)
        {
            return ((ISpatial)_object).TravelDistanceTo(spatialObject);
        }
    }
}
