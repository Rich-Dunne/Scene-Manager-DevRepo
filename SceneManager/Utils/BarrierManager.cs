using System.Collections.Generic;
using System.Linq;
using Rage;
using SceneManager.Menus;
using SceneManager.Objects;

namespace SceneManager.Utils
{
    internal static class BarrierManager
    {
        internal static Object PlaceholderBarrier { get; private set; }
        internal static List<Barrier> Barriers { get; } = new List<Barrier>();

        internal static void CreatePlaceholderBarrier()
        {
            Hints.Display($"~o~Scene Manager ~y~[Hint]\n~w~The shadow barrier will disappear if you aim too far away.");
            if (PlaceholderBarrier)
            {
                PlaceholderBarrier.Delete();
            }

            var barrierKey = Settings.Barriers.Where(x => x.Key == BarrierMenu.BarrierList.SelectedItem).FirstOrDefault().Key;
            var barrierValue = Settings.Barriers[barrierKey].Name;
            PlaceholderBarrier = new Object(barrierValue, UserInput.GetMousePositionForBarrier, BarrierMenu.RotateBarrier.Value);
            if (!PlaceholderBarrier)
            {
                BarrierMenu.Menu.Close();
                Game.LogTrivial($"Something went wrong creating the placeholder barrier.  Mouse position: {UserInput.GetMousePositionForBarrier}");
                Game.DisplayNotification($"~o~Scene Manager ~r~[Error]\n~w~Something went wrong creating the placeholder barrier.  This is a rare problem that only happens in certain areas of the world.  Please try again somewhere else.");
                return;
            }

            //Rage.Native.NativeFunction.Natives.SET_ENTITY_TRAFFICLIGHT_OVERRIDE(shadowBarrier, setBarrierTrafficLight.Index);
            Rage.Native.NativeFunction.Natives.PLACE_OBJECT_ON_GROUND_PROPERLY(PlaceholderBarrier);
            PlaceholderBarrier.IsGravityDisabled = true;
            PlaceholderBarrier.IsCollisionEnabled = false;
            PlaceholderBarrier.Opacity = 0.7f;

            // Start with lights off for Parks's objects
            if (Settings.EnableAdvancedBarricadeOptions)
            {
                Rage.Native.NativeFunction.Natives.x971DA0055324D033(PlaceholderBarrier, BarrierMenu.BarrierTexture.Value); // _SET_OBJECT_TEXTURE_VARIATION
                SetBarrierLights();
            }
        }

        internal static void SetBarrierLights()
        {
            if (BarrierMenu.SetBarrierLights.Checked)
            {
                Rage.Native.NativeFunction.Natives.SET_ENTITY_LIGHTS(PlaceholderBarrier, false);
            }
            else
            {
                Rage.Native.NativeFunction.Natives.SET_ENTITY_LIGHTS(PlaceholderBarrier, true);
            }

            //Rage.Native.NativeFunction.Natives.SET_ENTITY_TRAFFICLIGHT_OVERRIDE(shadowBarrier, setBarrierTrafficLight.Index);
        }

        internal static void UpdatePlaceholderBarrierPosition()
        {
            DisableBarrierMenuOptionsIfShadowConeTooFar();
            if (PlaceholderBarrier)
            {
                PlaceholderBarrier.Heading = BarrierMenu.RotateBarrier.Value;
                PlaceholderBarrier.Position = UserInput.GetMousePositionForBarrier;
                Rage.Native.NativeFunction.Natives.PLACE_OBJECT_ON_GROUND_PROPERLY(PlaceholderBarrier);
                //Rage.Native.NativeFunction.Natives.SET_ENTITY_TRAFFICLIGHT_OVERRIDE(shadowBarrier, setBarrierTrafficLight.Index);
            }

            void DisableBarrierMenuOptionsIfShadowConeTooFar()
            {
                if (!PlaceholderBarrier && UserInput.GetMousePositionForBarrier.DistanceTo2D(Game.LocalPlayer.Character.Position) <= Settings.BarrierPlacementDistance)
                {
                    CreatePlaceholderBarrier();

                }
                else if (PlaceholderBarrier && PlaceholderBarrier.Position.DistanceTo2D(Game.LocalPlayer.Character.Position) > Settings.BarrierPlacementDistance)
                {
                    BarrierMenu.BarrierList.Enabled = false;
                    BarrierMenu.RotateBarrier.Enabled = false;
                    PlaceholderBarrier.Delete();
                }
                else if (PlaceholderBarrier && PlaceholderBarrier.Position.DistanceTo2D(Game.LocalPlayer.Character.Position) <= Settings.BarrierPlacementDistance && BarrierMenu.BarrierList.SelectedItem == "Flare")
                {
                    BarrierMenu.BarrierList.Enabled = true;
                    BarrierMenu.RotateBarrier.Enabled = false;
                }
                else
                {
                    BarrierMenu.BarrierList.Enabled = true;
                    BarrierMenu.RotateBarrier.Enabled = true;
                }
            }
        }

