using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using SceneManager.Utils;
using SceneManager.Managers;

namespace SceneManager.Menus
{
    class SettingsMenu
    {
        internal static UIMenu Menu { get; set; } = new UIMenu("Scene Manager", "~o~Plugin Settings");
        internal static UIMenuCheckboxItem ThreeDWaypoints { get; } = new UIMenuCheckboxItem("Enable 3D Waypoints", Settings.Enable3DWaypoints);
        internal static UIMenuCheckboxItem MapBlips { get; } = new UIMenuCheckboxItem("Enable Map Blips", Settings.EnableMapBlips);
        internal static UIMenuCheckboxItem Hints { get; } = new UIMenuCheckboxItem("Enable Hints", Settings.EnableHints);
        private static SpeedUnits[] SpeedUnitsArray { get; } = { Utils.SpeedUnits.MPH, Utils.SpeedUnits.KPH };
        internal static UIMenuListScrollerItem<SpeedUnits> SpeedUnits { get; } = new UIMenuListScrollerItem<SpeedUnits>("Speed Unit of Measure", "", new[] { Utils.SpeedUnits.MPH, Utils.SpeedUnits.KPH });
        internal static UIMenuItem SaveSettings { get; } = new UIMenuItem("Save settings to .ini", "Updates the plugin's .ini file with the current settings.  The next time the plugin is loaded, it will use these settings.");

        internal static void Initialize()
        {
            Menu.ParentMenu = MainMenu.Menu;
            MenuManager.MenuPool.Add(Menu);

            Menu.OnCheckboxChange += SettingsMenu_OnCheckboxChange;
            Menu.OnItemSelect += SettingsMenu_OnItemSelected;
            Menu.OnMenuOpen += SettingsMenu_OnMenuOpen;
        }

        internal static void Build()
        {
            Menu.Clear();

            Menu.AddItem(ThreeDWaypoints);
            Menu.AddItem(MapBlips);
            Menu.AddItem(Hints);
            Menu.AddItem(SpeedUnits);
            SpeedUnits.Index = Array.IndexOf(SpeedUnitsArray, Settings.SpeedUnit);
            Menu.AddItem(SaveSettings);
            SaveSettings.ForeColor = System.Drawing.Color.Gold;
        }

        private static void SettingsMenu_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if(selectedItem == SaveSettings)
            {
                Settings.UpdateSettings(ThreeDWaypoints.Checked, MapBlips.Checked, Hints.Checked, SpeedUnits.SelectedItem);
                Game.DisplayHelp($"Scene Manager settings saved");
            }
        }

        private static void SettingsMenu_OnCheckboxChange(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            if (checkboxItem == MapBlips)
            {
                PathManager.ToggleBlips(MapBlips.Checked);
            }

            if (checkboxItem == Hints)
            {
                SceneManager.Hints.Enabled = Hints.Checked ? true : false;
            }
        }

        private static void SettingsMenu_OnMenuOpen(UIMenu menu)
        {
            var scrollerItems = new List<UIMenuScrollerItem> { SpeedUnits };
            GameFiber.StartNew(() => UserInput.InitializeMenuMouseControl(menu, scrollerItems), "RNUI Mouse Input Fiber");
        }
    }
}