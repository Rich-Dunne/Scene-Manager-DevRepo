using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using SceneManager.Managers;
using SceneManager.Utils;
using SceneManager.Waypoints;

namespace SceneManager.Menus
{
    class EditWaypointMenu
    {
        internal static UIMenu Menu = new UIMenu("Scene Manager", "~o~Edit Waypoint");
        internal static UIMenuItem UpdateWaypoint { get; } = new UIMenuItem("Update Waypoint");
        internal static UIMenuItem RemoveWaypoint { get; } = new UIMenuItem("Remove Waypoint");
        internal static UIMenuItem AddNewWaypoint { get; } = new UIMenuItem("Add as New Waypoint", "Adds a new waypoint to the end of the path with these settings");
        internal static UIMenuNumericScrollerItem<int> EditWaypoint { get; set; }
        internal static UIMenuNumericScrollerItem<int> ChangeWaypointSpeed { get; private set; }
        internal static UIMenuCheckboxItem StopWaypointType { get; private set; }
        internal static UIMenuCheckboxItem DirectWaypointBehavior { get; } = new UIMenuCheckboxItem("Drive directly to waypoint?", false, "If checked, vehicles will ignore traffic rules and drive directly to this waypoint.");
        internal static UIMenuCheckboxItem CollectorWaypoint { get; private set; }
        internal static UIMenuNumericScrollerItem<int> ChangeCollectorRadius { get; } = new UIMenuNumericScrollerItem<int>("Collection Radius", "The distance from this waypoint (in meters) vehicles will be collected", 1, 50, 1);
        internal static UIMenuNumericScrollerItem<int> ChangeSpeedZoneRadius { get; } = new UIMenuNumericScrollerItem<int>("Speed Zone Radius", "The distance from this collector waypoint (in meters) non-collected vehicles will drive at this waypoint's speed", 5, 200, 5);
        internal static UIMenuCheckboxItem UpdateWaypointPosition { get; } = new UIMenuCheckboxItem("Update Waypoint Position", false, "Updates the waypoint's position to the player's chosen position.  You should turn this on if you're planning on adding this waypoint as a new waypoint.");

        internal static void Initialize()
        {
            Menu.ParentMenu = EditPathMenu.Menu;
            MenuManager.MenuPool.Add(Menu);
            Menu.MaxItemsOnScreen = 11;

            Menu.OnScrollerChange += EditWaypoint_OnScrollerChanged;
            Menu.OnCheckboxChange += EditWaypoint_OnCheckboxChanged;
            Menu.OnItemSelect += EditWaypoint_OnItemSelected;
            Menu.OnMenuOpen += EditWaypoint_OnMenuOpen;
        }

        internal static void BuildEditWaypointMenu()
        {
            Menu.MenuItems.Clear();
            var currentPath = PathManager.Paths.FirstOrDefault(x => x.Name == PathMainMenu.EditPath.OptionText);

            Menu.AddItem(EditWaypoint = new UIMenuNumericScrollerItem<int>("Edit Waypoint", "", currentPath.Waypoints.First().Number, currentPath.Waypoints.Last().Number, 1));
            EditWaypoint.Index = 0;

            var currentWaypoint = currentPath.Waypoints.Where(wp => wp.Number == EditWaypoint.Value).FirstOrDefault();
            if(currentWaypoint == null)
            {
                Game.LogTrivial($"Current waypoint is null.");
                return;
            }

            Menu.AddItem(CollectorWaypoint = new UIMenuCheckboxItem("Collector", currentWaypoint.IsCollector, "If this waypoint will collect vehicles to follow the path"));

            Menu.AddItem(ChangeCollectorRadius);
            ChangeCollectorRadius.Value = currentWaypoint.CollectorRadius != 0
                ? (int)currentWaypoint.CollectorRadius
                : ChangeCollectorRadius.Minimum;

            Menu.AddItem(ChangeSpeedZoneRadius);
            ChangeSpeedZoneRadius.Value = currentWaypoint.CollectorRadius != 0
                ? (int)currentWaypoint.SpeedZoneRadius
                : ChangeSpeedZoneRadius.Minimum;

            ChangeCollectorRadius.Enabled = CollectorWaypoint.Checked ? true : false;
            ChangeSpeedZoneRadius.Enabled = CollectorWaypoint.Checked ? true : false;

            Menu.AddItem(StopWaypointType = new UIMenuCheckboxItem("Is this a Stop waypoint?", currentWaypoint.IsStopWaypoint, "If checked, vehicles will drive to this waypoint, then stop."));
            Menu.AddItem(DirectWaypointBehavior);
            if(currentWaypoint.DrivingFlagType == DrivingFlagType.Direct)
            {
                DirectWaypointBehavior.Checked = true;
            }

            Menu.AddItem(ChangeWaypointSpeed = new UIMenuNumericScrollerItem<int>("Waypoint Speed", $"How fast the AI will drive to the waypoint in ~b~{SettingsMenu.SpeedUnits.SelectedItem}", 5, 100, 5));
            ChangeWaypointSpeed.Value = (int)MathHelper.ConvertMetersPerSecondToMilesPerHour(currentWaypoint.Speed);

            Menu.AddItem(UpdateWaypointPosition);
            Menu.AddItem(UpdateWaypoint);
            UpdateWaypoint.ForeColor = Color.Gold;
            Menu.AddItem(RemoveWaypoint);
            RemoveWaypoint.ForeColor = Color.Gold;
            Menu.AddItem(AddNewWaypoint);
            AddNewWaypoint.ForeColor = Color.Gold;

            EditPathMenu.Menu.Visible = false;

            Menu.RefreshIndex();
            Menu.Visible = true;
        }

