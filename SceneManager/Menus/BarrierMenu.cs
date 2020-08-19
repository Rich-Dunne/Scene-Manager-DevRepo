using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    class BarrierMenu
    {
        public static List<Rage.Object> barriers = new List<Rage.Object>() { };

        // TODO: Refactor as dictionary
        private static UIMenuListScrollerItem<string> barrierList = new UIMenuListScrollerItem<string>("Select Barrier", "", new[] { "Large Striped Cone", "Large Cone", "Medium Striped Cone", "Medium Cone", "Roadpole A", "Roadpole B", "Police Barrier", "Road Barrier", "Flare" });
        private static string[] barrierObjectNames = new string[] { "prop_mp_cone_01", "prop_roadcone01c", "prop_mp_cone_02", "prop_mp_cone_03", "prop_roadpole_01a", "prop_roadpole_01b", "prop_barrier_work05", "prop_barrier_work06a", "prop_flare_01b" };
        private static UIMenuNumericScrollerItem<int> rotateBarrier = new UIMenuNumericScrollerItem<int>("Rotate Barrier", "Rotate the barrier.", 0, 359, 10);
        private static UIMenuListScrollerItem<string> removeBarrierOptions = new UIMenuListScrollerItem<string>("Remove Barrier", "", new[] { "Last Barrier", "Nearest Barrier", "All Barriers" });
        public static Rage.Object shadowBarrier;

        public static void BuildBarrierMenu()
        {
            MenuManager.barrierMenu.AddItem(removeBarrierOptions, 0);
            removeBarrierOptions.Enabled = false;
            MenuManager.barrierMenu.AddItem(rotateBarrier, 0);
            MenuManager.barrierMenu.AddItem(barrierList, 0);
            MenuManager.barrierMenu.RefreshIndex();

            MenuManager.barrierMenu.OnItemSelect += BarrierMenu_OnItemSelected;
            MenuManager.barrierMenu.OnScrollerChange += BarrierMenu_OnScrollerChange;
        }

        public static void CreateShadowBarrier(UIMenu barrierMenu)
        {
            if (EntryPoint.Settings.EnableHints)
            {
                Game.DisplayNotification($"~o~Scene Manager\n~y~[Hint]~y~ ~w~The shadow cone will disappear if you aim too far away.");
            }

            //Game.LogTrivial("Creating shadow cone");
            if (shadowBarrier)
                shadowBarrier.Delete();

            shadowBarrier = new Rage.Object(barrierObjectNames[barrierList.Index], TracePlayerView(15, TraceFlags.IntersectEverything).HitPosition, rotateBarrier.Index);
            Rage.Native.NativeFunction.Natives.PLACE_OBJECT_ON_GROUND_PROPERLY(shadowBarrier);
            shadowBarrier.IsGravityDisabled = true;
            shadowBarrier.IsCollisionEnabled = false;
            shadowBarrier.Opacity = 0.7f;

            GameFiber ShadowConeLoopFiber = new GameFiber(() => LoopToDisplayShadowBarrier(barrierMenu));
            ShadowConeLoopFiber.Start();
        }

        private static void LoopToDisplayShadowBarrier(UIMenu coneMenu)
        {
            while (coneMenu.Visible && shadowBarrier)
            {
                UpdateShadowBarrierPosition();
                GameFiber.Yield();
            }

            if (shadowBarrier)
                shadowBarrier.Delete();
        }

        private static void UpdateShadowBarrierPosition()
        {
            DisableBarrierMenuOptionsIfShadowConeTooFar();
            shadowBarrier.Position = TracePlayerView(15, TraceFlags.IntersectEverything).HitPosition;
            Rage.Native.NativeFunction.Natives.PLACE_OBJECT_ON_GROUND_PROPERLY(shadowBarrier);
            shadowBarrier.Heading = rotateBarrier.Index;

            void DisableBarrierMenuOptionsIfShadowConeTooFar()
            {
                if (shadowBarrier.Position.DistanceTo2D(Game.LocalPlayer.Character.Position) > 15f)
                {
                    barrierList.Enabled = false;
                    rotateBarrier.Enabled = false;
                }
                else
                {
                    barrierList.Enabled = true;
                    rotateBarrier.Enabled = true;
                }
            }
        }

        private static void BarrierMenu_OnScrollerChange(UIMenu sender, UIMenuScrollerItem scrollerItem, int oldIndex, int newIndex)
        {
            if (scrollerItem == barrierList)
            {
                CreateShadowBarrier(MenuManager.barrierMenu);

                if (barrierObjectNames[barrierList.Index] == "prop_flare_01b")
                {
                    rotateBarrier.Enabled = false;
                }
                else
                {
                    rotateBarrier.Enabled = true;
                }
            }

            if (scrollerItem == rotateBarrier)
            {
                shadowBarrier.Heading = rotateBarrier.Index;
            }
        }

        private static void BarrierMenu_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == barrierList as UIMenuItem)
            {
                // Attach some invisible object to the cone which the AI try to drive around
                // Barrier rotates with cone and becomes invisible similar to ASC when created

                if (shadowBarrier.Model.Name == "prop_flare_01b".ToUpper())
                {
                    SpawnFlare();
                }
                else
                {
                    SpawnBarrier();
                }

            }

            if (selectedItem == removeBarrierOptions as UIMenuItem)
            {
                RemoveBarrier();
            }
        }

        private static void SpawnBarrier()
        {
            var cone = new Rage.Object(shadowBarrier.Model, shadowBarrier.Position, rotateBarrier.Index);
            cone.SetPositionWithSnap(shadowBarrier.Position);
            Rage.Native.NativeFunction.Natives.SET_DISABLE_FRAG_DAMAGE(cone, true);
            Rage.Native.NativeFunction.Natives.SET_DISABLE_BREAKING(cone, true);

            barriers.Add(cone);
            removeBarrierOptions.Enabled = true;
        }

        private static void RemoveBarrier()
        {
            switch (removeBarrierOptions.Index)
            {
                case 0:
                    barriers[barriers.Count - 1].Delete();
                    barriers.RemoveAt(barriers.Count - 1);
                    break;
                case 1:
                    barriers = barriers.OrderBy(c => c.DistanceTo(Game.LocalPlayer.Character)).ToList();
                    barriers[0].Delete();
                    barriers.RemoveAt(0);
                    break;
                case 2:
                    foreach (Rage.Object c in barriers.Where(c => c))
                    {
                        c.Delete();
                    }
                    if (barriers.Count > 0)
                    {
                        barriers.Clear();
                    }
                    break;
            }

            removeBarrierOptions.Enabled = barriers.Count == 0 ? false : true;
        }

        private static void SpawnFlare()
        {
            var flare = new Weapon("weapon_flare", shadowBarrier.Position, 1);
            flare.SetPositionWithSnap(shadowBarrier.Position);

            // The purpose of this fiber is to allow the flare to spawn and fall to the ground naturally before freezing its position because you can't spawn it on the ground gracefully (it stands upright)
            GameFiber.StartNew(delegate
            {
                GameFiber.Sleep(1000);
                flare.IsPositionFrozen = true;
                flare.IsCollisionEnabled = false;
            });

            barriers.Add(flare);
            removeBarrierOptions.Enabled = true;
        }

        //------------ CREDIT PNWPARKS FOR THESE FUNCTIONS ------------\\
        // Implement Parks's 'Get Point Player is Looking At' script for better placement in 3rd person https://bitbucket.org/snippets/gtaparks/MeBKxX
        internal static Vector3 GetPlayerLookingDirection(out Vector3 camPosition)
        {
            if (Camera.RenderingCamera)
            {
                camPosition = Camera.RenderingCamera.Position;
                return Camera.RenderingCamera.Direction;
            }
            else
            {
                float pitch = Rage.Native.NativeFunction.Natives.GET_GAMEPLAY_CAM_RELATIVE_PITCH<float>();
                float heading = Rage.Native.NativeFunction.Natives.GET_GAMEPLAY_CAM_RELATIVE_HEADING<float>();

                camPosition = Rage.Native.NativeFunction.Natives.GET_GAMEPLAY_CAM_COORD<Vector3>();
                return (Game.LocalPlayer.Character.Rotation + new Rotator(pitch, 0, heading)).ToVector().ToNormalized();
            }
        }

        internal static Vector3 GetPlayerLookingDirection() => GetPlayerLookingDirection(out Vector3 v1);

        internal static HitResult TracePlayerView(out Vector3 start, out Vector3 end, float maxTraceDistance, TraceFlags flags)
        {
            Vector3 direction = GetPlayerLookingDirection(out start);
            end = start + (maxTraceDistance * direction);
            return World.TraceLine(start, end, flags);
        }

        internal static HitResult TracePlayerView(float maxTraceDistance = 15f, TraceFlags flags = TraceFlags.IntersectEverything) => TracePlayerView(out Vector3 v1, out Vector3 v2, maxTraceDistance, flags);
        //------------ CREDIT PNWPARKS FOR THESE FUNCTIONS ------------\\
    }
}
