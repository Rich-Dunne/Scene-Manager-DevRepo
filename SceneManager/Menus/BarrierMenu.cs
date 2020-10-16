using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    class BarrierMenu
    {
        internal static UIMenu barrierMenu = new UIMenu("Scene Manager", "~o~Barrier Management");
        internal static List<Barrier> barriers = new List<Barrier>();
        private static UIMenuListScrollerItem<string> barrierList = new UIMenuListScrollerItem<string>("Spawn Barrier", "", Settings.barrierKeys);
        private static UIMenuNumericScrollerItem<int> rotateBarrier = new UIMenuNumericScrollerItem<int>("Rotate Barrier", "", 0, 350, 10);
        private static UIMenuListScrollerItem<string> removeBarrierOptions = new UIMenuListScrollerItem<string>("Remove Barrier", "", new[] { "Last Barrier", "Nearest Barrier", "All Barriers" });
        private static UIMenuItem resetBarriers = new UIMenuItem("Reset Barriers", "Reset all spawned barriers to their original position and rotation");
        internal static Rage.Object shadowBarrier;

        internal static void InstantiateMenu()
        {
            barrierMenu.ParentMenu = MainMenu.mainMenu;
            MenuManager.menuPool.Add(barrierMenu);
        }

        internal static void BuildBarrierMenu()
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

            barrierMenu.OnItemSelect += BarrierMenu_OnItemSelected;
            barrierMenu.OnScrollerChange += BarrierMenu_OnScrollerChange;
        }

        internal static void CreateShadowBarrier(UIMenu barrierMenu)
        {
            Hints.Display($"~o~Scene Manager\n~y~[Hint]~y~ ~w~The shadow cone will disappear if you aim too far away.");

            if (shadowBarrier)
                shadowBarrier.Delete();

            shadowBarrier = new Rage.Object(Settings.barrierValues[barrierList.Index], TracePlayerView(Settings.BarrierPlacementDistance, TraceFlags.IntersectWorld).HitPosition, rotateBarrier.Value);
            if (!shadowBarrier)
            {
                barrierMenu.Close();
                Game.DisplayNotification($"~o~Scene Manager\n~red~[Error]~w~ Something went wrong creating the shadow barrier.  Please try again.");
                return;
            }
            Rage.Native.NativeFunction.Natives.PLACE_OBJECT_ON_GROUND_PROPERLY(shadowBarrier);
            shadowBarrier.IsGravityDisabled = true;
            shadowBarrier.IsCollisionEnabled = false;
            shadowBarrier.Opacity = 0.7f;

            GameFiber ShadowConeLoopFiber = new GameFiber(() => LoopToDisplayShadowBarrier());
            ShadowConeLoopFiber.Start();

            void LoopToDisplayShadowBarrier()
            {
                while (barrierMenu.Visible && shadowBarrier)
                {
                    if (barrierList.Selected || rotateBarrier.Selected)
                    {
                        shadowBarrier.IsVisible = true;
                        UpdateShadowBarrierPosition();
                    }
                    else
                    {
                        shadowBarrier.IsVisible = false;
                    }
                    GameFiber.Yield();
                }

                if (shadowBarrier)
                    shadowBarrier.Delete();

                void UpdateShadowBarrierPosition()
                {
                    DisableBarrierMenuOptionsIfShadowConeTooFar();
                    shadowBarrier.SetPositionWithSnap(TracePlayerView(Settings.BarrierPlacementDistance, TraceFlags.IntersectWorld).HitPosition);

                    void DisableBarrierMenuOptionsIfShadowConeTooFar()
                    {
                        if (shadowBarrier.Position.DistanceTo2D(Game.LocalPlayer.Character.Position) > Settings.BarrierPlacementDistance)
                        {
                            barrierList.Enabled = false;
                            rotateBarrier.Enabled = false;
                        }
                        else if (shadowBarrier.Position.DistanceTo2D(Game.LocalPlayer.Character.Position) <= Settings.BarrierPlacementDistance && barrierList.SelectedItem == "Flare")
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
            }

            //------------ CREDIT PNWPARKS FOR THESE FUNCTIONS ------------\\
            // Implement Parks's 'Get Point Player is Looking At' script for better placement in 3rd person https://bitbucket.org/snippets/gtaparks/MeBKxX

            HitResult TracePlayerView(float maxTraceDistance = 30f, TraceFlags flags = TraceFlags.IntersectWorld) => TracePlayerView2(out Vector3 v1, out Vector3 v2, maxTraceDistance, flags);

            HitResult TracePlayerView2(out Vector3 start, out Vector3 end, float maxTraceDistance, TraceFlags flags)
            {
                Vector3 direction = GetPlayerLookingDirection(out start);
                end = start + (maxTraceDistance * direction);
                var barrierObjects = barriers.Where(b => b.Object).Select(b => b.Object).ToArray();
                return World.TraceLine(start, end, flags, barrierObjects);
            }

            Vector3 GetPlayerLookingDirection(out Vector3 camPosition)
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
            //------------ CREDIT PNWPARKS FOR THESE FUNCTIONS ------------\\
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

                barrierMenu.Width = SetMenuWidth();
            }

            if (scrollerItem == rotateBarrier)
            {
                shadowBarrier.Heading = rotateBarrier.Value;
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
            
            if (selectedItem == resetBarriers)
            {
                var currentBarriers = barriers.Where(b => b.Model.Name != "0xa2c44e80").ToList(); // 0xa2c44e80 is the flare weapon hash
                foreach (Barrier barrier in currentBarriers)
                {
                    var newBarrier = new Rage.Object(barrier.Model, barrier.Position, barrier.Rotation);
                    newBarrier.SetPositionWithSnap(barrier.Position);
                    Rage.Native.NativeFunction.Natives.SET_ENTITY_DYNAMIC(newBarrier, true);
                    newBarrier.IsPositionFrozen = false;
                    Rage.Native.NativeFunction.Natives.SET_DISABLE_FRAG_DAMAGE(newBarrier, true);
                    barriers.Add(new Barrier(newBarrier, newBarrier.Position, newBarrier.Heading));


                    if (barrier.Object)
                    {
                        barrier.Object.Delete();
                    }
                    barriers.Remove(barrier);
                }
                currentBarriers.Clear();
            }

            void SpawnBarrier()
            {
                var barrier = new Rage.Object(shadowBarrier.Model, shadowBarrier.Position, rotateBarrier.Value);
                barrier.SetPositionWithSnap(shadowBarrier.Position);
                Rage.Native.NativeFunction.Natives.SET_ENTITY_DYNAMIC(barrier, true);
                barrier.IsPositionFrozen = false;
                Rage.Native.NativeFunction.Natives.SET_DISABLE_FRAG_DAMAGE(barrier, true);

                barriers.Add(new Barrier(barrier, barrier.Position, barrier.Heading));
                removeBarrierOptions.Enabled = true;
                resetBarriers.Enabled = true;
            }

            void SpawnFlare()
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

            void RemoveBarrier()
            {
                switch (removeBarrierOptions.Index)
                {
                    case 0:
                        barriers[barriers.Count - 1].Object.Delete();
                        barriers.RemoveAt(barriers.Count - 1);
                        break;
                    case 1:
                        var nearestBarrier = barriers.OrderBy(b => b.Object.DistanceTo2D(Game.LocalPlayer.Character)).FirstOrDefault();
                        if(nearestBarrier != null)
                        {
                            nearestBarrier.Object.Delete();
                            barriers.Remove(nearestBarrier);
                        }
                        break;
                    case 2:
                        foreach (Barrier b in barriers.Where(b => b.Object))
                        {
                            b.Object.Delete();
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
        }

        private static float SetMenuWidth()
        {
            float defaultWidth = UIMenu.DefaultWidth;
            float width = barrierMenu.Width;

            barrierList.TextStyle.Apply();
            Rage.Native.NativeFunction.Natives.x54CE8AC98E120CAB("STRING"); // _BEGIN_TEXT_COMMAND_GET_WIDTH
            Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(barrierList.SelectedItem);
            float textWidth = Rage.Native.NativeFunction.Natives.x85F061DA64ED2F67<float>(true); // _END_TEXT_COMMAND_GET_WIDTH
            float padding = 0.00390625f * 2; // typical padding used in RNUI

            var selectedItemWidth = textWidth + padding;
            if(selectedItemWidth <= 0.14f)
            {
                return defaultWidth;
            }
            else
            {
                return selectedItemWidth * 1.6f;
            }
        }
    }
}
