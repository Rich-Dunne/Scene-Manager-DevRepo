using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    class SettingsMenu
    {
        public static UIMenu settingsMenu { get; private set; }
        public static UIMenuCheckboxItem debugGraphics = new UIMenuCheckboxItem("Enable 3D Waypoints", false), 
            hints = new UIMenuCheckboxItem("Enable Hints", true);  // Refactor this to be true/false based off the ini
        public static UIMenuListScrollerItem<SpeedUnitsOfMeasure> speedUnits = new UIMenuListScrollerItem<SpeedUnitsOfMeasure>("Speed Unit of Measure", "", new[] { SpeedUnitsOfMeasure.MPH, SpeedUnitsOfMeasure.KPH });
        public static UIMenuNumericScrollerItem<int> barrierPlacementDistance = new UIMenuNumericScrollerItem<int>("Barrier Placement Distance", "How far away you can place a barrier (in meters)", 1, 30, 1);

        public enum SpeedUnitsOfMeasure
        {
            MPH,
            KPH
        }

        internal static void InstantiateMenu()
        {
            settingsMenu = new UIMenu("Scene Menu", "~o~Plugin Settings");
            settingsMenu.ParentMenu = MainMenu.mainMenu;
            MenuManager.menuPool.Add(settingsMenu);
        }

        public static void BuildSettingsMenu()
        {
            settingsMenu.AddItem(debugGraphics);
            settingsMenu.AddItem(hints);
            settingsMenu.AddItem(speedUnits);
            settingsMenu.AddItem(barrierPlacementDistance);
            barrierPlacementDistance.Index = 14;

            settingsMenu.OnCheckboxChange += SettingsMenu_OnCheckboxChange;
            settingsMenu.OnScrollerChange += SettingsMenu_OnScrollerChange;
        }

        private static void SettingsMenu_OnCheckboxChange(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            if (checkboxItem == debugGraphics)
            {
                if (debugGraphics.Checked)
                {
                    foreach (Path path in PathMainMenu.GetPaths())
                    {
                        GameFiber.StartNew(() =>
                        {
                            DebugGraphics.LoopToDrawDebugGraphics(path);
                        });
                    }

                    DebugGraphics.Draw3DWaypointOnPlayer();
                }
            }

            if(checkboxItem == hints)
            {
                Hints.Enabled = hints.Checked ? true : false;
                // Update the setting in the .ini when check state is changed
            }
        }

        private static void SettingsMenu_OnScrollerChange(UIMenu sender, UIMenuScrollerItem scrollerItem, int oldIndex, int newIndex)
        {
            if (scrollerItem == speedUnits)
            {
                // Clear the menu and rebuild it to reflect the menu item text change
                PathCreationMenu.pathCreationMenu.Clear();
                PathCreationMenu.BuildPathCreationMenu();
            }
        }
    }
}