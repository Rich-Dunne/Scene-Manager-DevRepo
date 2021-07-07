using System.Collections.Generic;
using System.Linq;
using Rage;
using RAGENativeUI;
using SceneManager.Barriers;
using SceneManager.Menus;
using SceneManager.Paths;
using SceneManager.Utils;

namespace SceneManager.Managers
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

            var barrierKey = Settings.BarrierModels.Where(x => x.Key == BarrierMenu.BarrierList.SelectedItem).FirstOrDefault().Key;
            var barrierValue = Settings.BarrierModels[barrierKey].Name;
            PlaceholderBarrier = new Object(barrierValue, UserInput.PlayerMousePositionForBarrier, BarrierMenu.RotateBarrier.Value);
            if (!PlaceholderBarrier)
            {
                BarrierMenu.Menu.Close();
                Game.LogTrivial($"Something went wrong creating the placeholder barrier.  Mouse position: {UserInput.PlayerMousePositionForBarrier}");
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
            DisableBarrierMenuOptionsIfPlaceholderTooFar();
            if (PlaceholderBarrier)
            {
                PlaceholderBarrier.Heading = BarrierMenu.RotateBarrier.Value;
                PlaceholderBarrier.Position = UserInput.PlayerMousePositionForBarrier;
                Rage.Native.NativeFunction.Natives.PLACE_OBJECT_ON_GROUND_PROPERLY<bool>(PlaceholderBarrier);
                //Rage.Native.NativeFunction.Natives.SET_ENTITY_TRAFFICLIGHT_OVERRIDE(shadowBarrier, setBarrierTrafficLight.Index);
            }

            void DisableBarrierMenuOptionsIfPlaceholderTooFar()
            {
                if (!PlaceholderBarrier && UserInput.PlayerMousePositionForBarrier.DistanceTo2D(Game.LocalPlayer.Character.Position) <= Settings.BarrierPlacementDistance)
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
                        UpdatePlaceholderBarrierPosition();
                    }
                    else if (UserInput.PlayerMousePositionForBarrier.DistanceTo2D(Game.LocalPlayer.Character.Position) <= Settings.BarrierPlacementDistance)
                    {
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
            Barrier barrier;

            if (BarrierMenu.BarrierList.SelectedItem == "Flare")
            {
                SpawnFlare();
            }
            else
            {
                barrier = new Barrier(PlaceholderBarrier, BarrierMenu.Invincible.Checked, BarrierMenu.Immobile.Checked, BarrierMenu.BarrierTexture.Value, BarrierMenu.SetBarrierLights.Checked);
                Barriers.Add(barrier);

                BarrierMenu.AddUnassignedToPath.Enabled = true;
                BarrierMenu.RemoveBarrierOptions.Enabled = true;
                BarrierMenu.ResetBarriers.Enabled = true;
            }

            if (barrier != null && BarrierMenu.BelongsToPath.Checked)
            {
                var matchingPath = PathManager.Paths.FirstOrDefault(x => x.Name == BarrierMenu.AddToPath.OptionText);
                if(matchingPath != null)
                {
                    matchingPath.Barriers.Add(barrier);
                }
            }

            void SpawnFlare()
            {
                var flare = new Weapon("weapon_flare", PlaceholderBarrier.Position, 1);
                Rage.Native.NativeFunction.Natives.SET_ENTITY_DYNAMIC(flare, true);

                barrier = new Barrier(flare, BarrierMenu.Invincible.Checked, BarrierMenu.Immobile.Checked);
                Barriers.Add(barrier);

                BarrierMenu.AddUnassignedToPath.Enabled = true;
                BarrierMenu.RemoveBarrierOptions.Enabled = true;
                BarrierMenu.ResetBarriers.Enabled = true;
            }
        }

        internal static void RemoveBarrier(int removeBarrierOptionsIndex)
        {
            Path path;
            switch (removeBarrierOptionsIndex)
            {
                case 0:
                    var barrierToRemove = Barriers[Barriers.Count - 1];
                    path = PathManager.Paths.FirstOrDefault(x => x != null && x.Barriers.Contains(barrierToRemove));
                    if(path != null)
                    {
                        path.Barriers.Remove(barrierToRemove);
                    }

                    barrierToRemove.Delete();
                    Barriers.RemoveAt(Barriers.Count - 1);
                    break;
                case 1:
                    var nearestBarrier = Barriers.OrderBy(b => b.DistanceTo2D(Game.LocalPlayer.Character)).FirstOrDefault();
                    if (nearestBarrier != null)
                    {
                        path = PathManager.Paths.FirstOrDefault(x => x != null && x.Barriers.Contains(nearestBarrier));
                        if (path != null)
                        {
                            path.Barriers.Remove(nearestBarrier);
                        }

                        nearestBarrier.Delete();
                        Barriers.Remove(nearestBarrier);
                    }
                    break;
                case 2:
                    foreach (Barrier barrier in Barriers)
                    {
                        if(barrier.Path != null)
                        {
                            barrier.Path.Barriers.Remove(barrier);
                        }

                        barrier.Delete();
                    }
                    if (Barriers.Count > 0)
                    {
                        Barriers.Clear();
                    }
                    break;
            }

            BarrierMenu.AddUnassignedToPath.Enabled = Barriers.Any(x => x.Path == null);
            BarrierMenu.RemoveBarrierOptions.Enabled = Barriers.Count != 0;
            BarrierMenu.ResetBarriers.Enabled = Barriers.Count != 0;
        }

        internal static void ResetBarriers()
        {
            //GameFiber.StartNew(() =>
            //{
                Game.DisplayHelp($"~{InstructionalKey.SymbolBusySpinner.GetId()}~ Resetting barriers...");
                var currentBarriers = Barriers.Where(b => b.ModelName != "0xa2c44e80").ToList(); // 0xa2c44e80 is the flare weapon hash
                foreach (Barrier barrier in currentBarriers)
                {
                    var newBarrier = new Barrier(barrier, barrier.Invincible, barrier.Immobile, barrier.TextureVariation, barrier.LightsEnabled);
                    Barriers.Add(newBarrier);

                    //barrier.Delete();
                    Barriers.Remove(barrier);
                }
                currentBarriers.Clear();
            //}, "Barrier Reset Fiber");

        }

        internal static void RotateBarrier()
        {
            PlaceholderBarrier.Heading = BarrierMenu.RotateBarrier.Value;
            PlaceholderBarrier.Position = UserInput.PlayerMousePositionForBarrier;
            Rage.Native.NativeFunction.Natives.PLACE_OBJECT_ON_GROUND_PROPERLY(PlaceholderBarrier);
        }

        internal static void AddBarrierToPath()
        {
            var unassignedBarriers = Barriers.Where(x => x.Path == null);
            if(unassignedBarriers.Count() == 0)
            {
                Game.LogTrivial($"There are no unassigned barriers.");
                return;
            }

            var pathToAssignTo = PathManager.Paths.First(x => x.Name == BarrierMenu.AddUnassignedToPath.OptionText);
            foreach (Barrier barrier in unassignedBarriers)
            {
                pathToAssignTo.Barriers.Add(barrier);
            }

            Game.LogTrivial($"Added {unassignedBarriers.Count()} unassigned barrier to {pathToAssignTo.Name}");
            Game.DisplayNotification($"~o~Scene Manager ~y~[Barriers]\n~w~Assigned ~b~{unassignedBarriers.Count()} ~w~barrier(s) to ~b~{pathToAssignTo.Name}~w~.");

            BarrierMenu.AddUnassignedToPath.Enabled = false;
        }
    }
}
