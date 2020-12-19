using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using SceneManager.Objects;
using SceneManager.Utils;

namespace SceneManager
{
    class BarrierMenu
    {
        private static List<TrafficLight> trafficLightList = new List<TrafficLight>() { TrafficLight.Green, TrafficLight.Red, TrafficLight.Yellow, TrafficLight.None };
        internal static UIMenu barrierMenu = new UIMenu("Scene Manager", "~o~Barrier Management");
        internal static List<Barrier> barriers = new List<Barrier>();
        private static UIMenuListScrollerItem<string> barrierList = new UIMenuListScrollerItem<string>("Spawn Barrier", "", Settings.barriers.Keys); // Settings.barrierKeys
        private static UIMenuNumericScrollerItem<int> rotateBarrier = new UIMenuNumericScrollerItem<int>("Rotate Barrier", "", 0, 350, 10);
        // ADD CHECKBOX FOR BARRIER TO STOP TRAFFIC?  ADD 3D MARKER TO SHOW WHERE TRAFFIC WILL STOP.  ONLY NEED ONE CONE TO DO IT PER LANE
        private static UIMenuCheckboxItem invincible = new UIMenuCheckboxItem("Indestructible", false);
        private static UIMenuCheckboxItem immobile = new UIMenuCheckboxItem("Immobile", false);
        private static UIMenuNumericScrollerItem<int> barrierTexture = new UIMenuNumericScrollerItem<int>("Change Texture", "", 0, 15, 1);
        private static UIMenuCheckboxItem setBarrierLights = new UIMenuCheckboxItem("Enable Barrier Lights", Settings.EnableBarrierLightsDefaultOn);
        private static UIMenuListScrollerItem<TrafficLight> setBarrierTrafficLight = new UIMenuListScrollerItem<TrafficLight>("Set Barrier Traffic Light", "", trafficLightList);
        private static UIMenuListScrollerItem<string> removeBarrierOptions = new UIMenuListScrollerItem<string>("Remove Barrier", "", new[] { "Last Barrier", "Nearest Barrier", "All Barriers" });
        private static UIMenuItem resetBarriers = new UIMenuItem("Reset Barriers", "Reset all spawned barriers to their original position and rotation");
        internal static Object shadowBarrier;

        internal static void InstantiateMenu()
        {
            barrierMenu.ParentMenu = MainMenu.mainMenu;
            MenuManager.menuPool.Add(barrierMenu);

            barrierMenu.OnItemSelect += BarrierMenu_OnItemSelected;
            barrierMenu.OnScrollerChange += BarrierMenu_OnScrollerChanged;
            barrierMenu.OnCheckboxChange += BarrierMenu_OnCheckboxChanged;
            barrierMenu.OnMenuOpen += BarrierMenu_OnMenuOpen;
        }

        internal static void BuildBarrierMenu()
        {
            barrierMenu.AddItem(barrierList);
            barrierList.ForeColor = Color.Gold;

            barrierMenu.AddItem(rotateBarrier);

            barrierMenu.AddItem(invincible);

            barrierMenu.AddItem(immobile);

            if (Settings.EnableAdvancedBarricadeOptions)
            {
                barrierMenu.AddItem(barrierTexture);
                barrierTexture.Index = 0;

                barrierMenu.AddItem(setBarrierLights);

                //barrierMenu.AddItem(setBarrierTrafficLight);
                //setBarrierTrafficLight.Index = 3;
            }

            barrierMenu.AddItem(removeBarrierOptions);
            removeBarrierOptions.ForeColor = Color.Gold;
            removeBarrierOptions.Enabled = false;

            barrierMenu.AddItem(resetBarriers);
            resetBarriers.ForeColor = Color.Gold;
            resetBarriers.Enabled = false;    
        }

