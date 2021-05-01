using Rage;
using SceneManager.Menus;
using SceneManager.Utils;
using System.Xml.Serialization;

namespace SceneManager.Objects
{
    [XmlRoot(ElementName = "Barrier", Namespace = "")]
    public class Barrier : Object // Change this and properties to Public for import/export
    {
        public Vector3 SpawnPosition { get; }
        public float SpawnHeading { get; }
        new public bool Invincible { get; }
        public bool Immobile { get; }
        public bool LightsEnabled { get; }
        public int TextureVariation { get; }

        internal Barrier(Object barrier, Vector3 barrierPosition, float barrierRotation, bool invincible, bool immobile, int textureVariation = 0, bool lightsEnabled = false) : base(barrier.Model, barrierPosition, barrierRotation)
        {
            SpawnPosition = barrierPosition;
            SpawnHeading = barrierRotation;
            Invincible = invincible;
            IsInvincible = invincible;
            Immobile = immobile;
            TextureVariation = textureVariation;
            LightsEnabled = lightsEnabled;

            if(BarrierManager.PlaceholderBarrier)
            {
                SetPositionWithSnap(BarrierManager.PlaceholderBarrier.Position);
            }

            Rage.Native.NativeFunction.Natives.SET_ENTITY_DYNAMIC(this, true);
            if (Invincible)
            {
                Rage.Native.NativeFunction.Natives.SET_DISABLE_FRAG_DAMAGE(this, true);
                if (Model.Name != "prop_barrier_wat_03a")
                {
                    Rage.Native.NativeFunction.Natives.SET_DISABLE_BREAKING(this, true);
                }
            }
            IsPositionFrozen = Immobile;

            if (Settings.EnableAdvancedBarricadeOptions)
            {
                SetAdvancedOptions();
            }
        }

        private void SetAdvancedOptions()
        {
            Rage.Native.NativeFunction.Natives.x971DA0055324D033(this, TextureVariation);
            if (LightsEnabled)
            {
                Rage.Native.NativeFunction.Natives.SET_ENTITY_LIGHTS(this, false);
            }
            else
            {
                Rage.Native.NativeFunction.Natives.SET_ENTITY_LIGHTS(this, true);
            }

            //Rage.Native.NativeFunction.Natives.SET_ENTITY_TRAFFICLIGHT_OVERRIDE(barrier, setBarrierTrafficLight.Index);
            IsPositionFrozen = true;
            GameFiber.Sleep(50);
            if (this && !Immobile)
            {
                IsPositionFrozen = false;
            }
        }
    }
}
