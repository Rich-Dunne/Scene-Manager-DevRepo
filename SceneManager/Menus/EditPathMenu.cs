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
            editPathMenu.OnMenuOpen += EditPath_OnMouseDown;
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

        private static void EditPath_OnMouseDown(UIMenu menu)
        {
            GameFiber.StartNew(() =>
            {
                while (menu.Visible)
                {
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
                if (disablePath.Selected && disablePath.Enabled)
                {
                    disablePath.Checked = !disablePath.Checked;
                    DisablePath();
                }
            }

            void OnMenuItemClicked()
            {
                if (editPathWaypoints.Selected)
                {
                    EditPathWaypoints();
                }
                else if (deletePath.Selected)
                {
                    DeletePath();
                }
            }
        }
    }
}
