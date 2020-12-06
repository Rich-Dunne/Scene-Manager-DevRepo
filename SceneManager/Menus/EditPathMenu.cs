using System.Collections.Generic;
using System.Drawing;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using SceneManager.Utils;

namespace SceneManager
{
    class EditPathMenu
    {
        internal static UIMenu editPathMenu = new UIMenu("Scene Manager", "~o~Edit Path");
        private static UIMenuItem editPathWaypoints, deletePath, exportPath;
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
            //editPathMenu.AddItem(exportPath = new UIMenuItem("Export Path"));
            //exportPath.ForeColor = Color.Gold;
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
            PathMainMenu.DeletePath(currentPath, Delete.Single);
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

        private static void ExportPath()
        {
            var currentPath = PathMainMenu.paths[PathMainMenu.editPath.Index];
            // Reference PNWParks's UserInput class from LiveLights
            var filename = PNWUserInput.GetUserInput("Type the name you would like to save your file as", "Enter a filename", 100) + ".xml";

            // If filename != null or empty, check if export directory exists (GTA V/Plugins/SceneManager/Saved Paths)
            if(string.IsNullOrWhiteSpace(filename))
            {
                Game.DisplayHelp($"Invalid filename given.  Filename cannot be null, empty, or consist of just white spaces.");
                Game.LogTrivial($"Invalid filename given.  Filename cannot be null, empty, or consist of just white spaces.");
                return;
            }
            Game.LogTrivial($"Filename: {filename}");
            currentPath.Save(filename);
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

            if(selectedItem == exportPath)
            {
                ExportPath();
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
            RNUIMouseInputHandler.Initialize(menu, scrollerItems);
        }
    }
}
