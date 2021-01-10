using System.Collections.Generic;
using System.Drawing;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using SceneManager.Menus;
using SceneManager.Utils;

namespace SceneManager
{
    internal class EditPathMenu
    {
        internal static UIMenu Menu { get; } = new UIMenu("Scene Manager", "~o~Edit Path");
        internal static UIMenuCheckboxItem DisablePath { get; } = new UIMenuCheckboxItem("Disable Path", false);
        private static UIMenuItem EditWaypoints { get; } = new UIMenuItem("Edit Waypoints");
        private static UIMenuItem deletePath { get; } = new UIMenuItem("Delete Path");
        private static UIMenuItem ExportPath { get; } = new UIMenuItem("Export Path");

        internal static void Initialize()
        {
            Menu.ParentMenu = PathMainMenu.Menu;
            MenuManager.MenuPool.Add(Menu);

            Menu.OnItemSelect += EditPath_OnItemSelected;
            Menu.OnCheckboxChange += EditPath_OnCheckboxChange;
            Menu.OnMenuOpen += EditPath_OnMenuOpen;
        }

        internal static void BuildEditPathMenu()
        {
            Menu.AddItem(DisablePath);
            Menu.AddItem(EditWaypoints);
            EditWaypoints.ForeColor = Color.Gold;
            Menu.AddItem(deletePath);
            deletePath.ForeColor = Color.Gold;
            Menu.AddItem(ExportPath);
            ExportPath.ForeColor = Color.Gold;
            Menu.RefreshIndex();
        }

        private static void EditPath_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == EditWaypoints)
            {
                if (!SettingsMenu.ThreeDWaypoints.Checked)
                {
                    Hints.Display($"~o~Scene Manager ~y~[Hint]\n~w~You have 3D waypoints disabled in your settings.  It's recommended to enable 3D waypoints while working with waypoints.");
                }
                EditWaypointMenu.BuildEditWaypointMenu();
            }

            if (selectedItem == deletePath)
            {
                var currentPath = PathManager.Paths[PathMainMenu.EditPath.Index];
                currentPath.Delete();
                PathManager.Paths.Remove(currentPath);
                PathMainMenu.BuildPathMenu();
                PathMainMenu.Menu.Visible = true;
            }

            if(selectedItem == ExportPath)
            {
                PathManager.ExportPath();
            }
        }

        private static void EditPath_OnCheckboxChange(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            if (checkboxItem == DisablePath)
            {
                var currentPath = PathManager.Paths[PathMainMenu.EditPath.Index];
                if (DisablePath.Checked)
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

        private static void EditPath_OnMenuOpen(UIMenu menu)
        {
            var scrollerItems = new List<UIMenuScrollerItem> {  };
            GameFiber.StartNew(() => UserInput.InitializeMenuMouseControl(menu, scrollerItems), "RNUI Mouse Input Fiber");
        }
    }
}
