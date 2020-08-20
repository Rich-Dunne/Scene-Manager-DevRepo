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
                var currentPath = TrafficMenu.paths[TrafficMenu.editPath.Index];
                if (togglePath.Checked)
                {
                    currentPath.DisablePath();
                    Game.LogTrivial($"Path {currentPath.PathNum} disabled.");
                }
                else
                {
                    currentPath.EnablePath();
                    Game.LogTrivial($"Path {currentPath.PathNum} enabled.");
                }
            }
        }
    }
}
