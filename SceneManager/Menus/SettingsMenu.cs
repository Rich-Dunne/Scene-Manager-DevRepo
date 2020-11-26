using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SceneManager
{
    class SettingsMenu
    {
        internal static UIMenu settingsMenu = new UIMenu("Scene Manager", "~o~Plugin Settings");
        internal static UIMenuCheckboxItem threeDWaypoints = new UIMenuCheckboxItem("Enable 3D Waypoints", Settings.Enable3DWaypoints),
            mapBlips = new UIMenuCheckboxItem("Enable Map Blips", Settings.EnableMapBlips),
            hints = new UIMenuCheckboxItem("Enable Hints", Settings.EnableHints);
        private static SpeedUnits[] speedArray = {SpeedUnits.MPH, SpeedUnits.KPH };
        internal static UIMenuListScrollerItem<SpeedUnits> speedUnits = new UIMenuListScrollerItem<SpeedUnits>("Speed Unit of Measure", "", new[] { SpeedUnits.MPH, SpeedUnits.KPH });
        internal static UIMenuItem saveSettings = new UIMenuItem("Save settings to .ini", "Updates the plugin's .ini file with the current settings so the next time the plugin is loaded, it will use these settings.");

        internal static void InstantiateMenu()
        {
            settingsMenu.ParentMenu = MainMenu.mainMenu;
            MenuManager.menuPool.Add(settingsMenu);
            settingsMenu.OnCheckboxChange += SettingsMenu_OnCheckboxChange;
            settingsMenu.OnScrollerChange += SettingsMenu_OnScrollerChange;
            settingsMenu.OnItemSelect += SettingsMenu_OnItemSelected;
            settingsMenu.OnMenuOpen += SettingsMenu_OnMenuOpen;
        }

        internal static void BuildSettingsMenu()
        {
            settingsMenu.AddItem(threeDWaypoints);
            settingsMenu.AddItem(mapBlips);
            settingsMenu.AddItem(hints);
            settingsMenu.AddItem(speedUnits);
            speedUnits.Index = Array.IndexOf(speedArray, Settings.SpeedUnit);
            settingsMenu.AddItem(saveSettings);
            saveSettings.ForeColor = System.Drawing.Color.Gold;
        }
        
        private static void ToggleMapBlips()
        {
            if (mapBlips.Checked)
            {
                foreach (Path path in PathMainMenu.paths)
                {
                    foreach (Waypoint wp in path.Waypoints)
                    {
                        wp.EnableBlip();
                    }
                }
            }
            else
            {
                foreach (Path path in PathMainMenu.paths)
                {
                    foreach (Waypoint wp in path.Waypoints)
                    {
                        wp.DisableBlip();
                    }
                }
            }
        }

        private static void ToggleHints()
        {
            Hints.Enabled = hints.Checked ? true : false;
        }

        private static void ToggleSettings()
        {
            Settings.UpdateSettings(threeDWaypoints.Checked, mapBlips.Checked, hints.Checked, speedUnits.SelectedItem);
            Game.DisplayHelp($"Scene Manager settings saved");
        }

        private static void SettingsMenu_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if(selectedItem == saveSettings)
            {
                Settings.UpdateSettings(threeDWaypoints.Checked, mapBlips.Checked, hints.Checked, speedUnits.SelectedItem);
                Game.DisplayHelp($"Scene Manager settings saved");
            }
        }

        private static void SettingsMenu_OnCheckboxChange(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            if (checkboxItem == mapBlips)
            {
                ToggleMapBlips();
            }

            if (checkboxItem == hints)
            {
                Hints.Enabled = hints.Checked ? true : false;
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

        private static void SettingsMenu_OnMenuOpen(UIMenu menu)
        {
            var scrollerItems = new List<UIMenuScrollerItem> { speedUnits };
            var checkboxItems = new Dictionary<UIMenuCheckboxItem, RNUIMouseInputHandler.Function>()
            {
                { threeDWaypoints, null},
                { mapBlips, ToggleMapBlips},
                { hints, ToggleHints}
            };
            var selectItems = new Dictionary<UIMenuItem, RNUIMouseInputHandler.Function>()
            {
                { saveSettings, ToggleSettings }
            };

            RNUIMouseInputHandler.Initialize(menu, scrollerItems, checkboxItems, selectItems);
        }
    }
}