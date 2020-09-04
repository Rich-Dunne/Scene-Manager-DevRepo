using System;
using System.Collections.Generic;
using System.Drawing;
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
        public static UIMenu barrierMenu { get; private set; }
        public static List<Barrier> barriers = new List<Barrier>();
        //public static List<Rage.Object> barriers = new List<Rage.Object>() { };

        private static UIMenuListScrollerItem<string> barrierList = new UIMenuListScrollerItem<string>("Select Barrier", "", new[] { "Large Striped Cone", "Large Cone", "Medium Striped Cone", "Medium Cone", "Roadpole A", "Roadpole B", "Police Barrier", "Road Barrier", "Flare" });
        private static string[] barrierObjectNames = new string[] { "prop_mp_cone_01", "prop_roadcone01c", "prop_mp_cone_02", "prop_mp_cone_03", "prop_roadpole_01a", "prop_roadpole_01b", "prop_barrier_work05", "prop_barrier_work06a", "prop_flare_01b" };
        private static UIMenuNumericScrollerItem<int> rotateBarrier = new UIMenuNumericScrollerItem<int>("Rotate Barrier", "", 0, 350, 10);
        private static UIMenuListScrollerItem<string> removeBarrierOptions = new UIMenuListScrollerItem<string>("Remove Barrier", "", new[] { "Last Barrier", "Nearest Barrier", "All Barriers" });
        private static UIMenuItem resetBarriers = new UIMenuItem("Reset Barriers", "Reset all spawned barriers to their original position and rotation");
        public static Rage.Object shadowBarrier;

        internal static void InstantiateMenu()
        {
            barrierMenu = new UIMenu("Scene Manager", "~o~Barrier Management");
            barrierMenu.ParentMenu = MainMenu.mainMenu;
            MenuManager.menuPool.Add(barrierMenu);
        }

        public static void BuildBarrierMenu()
        {
            barrierMenu.AddItem(resetBarriers);
            resetBarriers.ForeColor = Color.Gold;
            resetBarriers.Enabled = false;

            barrierMenu.AddItem(removeBarrierOptions, 0);
            removeBarrierOptions.ForeColor = Color.Gold;
            removeBarrierOptions.Enabled = false;

            barrierMenu.AddItem(rotateBarrier, 0);

            barrierMenu.AddItem(barrierList, 0);
            barrierList.ForeColor = Color.Gold;

            barrierMenu.RefreshIndex();

            barrierMenu.OnItemSelect += BarrierMenu_OnItemSelected;
            barrierMenu.OnScrollerChange += BarrierMenu_OnScrollerChange;
        }

        public static void CreateShadowBarrier(UIMenu barrierMenu)
        {
            if (Settings.EnableHints)
            {
                Hints.Display($"~o~Scene Manager\n~y~[Hint]~y~ ~w~The shadow cone will disappear if you aim too far away.");
            }

            //Game.LogTrivial("Creating shadow barrier");
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
            shadowBarrier.Position = TracePlayerView(SettingsMenu.barrierPlacementDistance.Value, TraceFlags.IntersectEverything).HitPosition;
            Rage.Native.NativeFunction.Natives.PLACE_OBJECT_ON_GROUND_PROPERLY(shadowBarrier);
            shadowBarrier.Heading = rotateBarrier.Value;

            void DisableBarrierMenuOptionsIfShadowConeTooFar()
            {
                if (shadowBarrier.Position.DistanceTo2D(Game.LocalPlayer.Character.Position) > SettingsMenu.barrierPlacementDistance.Value)
                {
                    barrierList.Enabled = false;
                    rotateBarrier.Enabled = false;
                }
                else if(shadowBarrier.Position.DistanceTo2D(Game.LocalPlayer.Character.Position) <= SettingsMenu.barrierPlacementDistance.Value && barrierList.SelectedItem == "Flare")
                {
                    barrierList.Enabled = true;
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
                CreateShadowBarrier(barrierMenu);

                if(barrierList.SelectedItem == "Flare")
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
                if(barrierList.SelectedItem == "Flare")
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

            if(selectedItem == resetBarriers)
            {
                foreach(Barrier barrier in barriers.Where(b => b.GetBarrier() && !b.GetBarrier().Model.Name.Contains("flare")))
                {
                    barrier.GetBarrier().Position = barrier.GetPosition();
                    barrier.GetBarrier().Heading = barrier.GetRotation();
                }
            }
        }

        private static void SpawnBarrier()
        {
            var barrier = new Rage.Object(shadowBarrier.Model, shadowBarrier.Position, rotateBarrier.Value);
            barrier.SetPositionWithSnap(shadowBarrier.Position);
            Rage.Native.NativeFunction.Natives.SET_DISABLE_FRAG_DAMAGE(barrier, true);
            Rage.Native.NativeFunction.Natives.SET_DISABLE_BREAKING(barrier, true);

            barriers.Add(new Barrier(barrier, barrier.Position, barrier.Heading));
            removeBarrierOptions.Enabled = true;
            resetBarriers.Enabled = true;
        }

        private static void RemoveBarrier()
        {
            switch (removeBarrierOptions.Index)
            {
                case 0:
                    barriers[barriers.Count - 1].GetBarrier().Delete();
                    barriers.RemoveAt(barriers.Count - 1);
                    break;
                case 1:
                    barriers = barriers.OrderBy(c => c.DistanceTo(Game.LocalPlayer.Character)).ToList();
                    barriers[0].GetBarrier().Delete();
                    barriers.RemoveAt(0);
                    break;
                case 2:
                    foreach (Barrier b in barriers.Where(b => b.GetBarrier()))
                    {
                        b.GetBarrier().Delete();
                    }
                    if (barriers.Count > 0)
                    {
                        barriers.Clear();
                    }
                    break;
            }

            removeBarrierOptions.Enabled = barriers.Count == 0 ? false : true;
            resetBarriers.Enabled = barriers.Count == 0 ? false : true;
        }

        private static void SpawnFlare()
        {
            var flare = new Weapon("weapon_flare", shadowBarrier.Position, 1);

            Rage.Native.NativeFunction.Natives.SET_ENTITY_DYNAMIC(flare, true);
            GameFiber.StartNew(() =>
            {
                while (flare && flare.HeightAboveGround > 0.05f)
                {
                    GameFiber.Yield();
                }
                GameFiber.Sleep(1000);
                if (flare)
                {
                    flare.IsPositionFrozen = true;
                }
            });

            barriers.Add(new Barrier(flare, flare.Position, flare.Heading));
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

        internal static HitResult TracePlayerView(float maxTraceDistance = 30f, TraceFlags flags = TraceFlags.IntersectEverything) => TracePlayerView(out Vector3 v1, out Vector3 v2, maxTraceDistance, flags);
        //------------ CREDIT PNWPARKS FOR THESE FUNCTIONS ------------\\
    }
}
