using System.Collections.Generic;
using System.Drawing;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using SceneManager.Utils;
using SceneManager.Managers;
using SceneManager.Paths;

namespace SceneManager.Menus
{
    class PathCreationMenu
    {
        internal static UIMenu Menu { get; } = new UIMenu("Scene Manager", "~o~Path Creation Menu");
        private static Path CurrentPath { get; set; }
        private static UIMenuItem AddWaypoint { get; } = new UIMenuItem("Add waypoint");
        internal static UIMenuItem RemoveLastWaypoint { get; } = new UIMenuItem("Remove last waypoint");
        internal static UIMenuItem EndPathCreation { get; } = new UIMenuItem("End path creation");
        internal static UIMenuNumericScrollerItem<int> WaypointSpeed { get; private set; }
        internal static UIMenuCheckboxItem StopWaypoint { get; } = new UIMenuCheckboxItem("Is this a Stop waypoint?", false, "If checked, vehicles will drive to this waypoint, then stop.");
        internal static UIMenuCheckboxItem DirectWaypoint { get; } = new UIMenuCheckboxItem("Drive directly to waypoint?", false, "If checked, vehicles will ignore traffic rules and drive directly to this waypoint.");
        internal static UIMenuCheckboxItem CollectorWaypoint { get; } = new UIMenuCheckboxItem("Collector", true, "If checked, this waypoint will collect vehicles to follow the path.  Your path's first waypoint ~b~must~w~ be a collector.");
        internal static UIMenuNumericScrollerItem<int> CollectorRadius { get; } = new UIMenuNumericScrollerItem<int>("Collection Radius", "The distance from this waypoint (in meters) vehicles will be collected", 1, 50, 1);
        internal static UIMenuNumericScrollerItem<int> SpeedZoneRadius { get; } = new UIMenuNumericScrollerItem<int>("Speed Zone Radius", "The distance from this collector waypoint (in meters) non-collected vehicles will drive at this waypoint's speed", 5, 200, 5);
        internal static State PathCreationState { get; set; } = State.Uninitialized;

        internal static void Initialize()
        {
            Menu.ParentMenu = PathMainMenu.Menu;
            MenuManager.MenuPool.Add(Menu);

            Menu.OnItemSelect += PathCreation_OnItemSelected;
            Menu.OnCheckboxChange += PathCreation_OnCheckboxChanged;
            Menu.OnScrollerChange += PathCreation_OnScrollerChanged;
            Menu.OnMenuOpen += PathCreation_OnMenuOpen;
        }

        internal static void BuildPathCreationMenu()
        {
            Menu.Clear();

            Menu.AddItem(CollectorWaypoint);
            CollectorWaypoint.Enabled = false;
            CollectorWaypoint.Checked = true;

            Menu.AddItem(CollectorRadius);
            CollectorRadius.Index = Settings.CollectorRadius - 1;
            CollectorRadius.Enabled = true;

            Menu.AddItem(SpeedZoneRadius);
            SpeedZoneRadius.Index = (Settings.SpeedZoneRadius / 5) - 1;
            SpeedZoneRadius.Enabled = true;

            Menu.AddItem(StopWaypoint);
            StopWaypoint.Checked = Settings.StopWaypoint;
            Menu.AddItem(DirectWaypoint);
            DirectWaypoint.Checked = Settings.DirectDrivingBehavior;

            Menu.AddItem(WaypointSpeed = new UIMenuNumericScrollerItem<int>("Waypoint Speed", $"How fast the AI will drive to this waypoint in ~b~{SettingsMenu.SpeedUnits.SelectedItem}", 5, 100, 5));
            WaypointSpeed.Index = (Settings.WaypointSpeed / 5) - 1;

            Menu.AddItem(AddWaypoint);
            AddWaypoint.ForeColor = Color.Gold;

            Menu.AddItem(RemoveLastWaypoint);
            RemoveLastWaypoint.ForeColor = Color.Gold;
            RemoveLastWaypoint.Enabled = false;

            Menu.AddItem(EndPathCreation);
            EndPathCreation.ForeColor = Color.Gold;
            EndPathCreation.Enabled = false;

            Menu.RefreshIndex();
        }

        private static void ValidateCollectorRadiusSettings()
        {
            if (CollectorRadius.Value > SpeedZoneRadius.Value)
            {
                while (CollectorRadius.Value > SpeedZoneRadius.Value)
                {
                    SpeedZoneRadius.ScrollToNextOption();
                }
            }
        }
        
        private static void ValidateSpeedZoneRadiusSettings()
        {
            if (SpeedZoneRadius.Value < CollectorRadius.Value)
            {
                CollectorRadius.Value = SpeedZoneRadius.Value;
            }
        }

        private static void PathCreation_OnCheckboxChanged(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            if(checkboxItem == CollectorWaypoint)
            {
                CollectorRadius.Enabled = CollectorWaypoint.Checked ? true : false;
                SpeedZoneRadius.Enabled = CollectorWaypoint.Checked ? true : false;
            }
        }

        private static void PathCreation_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == AddWaypoint)
            {
                if (PathCreationState != State.Creating)
                {
                    CurrentPath = PathManager.InitializeNewPath();
                }

                PathManager.AddWaypoint(CurrentPath);
                PathManager.TogglePathCreationMenuItems(CurrentPath);
            }

            if (selectedItem == RemoveLastWaypoint)
            {
                PathManager.RemoveWaypoint(CurrentPath);
                PathManager.TogglePathCreationMenuItems(CurrentPath);
            }

            if (selectedItem == EndPathCreation)
            {
                PathCreationState = State.Finished;
                PathManager.EndPath(CurrentPath);
            }
        }

        private static void PathCreation_OnScrollerChanged(UIMenu sender, UIMenuScrollerItem scrollerItem, int first, int last)
        {
            if (scrollerItem == CollectorRadius)
            {
                ValidateCollectorRadiusSettings();   
            }

            if (scrollerItem == SpeedZoneRadius)
            {
                ValidateSpeedZoneRadiusSettings();  
            }
        }

        private static void PathCreation_OnMenuOpen(UIMenu menu)
        {
            var scrollerItems = new List<UIMenuScrollerItem> { CollectorRadius, SpeedZoneRadius, WaypointSpeed };
            GameFiber.StartNew(() => UserInput.InitializeMenuMouseControl(menu, scrollerItems), "RNUI Mouse Input Fiber");
        }
    }
}
