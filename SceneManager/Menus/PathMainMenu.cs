using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using SceneManager.Managers;
using SceneManager.Paths;
using SceneManager.Utils;

namespace SceneManager.Menus
{
    internal static class PathMainMenu
    {
        internal static UIMenu Menu { get; } = new UIMenu("Scene Manager", "~o~Path Manager");
        internal static UIMenuItem CreateNewPath { get; } = new UIMenuItem("Create New Path");
        internal static UIMenuItem ImportPath { get; } = new UIMenuItem("Import Paths", "Import saved paths from ~b~plugins/SceneManager/Saved Paths");
        internal static UIMenuItem ExportPath { get; } = new UIMenuItem("Export Paths", "Export selected paths to ~b~plugins/SceneManager/Saved Paths");
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
            ImportPath.Enabled = PathManager.ImportedPaths.Count > 0;
            Menu.AddItem(ExportPath);
            ExportPath.ForeColor = Color.Gold;
            ExportPath.Enabled = PathManager.Paths.Any(x => x != null);
            Menu.AddItem(EditPath = new UIMenuListScrollerItem<string>("Edit Path", "Options to ~b~edit path waypoints~w~, ~b~disable the path~w~, ~b~change path name~w~, or ~b~delete the path~w~.", PathManager.Paths.Where(x => x != null).Select(x => x.Name)));
            EditPath.ForeColor = Color.Gold;
            Menu.AddItem(DisableAllPaths);
            DisableAllPaths.Enabled = true;
            Menu.AddItem(DeleteAllPaths);
            DeleteAllPaths.Enabled = true;
            DeleteAllPaths.ForeColor = Color.Gold;

            if (PathManager.Paths.All(x => x != null))
            {
                CreateNewPath.Enabled = false;
                ImportPath.Enabled = false;
            }
            if (PathManager.Paths.All(x => x == null))
            {
                EditPath.Enabled = false;
                DeleteAllPaths.Enabled = false;
                DisableAllPaths.Enabled = false;
            }
            if(PathManager.ImportedPaths.Count == 0)
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

            if(selectedItem == ExportPath)
            {
                GoToExportMenu();
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
                BarrierMenu.Build();
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
                Path currentPath = PathManager.Paths.FirstOrDefault(x => x != null && x.State == State.Creating);
                Game.DisplayNotification($"~o~Scene Manager~y~[Creating]\n~w~Resuming path ~b~{currentPath.Name}~w~.");
            }
            else
            {
                Menu.Visible = false;
                PathCreationMenu.Menu.Visible = true;
            }
        }

        private static void GoToImportMenu()
        {
            Menu.Visible = false;
            ImportPathMenu.Menu.Visible = true;
        }

        private static void GoToExportMenu()
        {
            Menu.Visible = false;
            ExportPathMenu.Menu.Visible = true;
        }

        private static void GoToEditPathMenu()
        {
            Menu.Visible = false;
            EditPathMenu.CurrentPath = PathManager.Paths[EditPath.Index];
            EditPathMenu.Menu.Visible = true;
        }
    }
}
