using RAGENativeUI;
using RAGENativeUI.Elements;
using System.Drawing;
using SceneManager.Utils;
using System.Collections.Generic;
using System.Linq;
using Rage;
using SceneManager.Managers;
using SceneManager.Paths;
using System;
using System.Windows.Forms;

namespace SceneManager.Menus
{
    internal class ImportPathMenu
    {
        internal static UIMenu Menu = new UIMenu("Scene Manager", "~o~Import Path Menu");
        internal static UIMenuItem Import { get; } = new UIMenuItem("Import", "Import the selected paths.");

        private static List<string> _HasBeenImported = new List<string>();

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
            PathManager.ImportPaths();
            foreach(KeyValuePair<string, List<Path>> kvp in PathManager.ImportedPaths)
            {
                var menuItem = new UIMenuCheckboxItem(kvp.Key, false);
                if(!_HasBeenImported.Contains(kvp.Key))
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
                var checkboxItems = Menu.MenuItems.Where(x => x.GetType() == typeof(UIMenuCheckboxItem));
                foreach(UIMenuCheckboxItem menuItem in checkboxItems)
                {
                    if(menuItem.Checked)
                    {
                        var pathsFromFile = PathManager.ImportedPaths.FirstOrDefault(x => x.Key == menuItem.Text).Value;
                        foreach(Path path in pathsFromFile)
                        {
                            if(PathManager.Paths.Any(x => x != null && x.Name == path.Name))
                            {
                                Game.DisplayHelp($"A path with the name ~b~{path.Name} ~w~already exists.  Do you want to replace it?  ~{Keys.Y.GetInstructionalId()}~ or ~{Keys.N.GetInstructionalId()}~");
                                GameFiber.Sleep(100);
                                GameFiber.SleepUntil(() => Game.IsKeyDown(Keys.Y) || Game.IsKeyDown(Keys.N), 8300);

                                if(Game.IsKeyDown(Keys.Y))
                                {
                                    var pathToReplace = PathManager.Paths.First(x => x.Name == path.Name);
                                    var pathToReplaceIndex = Array.IndexOf(PathManager.Paths, pathToReplace);
                                    pathToReplace.Delete();
                                    PathManager.Paths[pathToReplaceIndex] = path;
                                    path.Load();
                                    Rage.Native.NativeFunction.Natives.CLEAR_ALL_HELP_MESSAGES();
                                    _HasBeenImported.Add(PathManager.ImportedPaths.FirstOrDefault(x => x.Key == menuItem.Text).Key);
                                    continue;
                                }
                                else
                                {
                                    Game.DisplayNotification($"~o~Scene Manager ~y~[Import]\n~w~Path ~b~{path.Name} ~w~was not imported.");
                                    Rage.Native.NativeFunction.Natives.CLEAR_ALL_HELP_MESSAGES();
                                    continue;
                                }
                            }

                            var firstNullPathIndex = Array.IndexOf(PathManager.Paths, PathManager.Paths.First(x => x == null));
                            PathManager.Paths[firstNullPathIndex] = path;
                            path.Load();
                            _HasBeenImported.Add(PathManager.ImportedPaths.FirstOrDefault(x => x.Key == menuItem.Text).Key);
                        }

                        var numberOfNonNullPaths = PathManager.Paths.Where(x => x != null).Count();
                        Game.LogTrivial($"{menuItem.Text} added to paths collection.  Paths count: {numberOfNonNullPaths}");
                        Menu.RefreshIndex();
                    }
                }

                MenuManager.BuildMenus();
                Menu.Visible = true;
            }
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