        internal static void CreateShadowBarrier()
        {
            if (shadowBarrier)
            {
                shadowBarrier.Delete();
            }

            var barrierKey = Settings.barriers.Where(x => x.Key == barrierList.SelectedItem).FirstOrDefault().Key;
            var barrierValue = Settings.barriers[barrierKey].Name;
            shadowBarrier = new Object(barrierValue, MousePositionInWorld.GetPosition, rotateBarrier.Value);

            // There arent enough available object handles in certain areas
            // The object gets spawned(sometimes) but doesn't get assigned a handle, so it returns a handle of 0 - Parks
            if (!shadowBarrier)
            {
                barrierMenu.Close();
                Game.DisplayNotification($"~o~Scene Manager ~r~[Error]\n~w~Something went wrong creating the shadow barrier.  Please try again.");
                return;
            }
            //Rage.Native.NativeFunction.Natives.SET_ENTITY_TRAFFICLIGHT_OVERRIDE(shadowBarrier, setBarrierTrafficLight.Index);
            Rage.Native.NativeFunction.Natives.PLACE_OBJECT_ON_GROUND_PROPERLY(shadowBarrier);
            shadowBarrier.IsGravityDisabled = true;
            shadowBarrier.IsCollisionEnabled = false;
            shadowBarrier.Opacity = 0.7f;

            if (Settings.EnableAdvancedBarricadeOptions)
            {
                Rage.Native.NativeFunction.Natives.x971DA0055324D033(shadowBarrier, barrierTexture.Value);
                SetBarrierLights();
            }
        }

