using RAGENativeUI;
using RAGENativeUI.Elements;
using System.Drawing;
using SceneManager.Utils;
using System.Collections.Generic;
using System.Linq;
using Rage;
using SceneManager.Managers;
using System.IO;
using System.Xml.Serialization;

namespace SceneManager.Menus
{
    internal static class ExportPathMenu
    {
        public static List<Paths.Path> ExportPaths { get; } = new List<Paths.Path>();

        internal static UIMenu Menu = new UIMenu("Scene Manager", "~o~Export Path Menu");
        internal static UIMenuListScrollerItem<string> ExportOptions = new UIMenuListScrollerItem<string>("Export As", "Choose whether you want the paths exported as individual files, or all within the same file.", new string[] { "Individual file(s)", "Combined file" });
        internal static UIMenuItem Export = new UIMenuItem("Export", "Export the selected path(s)");

        internal static void Initialize()
        {
            Menu.ParentMenu = PathMainMenu.Menu;
            MenuManager.MenuPool.Add(Menu);

            Menu.OnItemSelect += ExportPathMenu_OnItemSelect;
            Menu.OnCheckboxChange += ExportPathMenu_OnCheckboxChange;
            Menu.OnMenuOpen += ExportPathMenu_OnMenuOpen;
        }

        internal static void Build()
        {
            Menu.Clear();
            foreach(Paths.Path path in PathManager.Paths.Where(x => x != null))
            {
                Menu.AddItem(new UIMenuCheckboxItem(path.Name, false));
            }

            Menu.AddItem(ExportOptions);
            ExportOptions.Enabled = false;
            Menu.AddItem(Export);
            Export.ForeColor = Color.Gold;
            Export.Enabled = false;

            Menu.RefreshIndex();
        }

        private static void ExportPathMenu_OnMenuOpen(UIMenu menu)
        {
            var scrollerItems = new List<UIMenuScrollerItem> { ExportOptions };
            GameFiber.StartNew(() => UserInput.InitializeMenuMouseControl(menu, scrollerItems), "RNUI Mouse Input Fiber");
        }

        private static void ExportPathMenu_OnCheckboxChange(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool Checked)
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

            Export.Enabled = checkedItems > 0;

            if(checkedItems > 1)
            {
                ExportOptions.Enabled = true;
            }
            else
            {
                if(!ExportOptions.OptionText.Contains("Individual"))
                {
                    ExportOptions.ScrollToNextOption();
                }
                ExportOptions.Enabled = false;
            }
        }

        private static void ExportPathMenu_OnItemSelect(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if(selectedItem == Export)
            {
                ExportPaths.Clear();
                var checkboxItems = Menu.MenuItems.Where(x => x.GetType() == typeof(UIMenuCheckboxItem)).Select(x => (UIMenuCheckboxItem)x);
                var checkedItems = checkboxItems.Where(x => x.Checked);
                foreach(UIMenuCheckboxItem checkedItem in checkedItems)
                {
                    var pathToExport = PathManager.Paths.First(x => x.Name == checkedItem.Text);
                    ExportPaths.Add(pathToExport);
                }

                if (ExportOptions.OptionText.Contains("Individual"))
                {
                    foreach (UIMenuCheckboxItem menuItem in checkedItems)
                    {
                        var pathToExport = PathManager.Paths.First(x => x.Name == menuItem.Text);
                        ExportAsIndividualFile(pathToExport);
                    }
                }
                else
                {
                    ExportAsCombinedFile(checkboxItems);
                }

                MenuManager.BuildMenus();
                Menu.Visible = true;
            }
        }

        private static void ExportAsIndividualFile(Paths.Path pathToExport)
        {
            if (CanQuickSavePath(pathToExport))
            {
                pathToExport.Save();
                return;
            }

            var fileName = UserInput.PromptPlayerForFileName("Type the name you would like to save your file as", "Enter a filename", 100);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                Game.DisplayHelp($"Invalid filename given.  Filename cannot be null, empty, or consist of just white spaces.  Defaulting to ~b~\"{pathToExport.Name}\"");
                Game.LogTrivial($"Invalid filename given.  Filename cannot be null, empty, or consist of just white spaces.  Defaulting to \"{pathToExport.Name}\"");
                fileName = pathToExport.Name;
            }

