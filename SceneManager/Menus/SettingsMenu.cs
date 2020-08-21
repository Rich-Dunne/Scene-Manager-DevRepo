using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System.Drawing;

namespace SceneManager
{
    class SettingsMenu
    {
        public static UIMenu settingsMenu { get; private set; }
        public static UIMenuCheckboxItem debugGraphics;
        public static UIMenuListScrollerItem<SpeedUnitsOfMeasure> speedUnits;
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
            settingsMenu.AddItem(debugGraphics = new UIMenuCheckboxItem("Enable Debug Graphics", false));
            settingsMenu.AddItem(speedUnits = new UIMenuListScrollerItem<SpeedUnitsOfMeasure>("Speed Unit of Measure", "", new[] { SpeedUnitsOfMeasure.MPH, SpeedUnitsOfMeasure.KPH }));

            settingsMenu.OnCheckboxChange += SettingsMenu_OnCheckboxChange;
            settingsMenu.OnScrollerChange += SettingsMenu_OnScrollerChange;
        }

        private static void SettingsMenu_OnCheckboxChange(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            if (checkboxItem == debugGraphics)
            {
                // TODO: Fix graphics don't display when new path is created, have to uncheck and re-check the option
                // TODO: Add branch for this during path creation ... create temp Waypoint list during path creation, then assign to path[i] after creation?
                if (debugGraphics.Checked)
                {
                    foreach (Path path in PathMainMenu.GetPaths())
                    {
                        GameFiber.StartNew(() =>
                        {
                            DebugGraphics.LoopToDrawDebugGraphics(debugGraphics, path);
                        });
                    }
                }
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