using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
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
            editPathMenu.OnItemSelect += EditPath_OnItemSelected;
            editPathMenu.OnCheckboxChange += EditPath_OnCheckboxChange;
            editPathMenu.OnMenuOpen += EditPath_OnMenuOpen;
        }

        internal static void BuildEditPathMenu()
        {
            editPathMenu.AddItem(disablePath = new UIMenuCheckboxItem("Disable Path", false));
            editPathMenu.AddItem(editPathWaypoints = new UIMenuItem("Edit Waypoints"));
            editPathWaypoints.ForeColor = Color.Gold;
            editPathMenu.AddItem(deletePath = new UIMenuItem("Delete Path"));
            deletePath.ForeColor = Color.Gold;

            editPathMenu.RefreshIndex();
        }

        private static void EditPathWaypoints()
        {
            if (!SettingsMenu.threeDWaypoints.Checked)
            {
                Hints.Display($"~o~Scene Manager ~y~[Hint]\n~w~You have 3D waypoints disabled in your settings.  It's recommended to enable 3D waypoints while working with waypoints.");
            }
            EditWaypointMenu.BuildEditWaypointMenu();
        }

        private static void DeletePath()
        {
            var currentPath = PathMainMenu.paths[PathMainMenu.editPath.Index];
            PathMainMenu.DeletePath(currentPath, PathMainMenu.Delete.Single);
        }

        private static void DisablePath()
        {
            var currentPath = PathMainMenu.paths[PathMainMenu.editPath.Index];
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

        private static void EditPath_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == editPathWaypoints)
            {
                EditPathWaypoints();
            }

            if (selectedItem == deletePath)
            {
                DeletePath();
            }
        }

        private static void EditPath_OnCheckboxChange(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            if (checkboxItem == disablePath)
            {
                DisablePath();
            }
        }

        private static void EditPath_OnMenuOpen(UIMenu menu)
        {
            var scrollerItems = new List<UIMenuScrollerItem> {  };
            var checkboxItems = new Dictionary<UIMenuCheckboxItem, RNUIMouseInputHandler.Function>() { { disablePath, DisablePath } };
            var selectItems = new Dictionary<UIMenuItem, RNUIMouseInputHandler.Function>()
            {
                { editPathWaypoints, EditPathWaypoints },
                { deletePath, DeletePath }
            };

            RNUIMouseInputHandler.Initialize(menu, scrollerItems, checkboxItems, selectItems);
        }
    }
}