        internal static void LoopToRenderPlaceholderBarrier()
        {
            while (BarrierMenu.Menu.Visible)
            {
                if (BarrierMenu.BarrierList.Selected || BarrierMenu.RotateBarrier.Selected || BarrierMenu.Invincible.Selected || BarrierMenu.Immobile.Selected || BarrierMenu.BarrierTexture.Selected || BarrierMenu.SetBarrierLights.Selected || BarrierMenu.SetBarrierTrafficLight.Selected)
                {
                    if (PlaceholderBarrier)
                    {
                        //UpdatePlaceholderBarrierPosition();
                        UpdatePlaceholderBarrierPosition();
                    }
                    else if (UserInput.GetMousePositionForBarrier.DistanceTo2D(Game.LocalPlayer.Character.Position) <= Settings.BarrierPlacementDistance)
                    {
                        //CreatePlaceholderBarrier();
                        CreatePlaceholderBarrier();
                    }
                }
                else
                {
                    if (PlaceholderBarrier)
                    {
                        PlaceholderBarrier.Delete();
                    }
                }
                GameFiber.Yield();
            }

            if (PlaceholderBarrier)
            {
                PlaceholderBarrier.Delete();
            }
        }

        internal static void SpawnBarrier()
        {
            if (BarrierMenu.BarrierList.SelectedItem == "Flare")
            {
                SpawnFlare();
            }
            else
            {
                var barrier = new Barrier(PlaceholderBarrier, PlaceholderBarrier.Position, BarrierMenu.RotateBarrier.Value, BarrierMenu.Invincible.Checked, BarrierMenu.Immobile.Checked, BarrierMenu.BarrierTexture.Value, BarrierMenu.SetBarrierLights.Checked);
                Barriers.Add(barrier);

                BarrierMenu.RemoveBarrierOptions.Enabled = true;
                BarrierMenu.ResetBarriers.Enabled = true;
            }

            void SpawnFlare()
            {
                var flare = new Weapon("weapon_flare", PlaceholderBarrier.Position, 1);
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
                }, "Spawn Flare Fiber");

                Barriers.Add(new Barrier(flare, flare.Position, flare.Heading, BarrierMenu.Invincible.Checked, BarrierMenu.Immobile.Checked));
                BarrierMenu.RemoveBarrierOptions.Enabled = true;
            }
        }

        internal static void RemoveBarrier(int removeBarrierOptionsIndex)
        {
            switch (removeBarrierOptionsIndex)
            {
                case 0:
                    Barriers[Barriers.Count - 1].Delete();
                    Barriers.RemoveAt(Barriers.Count - 1);
                    break;
                case 1:
                    var nearestBarrier = Barriers.OrderBy(b => b.DistanceTo2D(Game.LocalPlayer.Character)).FirstOrDefault();
                    if (nearestBarrier != null)
                    {
                        nearestBarrier.Delete();
                        Barriers.Remove(nearestBarrier);
                    }
                    break;
                case 2:
                    foreach (Barrier b in Barriers)
                    {
                        b.Delete();
                    }
                    if (Barriers.Count > 0)
                    {
                        Barriers.Clear();
                    }
                    break;
            }

            BarrierMenu.RemoveBarrierOptions.Enabled = Barriers.Count == 0 ? false : true;
            BarrierMenu.ResetBarriers.Enabled = Barriers.Count == 0 ? false : true;
        }

        internal static void ResetBarriers()
        {
            GameFiber.StartNew(() =>
            {
                var currentBarriers = Barriers.Where(b => b.Model.Name != "0xa2c44e80").ToList(); // 0xa2c44e80 is the flare weapon hash
                foreach (Barrier barrier in currentBarriers)
                {
                    Barriers.Add(new Barrier(barrier, barrier.SpawnPosition, barrier.SpawnHeading, barrier.Invincible, barrier.Immobile, barrier.TextureVariation, barrier.LightsEnabled));

                    if (barrier)
                    {
                        barrier.Delete();
                    }
                    Barriers.Remove(barrier);
                }
                currentBarriers.Clear();
            }, "Barrier Reset Fiber");

        }

        internal static void RotateBarrier()
        {
            PlaceholderBarrier.Heading = BarrierMenu.RotateBarrier.Value;
            PlaceholderBarrier.Position = UserInput.GetMousePositionForBarrier;
            Rage.Native.NativeFunction.Natives.PLACE_OBJECT_ON_GROUND_PROPERLY(PlaceholderBarrier);
        }
    }
}