            Game.LogTrivial($"Filename: {fileName}");
            pathToExport.Name = fileName;

            var GAME_DIRECTORY = Directory.GetCurrentDirectory();
            var SAVED_PATHS_DIRECTORY = GAME_DIRECTORY + "/plugins/SceneManager/Saved Paths/";
            var overrides = DefineOverridesForCombinedPath();
            Serializer.SaveItemToXML(ExportPaths, SAVED_PATHS_DIRECTORY + fileName + ".xml", overrides);
            Game.DisplayNotification($"~o~Scene Manager ~g~[Success]\n~w~Path exported as ~b~{fileName}.xml~w~.");
        }

        private static void ExportAsCombinedFile(IEnumerable<UIMenuCheckboxItem> checkedItems)
        {
            // If any file contains all (and only) path names from checkedItems, it can be quicksaved        
            string existingFile = GetNameForExistingCombinedPathsFile(checkedItems);
            if(existingFile == "")
            {
                existingFile = UserInput.PromptPlayerForFileName("Type the name you would like to save your file as", "Enter a filename", 100);

                if (string.IsNullOrWhiteSpace(existingFile))
                {
                    Game.DisplayHelp($"Invalid filename given.  Filename cannot be null, empty, or consist of just white spaces.  Defaulting to ~b~\"{checkedItems.First().Text}\"");
                    Game.LogTrivial($"Invalid filename given.  Filename cannot be null, empty, or consist of just white spaces.  Defaulting to \"{checkedItems.First().Text}\"");
                    existingFile = checkedItems.First().Text;
                }
            }

            Game.LogTrivial($"Filename: {existingFile}");
            
            var GAME_DIRECTORY = Directory.GetCurrentDirectory();
            var SAVED_PATHS_DIRECTORY = GAME_DIRECTORY + "/plugins/SceneManager/Saved Paths/";
            var overrides = DefineOverridesForCombinedPath();
            Serializer.SaveItemToXML(ExportPaths, SAVED_PATHS_DIRECTORY + existingFile + ".xml", overrides);
            Game.DisplayNotification($"~o~Scene Manager ~g~[Success]\n~w~Paths exported as ~b~{existingFile}.xml~w~.");
        }

        private static bool CanQuickSavePath(Paths.Path pathToExport) 
        {
            var GAME_DIRECTORY = Directory.GetCurrentDirectory();
            var SAVED_PATHS_DIRECTORY = GAME_DIRECTORY + "\\plugins\\SceneManager\\Saved Paths\\";
            if (!Directory.Exists(SAVED_PATHS_DIRECTORY))
            {
                Game.LogTrivial($"Directory '\\plugins\\SceneManager\\Saved Paths' does not exist.");
                return false;
            }

            var savedPathNames = Directory.GetFiles(SAVED_PATHS_DIRECTORY, "*.xml");
            return savedPathNames.Any(x => Path.GetFileNameWithoutExtension(x) == pathToExport.Name);
        }

        private static string GetNameForExistingCombinedPathsFile(IEnumerable<UIMenuCheckboxItem> checkedItems)
        {
            var checkedItemsText = checkedItems.Select(x => x.Text);
            foreach (KeyValuePair<string, List<Paths.Path>> kvp in PathManager.ImportedPaths)
            {
                var pathNames = kvp.Value.Select(x => x.Name);
                if (checkedItemsText.Count() == pathNames.Count() && checkedItemsText.All(x => pathNames.Contains(x)))
                {
                    Game.LogTrivial($"File \"{kvp.Key}\" contains all paths to be exported.  Quicksave.");
                    return kvp.Key;
                }
            }

            return "";
        }

        private static XmlAttributeOverrides DefineOverridesForCombinedPath()
        {
            XmlAttributeOverrides overrides = new XmlAttributeOverrides();
            XmlAttributes attr = new XmlAttributes();
            attr.XmlRoot = new XmlRootAttribute("Paths");
            overrides.Add(typeof(List<Paths.Path>), attr);

            return overrides;
        }
    }
}
