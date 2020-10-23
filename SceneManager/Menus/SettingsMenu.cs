using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
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
            settingsMenu.OnMenuOpen += SettingsMenu_OnMouseDown;
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
        
        internal static void ToggleMapBlips()
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

        private static void SettingsMenu_OnMouseDown(UIMenu menu)
        {
            GameFiber.StartNew(() =>
            {
                while (menu.Visible)
                {
                    var selectedScroller = menu.MenuItems.Where(x => x == speedUnits && x.Selected).FirstOrDefault();
                    if (selectedScroller != null)
                    {
                        HandleScrollerItemsWithMouseWheel(selectedScroller);
                    }

                    // Add waypoint if menu item is selected and user left clicks
                    if (Game.IsKeyDown(Keys.LButton))
                    {
                        OnCheckboxItemClicked();
                        OnMenuItemClicked();
                    }
                    GameFiber.Yield();
                }
            });

            void OnCheckboxItemClicked()
            {
                if (threeDWaypoints.Selected && threeDWaypoints.Enabled)
                {
                    threeDWaypoints.Checked = !threeDWaypoints.Checked;
                }
                else if (mapBlips.Selected)
                {
                    mapBlips.Checked = !mapBlips.Checked;
                    ToggleMapBlips();
                }
                else if (hints.Selected)
                {
                    hints.Checked = !hints.Checked;
                    Hints.Enabled = hints.Checked ? true : false;
                }
            }

            void OnMenuItemClicked()
            {
                if (saveSettings.Selected)
                {
                    Settings.UpdateSettings(threeDWaypoints.Checked, mapBlips.Checked, hints.Checked, speedUnits.SelectedItem);
                    Game.DisplayHelp($"Scene Manager settings saved");
                }
            }

            void HandleScrollerItemsWithMouseWheel(UIMenuItem selectedScroller)
            {
                var menuScrollingDisabled = false;
                var menuItems = menu.MenuItems.Where(x => x != selectedScroller);
                while (Game.IsShiftKeyDownRightNow)
                {
                    menu.ResetKey(Common.MenuControls.Up);
                    menu.ResetKey(Common.MenuControls.Down);
                    menuScrollingDisabled = true;
                    ScrollMenuItem();
                    GameFiber.Yield();
                }

                if (menuScrollingDisabled)
                {
                    menuScrollingDisabled = false;
                    menu.SetKey(Common.MenuControls.Up, GameControl.CursorScrollUp);
                    menu.SetKey(Common.MenuControls.Up, GameControl.CellphoneUp);
                    menu.SetKey(Common.MenuControls.Down, GameControl.CursorScrollDown);
                    menu.SetKey(Common.MenuControls.Down, GameControl.CellphoneDown);
                }

                void ScrollMenuItem()
                {
                    if (Game.GetMouseWheelDelta() > 0)
                    {
                        if (selectedScroller == speedUnits)
                        {
                            speedUnits.ScrollToNextOption();
                            PathCreationMenu.pathCreationMenu.Clear();
                            PathCreationMenu.BuildPathCreationMenu();
                        }
                    }
                    else if (Game.GetMouseWheelDelta() < 0)
                    {
                        if (selectedScroller == speedUnits)
                        {
                            speedUnits.ScrollToPreviousOption();
                            PathCreationMenu.pathCreationMenu.Clear();
                            PathCreationMenu.BuildPathCreationMenu();
                        }
                    }
                }
            }
        }
    }
}