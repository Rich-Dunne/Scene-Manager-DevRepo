using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using SceneManager.Managers;
using SceneManager.Paths;
using SceneManager.Utils;
using SceneManager.Waypoints;

namespace SceneManager.Menus
{
    internal static class PathMainMenu
    {
        private static int MAX_PATH_LIMIT { get; } = 10;
        internal static UIMenu Menu { get; } = new UIMenu("Scene Manager", "~o~Path Manager");
        internal static UIMenuItem CreateNewPath { get; } = new UIMenuItem("Create New Path");
        internal static UIMenuItem ImportPath { get; } = new UIMenuItem("Import Path", "Import a saved path from ~b~plugins/SceneManager/Saved Paths");
        internal static UIMenuItem DeleteAllPaths { get; } = new UIMenuItem("Delete All Paths");
        internal static UIMenuListScrollerItem<string> EditPath { get; private set; }
        internal static UIMenuCheckboxItem DisableAllPaths { get; } = new UIMenuCheckboxItem("Disable All Paths", false);

        internal static void Initialize()
        {
            Menu.ParentMenu = MainMenu.Menu;
            MenuManager.MenuPool.Add(Menu);

            Menu.OnItemSelect += PathMenu_OnItemSelected;
            Menu.OnCheckboxChange += PathMenu_OnCheckboxChange;
            Menu.OnMenuOpen += PathMenu_OnMenuOpen;
        }

        internal static void Build()
        {
            MenuManager.MenuPool.CloseAllMenus();
            Menu.Clear();

            Menu.AddItem(CreateNewPath);
            CreateNewPath.ForeColor = Color.Gold;
            Menu.AddItem(ImportPath);
            ImportPath.ForeColor = Color.Gold;
            ImportPath.Enabled = Settings.ImportedPaths.Count() > 0;
            Menu.AddItem(EditPath = new UIMenuListScrollerItem<string>("Edit Path", "Options to ~b~edit path waypoints~w~, ~b~disable the path~w~, ~b~export the path~w~, or ~b~delete the path~w~.", PathManager.Paths.Select(x => x.Name)));
            EditPath.ForeColor = Color.Gold;
            Menu.AddItem(DisableAllPaths);
            DisableAllPaths.Enabled = true;
            Menu.AddItem(DeleteAllPaths);
            DeleteAllPaths.Enabled = true;
            DeleteAllPaths.ForeColor = Color.Gold;

            if (PathManager.Paths.Count == MAX_PATH_LIMIT)
            {
                CreateNewPath.Enabled = false;
                ImportPath.Enabled = false;
            }
            if (PathManager.Paths.Count == 0)
            {
                EditPath.Enabled = false;
                DeleteAllPaths.Enabled = false;
                DisableAllPaths.Enabled = false;
            }
            if(Settings.ImportedPaths.Count == 0)
            {
                ImportPath.Enabled = false;
            }

            MenuManager.MenuPool.RefreshIndex();
        }

        private static void PathMenu_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == CreateNewPath)
            {
                GoToPathCreationMenu();
            }

            if(selectedItem == ImportPath)
            {
                GoToImportMenu();
            }

            if (selectedItem == EditPath)
            {
                GoToEditPathMenu();
            }

            if (selectedItem == DeleteAllPaths)
            {
                PathManager.DeleteAllPaths();
                DisableAllPaths.Checked = false;
                Build();
                Menu.Visible = true;
                BarrierMenu.BuildMenu();
            }
        }

        private static void PathMenu_OnCheckboxChange(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            if (checkboxItem == DisableAllPaths)
            {
                PathManager.ToggleAllPaths(DisableAllPaths.Checked);
            }
        }

        private static void PathMenu_OnMenuOpen(UIMenu menu)
        {
            var scrollerItems = new List<UIMenuScrollerItem> { EditPath };
            GameFiber.StartNew(() => UserInput.InitializeMenuMouseControl(menu, scrollerItems), "RNUI Mouse Input Fiber");
        }

        private static void GoToPathCreationMenu()
        {
            if (PathCreationMenu.PathCreationState == State.Creating)
            {
                Menu.Visible = false;
                PathCreationMenu.Menu.Visible = true;
                Path currentPath = PathManager.Paths.FirstOrDefault(x => x.State == State.Creating);
                Game.DisplayNotification($"~o~Scene Manager~y~[Creating]\n~w~Resuming path {currentPath.Number}");
            }
            else
            {
                //PathCreationMenu.BuildPathCreationMenu();
                Menu.Visible = false;
                PathCreationMenu.Menu.Visible = true;
            }
        }

        private static void GoToImportMenu()
        {
            Menu.Visible = false;
            ImportPathMenu.Menu.Visible = true;
        }

        private static void GoToEditPathMenu()
        {
            Menu.Visible = false;
            EditPathMenu.Menu.Visible = true;
        }
    }
}
