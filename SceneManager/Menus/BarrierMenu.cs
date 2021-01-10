using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using SceneManager.Objects;
using SceneManager.Utils;

namespace SceneManager.Menus
{
    class BarrierMenu
    {
        private static List<TrafficLight> TrafficLightList { get; } = new List<TrafficLight>() { TrafficLight.Green, TrafficLight.Red, TrafficLight.Yellow, TrafficLight.None };
        internal static UIMenu Menu { get; } = new UIMenu("Scene Manager", "~o~Barrier Management");
        internal static UIMenuListScrollerItem<string> BarrierList { get; } = new UIMenuListScrollerItem<string>("Spawn Barrier", "", Settings.Barriers.Keys); // Settings.barrierKeys
        internal static UIMenuNumericScrollerItem<int> RotateBarrier { get; } = new UIMenuNumericScrollerItem<int>("Rotate Barrier", "", 0, 350, 10);
        // ADD CHECKBOX FOR BARRIER TO STOP TRAFFIC?  ADD 3D MARKER TO SHOW WHERE TRAFFIC WILL STOP.  ONLY NEED ONE CONE TO DO IT PER LANE
        internal static UIMenuCheckboxItem Invincible { get; } = new UIMenuCheckboxItem("Indestructible", false);
        internal static UIMenuCheckboxItem Immobile { get; } = new UIMenuCheckboxItem("Immobile", false);
        internal static UIMenuNumericScrollerItem<int> BarrierTexture { get; } = new UIMenuNumericScrollerItem<int>("Change Texture", "", 0, 15, 1);
        internal static UIMenuCheckboxItem SetBarrierLights { get; } = new UIMenuCheckboxItem("Enable Barrier Lights", Settings.EnableBarrierLightsDefaultOn);
        internal static UIMenuListScrollerItem<TrafficLight> SetBarrierTrafficLight { get; } = new UIMenuListScrollerItem<TrafficLight>("Set Barrier Traffic Light", "", TrafficLightList);
        internal static UIMenuListScrollerItem<string> RemoveBarrierOptions { get; } = new UIMenuListScrollerItem<string>("Remove Barrier", "", new[] { "Last Barrier", "Nearest Barrier", "All Barriers" });
        internal static UIMenuItem ResetBarriers { get; } = new UIMenuItem("Reset Barriers", "Reset all spawned barriers to their original position and rotation");

        internal static void Initialize()
        {
            Menu.ParentMenu = MainMenu.Menu;
            MenuManager.MenuPool.Add(Menu);

            Menu.OnItemSelect += BarrierMenu_OnItemSelected;
            Menu.OnScrollerChange += BarrierMenu_OnScrollerChanged;
            Menu.OnCheckboxChange += BarrierMenu_OnCheckboxChanged;
            Menu.OnMenuOpen += BarrierMenu_OnMenuOpen;
        }

        internal static void BuildBarrierMenu()
        {
            Menu.AddItem(BarrierList);
            BarrierList.ForeColor = Color.Gold;

            Menu.AddItem(RotateBarrier);
            Menu.AddItem(Invincible);
            Menu.AddItem(Immobile);

            if (Settings.EnableAdvancedBarricadeOptions)
            {
                Menu.AddItem(BarrierTexture);
                BarrierTexture.Index = 0;

                Menu.AddItem(SetBarrierLights);

                //barrierMenu.AddItem(setBarrierTrafficLight);
                //setBarrierTrafficLight.Index = 3;
            }

            Menu.AddItem(RemoveBarrierOptions);
            RemoveBarrierOptions.ForeColor = Color.Gold;
            RemoveBarrierOptions.Enabled = false;

            Menu.AddItem(ResetBarriers);
            ResetBarriers.ForeColor = Color.Gold;
            ResetBarriers.Enabled = false;    
        }

        internal static void ScrollBarrierList()
        {
            if (BarrierManager.PlaceholderBarrier)
            {
                BarrierManager.PlaceholderBarrier.Delete();
            }
            BarrierTexture.Index = 0;

            if (BarrierList.SelectedItem == "Flare")
            {
                RotateBarrier.Enabled = false;
            }
            else
            {
                RotateBarrier.Enabled = true;
            }

            Menu.Width = SetMenuWidth();
        }

        private static void BarrierMenu_OnCheckboxChanged(UIMenu sender, UIMenuCheckboxItem checkbox, bool @checked)
        {
            if(checkbox == SetBarrierLights)
            {
                //SetBarrierLights();
                BarrierManager.SetBarrierLights();
            }
        }

        private static void BarrierMenu_OnScrollerChanged(UIMenu sender, UIMenuScrollerItem scrollerItem, int oldIndex, int newIndex)
        {
            if (scrollerItem == BarrierList)
            {
                ScrollBarrierList();
            }

            if (scrollerItem == BarrierTexture)
            {
                Rage.Native.NativeFunction.Natives.x971DA0055324D033(BarrierManager.PlaceholderBarrier, BarrierTexture.Value);
            }

            if (scrollerItem == SetBarrierTrafficLight)
            {
                Rage.Native.NativeFunction.Natives.SET_ENTITY_TRAFFICLIGHT_OVERRIDE(BarrierManager.PlaceholderBarrier, SetBarrierTrafficLight.Index);
            }

            if (scrollerItem == RotateBarrier)
            {
                //RotateBarrier();
                BarrierManager.RotateBarrier();
            }
        }

        private static void BarrierMenu_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == BarrierList)
            {
                //SpawnBarrier();
                BarrierManager.SpawnBarrier();
            }

            if (selectedItem == RemoveBarrierOptions)
            {
                //RemoveBarrier();
                BarrierManager.RemoveBarrier(RemoveBarrierOptions.Index);
            }
            
            if (selectedItem == ResetBarriers)
            {
                //ResetBarriers();
                BarrierManager.ResetBarriers();
            }
        }

        private static void BarrierMenu_OnMenuOpen(UIMenu menu)
        {
            var scrollerItems = new List<UIMenuScrollerItem> { BarrierList, BarrierTexture, SetBarrierTrafficLight, RotateBarrier, RemoveBarrierOptions };

            //CreatePlaceholderBarrier();
            BarrierManager.CreatePlaceholderBarrier();

            //GameFiber PlaceholderBarrierRenderFiber = new GameFiber(() => LoopToDisplayShadowBarrier());
            GameFiber PlaceholderBarrierRenderFiber = new GameFiber(() => BarrierManager.LoopToRenderPlaceholderBarrier(), "Render Placeholder Barrier Loop Fiber");
            PlaceholderBarrierRenderFiber.Start();

            GameFiber.StartNew(() => UserInput.InitializeMenuMouseControl(menu, scrollerItems), "RNUI Mouse Input Fiber");
        }

        private static float SetMenuWidth()
        {
            float defaultWidth = UIMenu.DefaultWidth;

            BarrierList.TextStyle.Apply();
            Rage.Native.NativeFunction.Natives.x54CE8AC98E120CAB("STRING"); // _BEGIN_TEXT_COMMAND_GET_WIDTH
            Rage.Native.NativeFunction.Natives.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME(BarrierList.SelectedItem);
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

        internal static void Cleanup()
        {
            foreach (Barrier barrier in BarrierManager.Barriers.Where(x => x.Object))
            {
                barrier.Object.Delete();
            }
            if (BarrierManager.PlaceholderBarrier)
            {
                BarrierManager.PlaceholderBarrier.Delete();
            }
        }
    }
}