        private static void LoopToDisplayShadowBarrier()
        {
            while (barrierMenu.Visible)
            {
                if (barrierList.Selected || rotateBarrier.Selected || invincible.Selected || immobile.Selected || barrierTexture.Selected || setBarrierLights.Selected || setBarrierTrafficLight.Selected)
                {
                    if (shadowBarrier)
                    {
                        UpdateShadowBarrierPosition();
                    }
                    else if(MousePositionInWorld.GetPositionForBarrier.DistanceTo2D(Game.LocalPlayer.Character.Position) <= Settings.BarrierPlacementDistance)
                    {
                        CreateShadowBarrier();
                    }
                }
                else
                {
                    if (shadowBarrier)
                    {
                        shadowBarrier.Delete();
                    }
                }
                GameFiber.Yield();
            }

            if (shadowBarrier)
            {
                shadowBarrier.Delete();
            }

            void UpdateShadowBarrierPosition()
            {
                DisableBarrierMenuOptionsIfShadowConeTooFar();
                if (shadowBarrier)
                {
                    // Delete and re-create for testing purposes.. Parks' stop light prop
                    //shadowBarrier.Delete();
                    //CreateShadowBarrier();
                    shadowBarrier.Heading = rotateBarrier.Value;
                    shadowBarrier.Position = MousePositionInWorld.GetPositionForBarrier;
                    Rage.Native.NativeFunction.Natives.PLACE_OBJECT_ON_GROUND_PROPERLY(shadowBarrier);
                    //Rage.Native.NativeFunction.Natives.SET_ENTITY_TRAFFICLIGHT_OVERRIDE(shadowBarrier, setBarrierTrafficLight.Index);
                }

                void DisableBarrierMenuOptionsIfShadowConeTooFar()
                {
                    if (!shadowBarrier && MousePositionInWorld.GetPositionForBarrier.DistanceTo2D(Game.LocalPlayer.Character.Position) <= Settings.BarrierPlacementDistance)
                    {
                        CreateShadowBarrier();

                    }
                    else if (shadowBarrier && shadowBarrier.Position.DistanceTo2D(Game.LocalPlayer.Character.Position) > Settings.BarrierPlacementDistance)
                    {
                        barrierList.Enabled = false;
                        rotateBarrier.Enabled = false;
                        shadowBarrier.Delete();
                    }
                    else if (shadowBarrier && shadowBarrier.Position.DistanceTo2D(Game.LocalPlayer.Character.Position) <= Settings.BarrierPlacementDistance && barrierList.SelectedItem == "Flare")
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

        private static void SpawnBarrier()
        {
            GameFiber.StartNew(() =>
            {
                if (barrierList.SelectedItem == "Flare")
                {
                    SpawnFlare();
                }
                else
                {
                    var barrier = new Object(shadowBarrier.Model, shadowBarrier.Position, rotateBarrier.Value);
                    barrier.SetPositionWithSnap(shadowBarrier.Position);
                    Rage.Native.NativeFunction.Natives.SET_ENTITY_DYNAMIC(barrier, true);
                    if (invincible.Checked)
                    {
                        Rage.Native.NativeFunction.Natives.SET_DISABLE_FRAG_DAMAGE(barrier, true);
                        if (barrier.Model.Name != "prop_barrier_wat_03a")
                        {
                            Rage.Native.NativeFunction.Natives.SET_DISABLE_BREAKING(barrier, true);
                        }
                    }
                    if (immobile.Checked)
                    {
                        barrier.IsPositionFrozen = true;
                    }
                    else
                    {

                        barrier.IsPositionFrozen = false;
                    }
                    if (Settings.EnableAdvancedBarricadeOptions)
                    {
                        Rage.Native.NativeFunction.Natives.x971DA0055324D033(barrier, barrierTexture.Value);
                        if (setBarrierLights.Checked)
                        {
                            Rage.Native.NativeFunction.Natives.SET_ENTITY_LIGHTS(barrier, false);
                        }
                        else
                        {
                            Rage.Native.NativeFunction.Natives.SET_ENTITY_LIGHTS(barrier, true);
                        }

                        //Rage.Native.NativeFunction.Natives.SET_ENTITY_TRAFFICLIGHT_OVERRIDE(barrier, setBarrierTrafficLight.Index);
                        barrier.IsPositionFrozen = true;
                        GameFiber.Sleep(50);
                        if (barrier && !immobile.Checked)
                        {
                            barrier.IsPositionFrozen = false;
                        }
                    }
                    barriers.Add(new Barrier(barrier, barrier.Position, barrier.Heading, invincible.Checked, immobile.Checked));
                    //if (barriers.First().Object == barrier)
                    //{
                    //    barriers.First().GoAround();
                    //}
                    removeBarrierOptions.Enabled = true;
                    resetBarriers.Enabled = true;
                }
            }, "Scene Manager Spawn Barrier Fiber");
            

            void SpawnFlare()
            {
                var flare = new Weapon("weapon_flare", shadowBarrier.Position, 1);
                Rage.Native.NativeFunction.Natives.SET_ENTITY_DYNAMIC(flare, true);
                GameFiber.Sleep(1);
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

                barriers.Add(new Barrier(flare, flare.Position, flare.Heading, invincible.Checked, immobile.Checked));
                removeBarrierOptions.Enabled = true;
            }
        }

        internal static void RotateBarrier()
        {
            shadowBarrier.Heading = rotateBarrier.Value;
            shadowBarrier.Position = MousePositionInWorld.GetPositionForBarrier;
            Rage.Native.NativeFunction.Natives.PLACE_OBJECT_ON_GROUND_PROPERLY(shadowBarrier);
        }

        private static void RemoveBarrier()
        {
            switch (removeBarrierOptions.Index)
            {
                case 0:
                    barriers[barriers.Count - 1].Object.Delete();
                    barriers.RemoveAt(barriers.Count - 1);
                    break;
                case 1:
                    var nearestBarrier = barriers.OrderBy(b => b.Object.DistanceTo2D(Game.LocalPlayer.Character)).FirstOrDefault();
                    if (nearestBarrier != null)
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

        private static void ResetBarriers()
        {
            GameFiber.StartNew(() =>
            {
                var currentBarriers = barriers.Where(b => b.Model.Name != "0xa2c44e80").ToList(); // 0xa2c44e80 is the flare weapon hash
                foreach (Barrier barrier in currentBarriers)
                {
                    var newBarrier = new Object(barrier.Model, barrier.Position, barrier.Rotation);
                    newBarrier.SetPositionWithSnap(barrier.Position);
                    Rage.Native.NativeFunction.Natives.SET_ENTITY_DYNAMIC(newBarrier, true);
                    newBarrier.IsPositionFrozen = barrier.Immobile;
                    if (barrier.Invincible)
                    {
                        Rage.Native.NativeFunction.Natives.SET_DISABLE_FRAG_DAMAGE(newBarrier, true);
                        if (newBarrier.Model.Name != "prop_barrier_wat_03a")
                        {
                            Rage.Native.NativeFunction.Natives.SET_DISABLE_BREAKING(newBarrier, true);
                        }
                    }
                    //Rage.Native.NativeFunction.Natives.SET_ENTITY_TRAFFICLIGHT_OVERRIDE(newBarrier, setBarrierTrafficLight.Index);
                    newBarrier.IsPositionFrozen = true;
                    GameFiber.Sleep(50);
                    if (newBarrier && !barrier.Immobile)
                    {
                        newBarrier.IsPositionFrozen = false;
                    }
                    barriers.Add(new Barrier(newBarrier, newBarrier.Position, newBarrier.Heading, barrier.Invincible, barrier.Immobile));

                    if (barrier.Object)
                    {
                        barrier.Object.Delete();
                    }
                    barriers.Remove(barrier);
                }
                currentBarriers.Clear();
            });
            
        }

        private static void SetBarrierLights()
        {
            if (setBarrierLights.Checked)
            {
                Rage.Native.NativeFunction.Natives.SET_ENTITY_LIGHTS(shadowBarrier, false);
            }
            else
            {
                Rage.Native.NativeFunction.Natives.SET_ENTITY_LIGHTS(shadowBarrier, true);
            }

            //Rage.Native.NativeFunction.Natives.SET_ENTITY_TRAFFICLIGHT_OVERRIDE(shadowBarrier, setBarrierTrafficLight.Index);
        }

        private static void BarrierMenu_OnCheckboxChanged(UIMenu sender, UIMenuCheckboxItem checkbox, bool @checked)
        {
            if(checkbox == setBarrierLights)
            {
                SetBarrierLights();
            }
        }

        private static void BarrierMenu_OnScrollerChanged(UIMenu sender, UIMenuScrollerItem scrollerItem, int oldIndex, int newIndex)
        {
            if (scrollerItem == barrierList)
            {
                if (shadowBarrier)
                {
                    shadowBarrier.Delete();
                }
                barrierTexture.Index = 0;

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

            if (scrollerItem == barrierTexture)
            {
                Rage.Native.NativeFunction.Natives.x971DA0055324D033(shadowBarrier, barrierTexture.Value);
            }

            if (scrollerItem == setBarrierTrafficLight)
            {
                Rage.Native.NativeFunction.Natives.SET_ENTITY_TRAFFICLIGHT_OVERRIDE(shadowBarrier, setBarrierTrafficLight.Index);
            }

            if (scrollerItem == rotateBarrier)
            {
                RotateBarrier();
            }
        }

        private static void BarrierMenu_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == barrierList)
            {
                SpawnBarrier();
            }

            if (selectedItem == removeBarrierOptions)
            {
                RemoveBarrier();
            }
            
            if (selectedItem == resetBarriers)
            {
                ResetBarriers();
            }
        }

        private static void BarrierMenu_OnMenuOpen(UIMenu menu)
        {
            var scrollerItems = new List<UIMenuScrollerItem> { barrierList, barrierTexture, setBarrierTrafficLight, rotateBarrier, removeBarrierOptions };

            Hints.Display($"~o~Scene Manager ~y~[Hint]\n~w~The shadow barrier will disappear if you aim too far away.");
            CreateShadowBarrier();

            GameFiber ShadowConeLoopFiber = new GameFiber(() => LoopToDisplayShadowBarrier());
            ShadowConeLoopFiber.Start();

            RNUIMouseInputHandler.Initialize(menu, scrollerItems);      
        }

        internal static float SetMenuWidth()
        {
            float defaultWidth = UIMenu.DefaultWidth;

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