        private static void UpdateMenuSettings(Waypoint currentWaypoint)
        {
            ChangeWaypointSpeed.Value = (int)MathHelper.ConvertMetersPerSecondToMilesPerHour(currentWaypoint.Speed);
            StopWaypointType.Checked = currentWaypoint.IsStopWaypoint;
            DirectWaypointBehavior.Checked = currentWaypoint.DrivingFlagType == DrivingFlagType.Direct ? true : false;
            CollectorWaypoint.Checked = currentWaypoint.IsCollector;
            ChangeCollectorRadius.Enabled = CollectorWaypoint.Checked ? true : false;
            ChangeCollectorRadius.Value = (int)currentWaypoint.CollectorRadius;
            ChangeSpeedZoneRadius.Enabled = CollectorWaypoint.Checked ? true : false;
            ChangeSpeedZoneRadius.Value = (int)currentWaypoint.SpeedZoneRadius;
            UpdateWaypointPosition.Checked = false;
        }

        private static void ValidateCollectorRadiusSettings()
        {
            if (ChangeCollectorRadius.Value > ChangeSpeedZoneRadius.Value)
            {
                while (ChangeCollectorRadius.Value > ChangeSpeedZoneRadius.Value)
                {
                    ChangeSpeedZoneRadius.ScrollToNextOption();
                }
            }
        }

        private static void ValidateSpeedZoneRadiusSettings()
        {
            if (ChangeSpeedZoneRadius.Value < ChangeCollectorRadius.Value)
            {
                ChangeCollectorRadius.Value = ChangeSpeedZoneRadius.Value;
            }
        }

        private static void EditWaypoint_OnScrollerChanged(UIMenu sender, UIMenuScrollerItem scrollerItem, int first, int last)
        {
            var currentPath = PathManager.Paths[PathMainMenu.EditPath.Index];
            var currentWaypoint = currentPath.Waypoints[EditWaypoint.Value - 1];

            if (scrollerItem == EditWaypoint)
            {
                UpdateMenuSettings(currentWaypoint);
            }

            if (scrollerItem == ChangeCollectorRadius)
            {
                ValidateCollectorRadiusSettings();
            }

            if (scrollerItem == ChangeSpeedZoneRadius)
            {
                ValidateSpeedZoneRadiusSettings();
            }
        }

        private static void EditWaypoint_OnCheckboxChanged(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            if (checkboxItem == CollectorWaypoint)
            {
                ChangeCollectorRadius.Enabled = CollectorWaypoint.Checked ? true : false;
                ChangeSpeedZoneRadius.Enabled = CollectorWaypoint.Checked ? true : false;
            }
        }

        private static void EditWaypoint_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            //var currentPath = PathManager.Paths[PathMainMenu.EditPath.Index];
            var currentPath = PathManager.Paths.FirstOrDefault(x => x.Name == PathMainMenu.EditPath.OptionText);

            if (selectedItem == UpdateWaypoint)
            {
                PathManager.UpdateWaypoint();
            }

            if (selectedItem == AddNewWaypoint)
            {
                PathManager.AddNewEditWaypoint(currentPath);

                UpdateEditWaypointMenuItem();
            }

            if (selectedItem == RemoveWaypoint)
            {
                PathManager.RemoveEditWaypoint(currentPath);
                if(PathManager.Paths.Length < 1)
                {
                    return;
                }

                BuildEditWaypointMenu();
            }

            void UpdateEditWaypointMenuItem()
            {
                // Need to close and re-open the menu so the mouse scroll works on the re-added menu item.
                MenuManager.MenuPool.CloseAllMenus();
                Menu.RemoveItemAt(0);
                EditWaypoint = new UIMenuNumericScrollerItem<int>("Edit Waypoint", "", currentPath.Waypoints.First().Number, currentPath.Waypoints.Last().Number, 1);
                Menu.AddItem(EditWaypoint, 0);
                EditWaypoint.Index = EditWaypoint.OptionCount - 1;
                Menu.RefreshIndex();
                UpdateWaypointPosition.Checked = false;
                Menu.Visible = true;
            }
        }

        private static void EditWaypoint_OnMenuOpen(UIMenu menu)
        {
            var scrollerItems = new List<UIMenuScrollerItem> { EditWaypoint, ChangeWaypointSpeed, ChangeCollectorRadius, ChangeSpeedZoneRadius };
            GameFiber.StartNew(() => UserInput.InitializeMenuMouseControl(menu, scrollerItems), "RNUI Mouse Input Fiber");
        }
    }
}
