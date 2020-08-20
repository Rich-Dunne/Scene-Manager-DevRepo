using System.Drawing;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    class EditPathMenu
    {
        private static UIMenuItem editPathWaypoints, deletePath;
        public static UIMenuCheckboxItem togglePath;

        public static void BuildEditPathMenu()
        {
            MenuManager.editPathMenu.AddItem(togglePath = new UIMenuCheckboxItem("Disable Path", false));
            MenuManager.editPathMenu.AddItem(editPathWaypoints = new UIMenuItem("Edit Waypoints"));
            MenuManager.editPathMenu.AddItem(deletePath = new UIMenuItem("Delete Path"));

            MenuManager.editPathMenu.RefreshIndex();
            MenuManager.editPathMenu.OnItemSelect += EditPath_OnItemSelected;
            MenuManager.editPathMenu.OnCheckboxChange += EditPath_OnCheckboxChange;
        }

        private static void EditPath_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            var currentPath = TrafficMenu.paths[TrafficMenu.editPath.Index];

            if (selectedItem == editPathWaypoints)
            {
                EditWaypointMenu.BuildEditWaypointMenu();
            }

            if (selectedItem == deletePath)
            {
                TrafficMenu.DeletePath(currentPath, currentPath.PathNum - 1, "Single");
            }
        }

        private static void EditPath_OnCheckboxChange(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            if (checkboxItem == togglePath)
            {
                if (togglePath.Checked)
                {
                    TrafficMenu.paths[TrafficMenu.editPath.Index].PathDisabled = true;
                    Game.LogTrivial($"Path {TrafficMenu.paths[TrafficMenu.editPath.Index].PathNum} disabled.");

                    foreach (Waypoint wd in TrafficMenu.paths[TrafficMenu.editPath.Index].Waypoint)
                    {
                        wd.Blip.Alpha = 0.5f;
                        if (wd.CollectorRadiusBlip)
                        {
                            wd.CollectorRadiusBlip.Alpha = 0.25f;
                        }
                    }
                }
                else
                {
                    TrafficMenu.paths[TrafficMenu.editPath.Index].PathDisabled = false;
                    Game.LogTrivial($"Path {TrafficMenu.paths[TrafficMenu.editPath.Index].PathNum} enabled.");

                    foreach (Waypoint wd in TrafficMenu.paths[TrafficMenu.editPath.Index].Waypoint)
                    {
                        wd.Blip.Alpha = 1.0f;
                        if (wd.CollectorRadiusBlip)
                        {
                            wd.CollectorRadiusBlip.Alpha = 0.5f;
                        }
                    }
                }
            }
        }
    }
}
