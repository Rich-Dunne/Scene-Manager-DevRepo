using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System.Drawing;

namespace SceneManager
{
    class SettingsMenu
    {
        public static UIMenuCheckboxItem debugGraphics;
        public static UIMenuListScrollerItem<SpeedUnitsOfMeasure> speedUnits;
        public enum SpeedUnitsOfMeasure
        {
            MPH,
            KPH
        }

        public static void BuildSettingsMenu()
        {
            MenuManager.settingsMenu.AddItem(debugGraphics = new UIMenuCheckboxItem("Enable Debug Graphics", false));
            MenuManager.settingsMenu.AddItem(speedUnits = new UIMenuListScrollerItem<SpeedUnitsOfMeasure>("Speed Unit of Measure", "", new[] { SpeedUnitsOfMeasure.MPH, SpeedUnitsOfMeasure.KPH }));

            MenuManager.settingsMenu.OnCheckboxChange += SettingsMenu_OnCheckboxChange;
            MenuManager.settingsMenu.OnScrollerChange += SettingsMenu_OnScrollerChange;
        }

        private static void SettingsMenu_OnCheckboxChange(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            if (checkboxItem == debugGraphics)
            {
                // TODO: Fix graphics don't display when new path is created, have to uncheck and re-check the option
                // TODO: Add branch for this during path creation ... create temp Waypoint list during path creation, then assign to path[i] after creation?
                if (debugGraphics.Checked)
                {
                    foreach (Path path in TrafficMenu.paths)
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
                MenuManager.pathCreationMenu.Clear();
                PathCreationMenu.BuildPathCreationMenu();
            }
        }
    }
}