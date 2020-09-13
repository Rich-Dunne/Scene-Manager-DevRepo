using System.Drawing;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    class EditPathMenu
    {
        public static UIMenu editPathMenu { get; private set; }
        private static UIMenuItem editPathWaypoints, deletePath;
        public static UIMenuCheckboxItem disablePath;

        internal static void InstantiateMenu()
        {
            editPathMenu = new UIMenu("Scene Manager", "~o~Edit Path");
            editPathMenu.ParentMenu = PathMainMenu.pathMainMenu;
            MenuManager.menuPool.Add(editPathMenu);
        }

        public static void BuildEditPathMenu()
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
            var currentPath = PathMainMenu.GetPaths()[PathMainMenu.editPath.Index];

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
                var currentPath = PathMainMenu.GetPaths()[PathMainMenu.editPath.Index];
                if (disablePath.Checked)
                {
                    currentPath.DisablePath();
                    Game.LogTrivial($"Path {currentPath.Number} disabled.");
                }
                else
                {
                    currentPath.EnablePath();
                    Game.LogTrivial($"Path {currentPath.Number} enabled.");
                }
            }
        }
    }
}
