using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    class EditPathMenu
    {
        internal static UIMenu editPathMenu = new UIMenu("Scene Manager", "~o~Edit Path");
        private static UIMenuItem editPathWaypoints, deletePath, savePath;
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
            editPathMenu.AddItem(savePath = new UIMenuItem("Export Path"));
            savePath.ForeColor = Color.Gold;
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

        private static void ExportPath()
        {
            // Reference PNWParks's UserInput class from LiveLights
            string filename = PNWUserInput.GetUserInput("Type the name you would like to save your file as", "Enter a filename", 100);

            // If filename != null or empty, check if export directory exists (GTA V/Plugins/SceneManager/Saved Paths)
            if(string.IsNullOrWhiteSpace(filename))
            {
                Game.LogTrivial($"Invalid filename given.  Filename cannot be null, empty, or consist of just white spaces.");
                return;
            }
            Game.LogTrivial($"Filename: {filename}");

            // If directory does not exist, create it
            var gameDirectory = Directory.GetCurrentDirectory();
            var pathDirectoryExists = Directory.Exists(gameDirectory + "/plugins/SceneManager/Saved Paths");
            if (!pathDirectoryExists)
            {
                Directory.CreateDirectory(gameDirectory + "/plugins/SceneManager/Saved Paths");
                Game.LogTrivial($"New directory created at '/plugins/SceneManager/Saved Paths'");
            }

            // Create XML in save directory with user's filename, saving all path information
            //<Path>
            //    <Waypoint number="1" pos="x,y,z" speed="5" drivingFlag="Normal" stop="false" collector="true" collectorRadius="3" speedZoneRadius="5"/>        
            //</Path>
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

            if(selectedItem == savePath)
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
            var checkboxItems = new Dictionary<UIMenuCheckboxItem, RNUIMouseInputHandler.Function>() { { disablePath, DisablePath } };
            var selectItems = new Dictionary<UIMenuItem, RNUIMouseInputHandler.Function>()
            {
                { editPathWaypoints, EditPathWaypoints },
                { deletePath, DeletePath },
                { savePath, ExportPath }
            };

            RNUIMouseInputHandler.Initialize(menu, scrollerItems);
        }
    }
}
