using RAGENativeUI;
using RAGENativeUI.Elements;
using System.Drawing;
using SceneManager.Objects;
using SceneManager.Utils;
using System.Collections.Generic;
using System.Linq;
using Rage;

namespace SceneManager.Menus
{
    internal class ImportPathMenu
    {
        internal static UIMenu Menu = new UIMenu("Scene Manager", "~o~Import Path Menu");
        private static UIMenuItem menuItem;

        internal static void Initialize()
        {
            Menu.ParentMenu = PathMainMenu.Menu;
            MenuManager.MenuPool.Add(Menu);

            Menu.OnItemSelect += ImportPathMenu_OnItemSelect;
            Menu.OnMenuOpen += ImportPathMenu_OnMenuOpen;
        }

        internal static void BuildImportMenu()
        {
            Menu.Clear();
            foreach(Path path in Settings.ImportedPaths)
            {
                Menu.AddItem(menuItem = new UIMenuItem(path.Name));
                menuItem.ForeColor = Color.Gold;
            }
        }

        private static void ImportPathMenu_OnMenuOpen(UIMenu menu)
        {
            GameFiber.StartNew(() => UserInput.InitializeMenuMouseControl(menu, new List<UIMenuScrollerItem>()), "RNUI Mouse Input Fiber");
            
            // Disable menu item if PathManager.Paths contains a path with a matching name
            foreach (UIMenuItem menuItem in menu.MenuItems)
            {
                menuItem.Enabled = !PathManager.Paths.Any(x => x.Name == menuItem.Text);
            }
        }

        private static void ImportPathMenu_OnItemSelect(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            // When the user clicks on a path, that path needs to be added from Settings.importedPaths to PathMainMenu.paths
            Path importedPath = PathManager.ImportPath(Settings.ImportedPaths.FirstOrDefault(x => x.Name == selectedItem.Text));
            importedPath.Load();
            Game.LogTrivial($"{selectedItem.Text} added to paths collection as path #{importedPath.Number}.  Paths count: {PathManager.Paths.Count}");
            selectedItem.Enabled = false;

            // Refresh path main menu
            PathMainMenu.BuildPathMenu();
            PathMainMenu.Menu.RefreshIndex();
            Menu.Visible = true;
        }
    }
}
