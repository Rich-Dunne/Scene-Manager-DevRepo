using RAGENativeUI;
using RAGENativeUI.Elements;
using System.Drawing;
using SceneManager.Utils;
using System.Collections.Generic;
using System.Linq;
using Rage;
using SceneManager.Managers;
using System.IO;

namespace SceneManager.Menus
{
    internal class ImportPathMenu
    {
        internal static UIMenu Menu = new UIMenu("Scene Manager", "~o~Import Path Menu");
        internal static UIMenuItem Import { get; } = new UIMenuItem("Import", "Import the selected paths.");
        internal static List<string> ImportedFileNames { get; } = new List<string>();

        internal static void Initialize()
        {
            Menu.ParentMenu = PathMainMenu.Menu;
            MenuManager.MenuPool.Add(Menu);

            Menu.OnItemSelect += ImportPathMenu_OnItemSelect;
            Menu.OnMenuOpen += ImportPathMenu_OnMenuOpen;
            Menu.OnCheckboxChange += ImportPathMenu_OnCheckboxChange;
        }

        internal static void Build()
        {
            Menu.Clear();

            GetFileNamesForPathsToImport();
            foreach(string fileName in ImportedFileNames)
            {
                var menuItem = new UIMenuCheckboxItem(fileName, false);
                if (!PathManager.LoadedFiles.Contains(fileName))
                {
                    menuItem.LeftBadge = UIMenuItem.BadgeStyle.Star;
                }
                Menu.AddItem(menuItem);
            }

            Menu.AddItem(Import);
            Import.ForeColor = Color.Gold;
            Import.Enabled = false;
        }

        private static void ImportPathMenu_OnMenuOpen(UIMenu menu)
        {
            GameFiber.StartNew(() => UserInput.InitializeMenuMouseControl(menu, new List<UIMenuScrollerItem>()), "RNUI Mouse Input Fiber");
        }

        private static void ImportPathMenu_OnItemSelect(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if(selectedItem == Import)
            {
                var checkboxItems = Menu.MenuItems.Where(x => x.GetType() == typeof(UIMenuCheckboxItem)).Cast<UIMenuCheckboxItem>();
                var checkedItems = checkboxItems.Where(x => x.Checked);

                foreach(var menuItem in checkedItems)
                {
                    var importedPaths = PathManager.ImportPathsFromFile(menuItem.Text);
                    if (importedPaths != null)
                    {
                        PathManager.LoadImportedPaths(importedPaths, menuItem.Text);
                    }
                }
                Menu.RefreshIndex();

                MenuManager.BuildMenus();
                Menu.Visible = true;
            }
        }

        private static void GetFileNamesForPathsToImport()
        {
            ImportedFileNames.Clear();

            // Check if Saved Paths directory exists
            var GAME_DIRECTORY = Directory.GetCurrentDirectory();
            var SAVED_PATHS_DIRECTORY = GAME_DIRECTORY + "\\plugins\\SceneManager\\Saved Paths\\";
            if (!Directory.Exists(SAVED_PATHS_DIRECTORY))
            {
                Game.LogTrivial($"Directory '\\plugins\\SceneManager\\Saved Paths' does not exist.  No paths available to import.");
                return;
            }

            // Check if any XML files are available to import from Saved Paths
            var savedPathFiles = Directory.GetFiles(SAVED_PATHS_DIRECTORY, "*.xml");
            if (savedPathFiles.Length == 0)
            {
                Game.LogTrivial($"No saved paths found.");
                return;
            }
            else
            {
                Game.LogTrivial($"{savedPathFiles.Length} path(s) available to import.");
            }

            // Import file names
            foreach (string file in savedPathFiles)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                Game.LogTrivial($"File: {fileName}");
                ImportedFileNames.Add(fileName);
            }
            Game.LogTrivial($"Successfully populated menu with {ImportedFileNames.Count} file(s).");
        }

        private static void ImportPathMenu_OnCheckboxChange(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool Checked)
        {
            var checkboxItems = Menu.MenuItems.Where(x => x.GetType() == typeof(UIMenuCheckboxItem));
            int checkedItems = 0;
            foreach (UIMenuCheckboxItem menuItem in checkboxItems)
            {
                if (menuItem.Checked)
                {
                    checkedItems++;
                }
            }

            Import.Enabled = checkedItems > 0;
        }
    }
}
