using System.Drawing;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    class EditPathMenu
    {
        internal static UIMenu editPathMenu = new UIMenu("Scene Manager", "~o~Edit Path");
        private static UIMenuItem editPathWaypoints, deletePath;
        internal static UIMenuCheckboxItem disablePath;

        internal static void InstantiateMenu()
        {
            editPathMenu.ParentMenu = PathMainMenu.pathMainMenu;
            MenuManager.menuPool.Add(editPathMenu);
        }

        internal static void BuildEditPathMenu()
        {
            editPathMenu.AddItem(disablePath = new UIMenuCheckboxItem("Disable Path", false));
            editPathMenu.AddItem(editPathWaypoints = new UIMenuItem("Edit Waypoints"));
            editPathWaypoints.ForeColor = Color.Gold;
            editPathMenu.AddItem(deletePath = new UIMenuItem("Delete Path"));
            deletePath.ForeColor = Color.Gold;

            editPathMenu.RefreshIndex();
            editPathMenu.OnItemSelect += EditPath_OnItemSelected;
            editPathMenu.OnCheckboxChange += EditPath_OnCheckboxChange;
        }

        private static void EditPath_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            var currentPath = PathMainMenu.paths[PathMainMenu.editPath.Index];

            if (selectedItem == editPathWaypoints)
            {
                EditWaypointMenu.BuildEditWaypointMenu();
            }

            if (selectedItem == deletePath)
            {
                PathMainMenu.DeletePath(currentPath, PathMainMenu.Delete.Single);
            }
        }

        private static void EditPath_OnCheckboxChange(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            if (checkboxItem == disablePath)
            {
                var currentPath = PathMainMenu.paths[PathMainMenu.editPath.Index];
                if (disablePath.Checked)
                {
                    currentPath.DisablePath();
                    Logger.Log($"Path {currentPath.Number} disabled.");
                }
                else
                {
                    currentPath.EnablePath();
                    Logger.Log($"Path {currentPath.Number} enabled.");
                }
            }
        }
    }
}
