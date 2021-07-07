using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;
using SceneManager.Managers;
using SceneManager.Menus;
using SceneManager.Paths;
using SceneManager.Utils;

namespace SceneManager
{
    internal class EditPathMenu
    {
        internal static UIMenu Menu { get; } = new UIMenu("Scene Manager", "~o~Edit Path");
        private static UIMenuCheckboxItem DisablePath { get; } = new UIMenuCheckboxItem("Disable Path Collection", false);
        private static UIMenuItem EditWaypoints { get; } = new UIMenuItem("Edit Waypoints");
        private static UIMenuItem DeletePath { get; } = new UIMenuItem("Delete Path");
        private static UIMenuItem ChangePathName { get; } = new UIMenuItem("Change Path Name");
        internal static Path CurrentPath { get; set; }

        internal static void Initialize()
        {
            Menu.ParentMenu = PathMainMenu.Menu;
            MenuManager.MenuPool.Add(Menu);

            Menu.OnItemSelect += EditPath_OnItemSelected;
            Menu.OnCheckboxChange += EditPath_OnCheckboxChange;
            Menu.OnMenuOpen += EditPath_OnMenuOpen;
        }

        internal static void Build()
        {
            Menu.Clear();

            Menu.AddItem(DisablePath);
            Menu.AddItem(EditWaypoints);
            EditWaypoints.ForeColor = Color.Gold;
            Menu.AddItem(ChangePathName);
            ChangePathName.ForeColor = Color.Gold;
            Menu.AddItem(DeletePath);
            DeletePath.ForeColor = Color.Gold;

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

            if (selectedItem == DeletePath)
            {
                var currentPath = PathManager.Paths[PathMainMenu.EditPath.Index];
                currentPath.Delete();
                PathMainMenu.Build();
                PathMainMenu.Menu.Visible = true;
                BarrierMenu.Build();
            }

            if(selectedItem == ChangePathName)
            {
                var currentPath = PathManager.Paths[PathMainMenu.EditPath.Index];
                currentPath.ChangeName();
                MenuManager.BuildMenus();
                Menu.Visible = true;
            }
        }

        private static void EditPath_OnCheckboxChange(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool @checked)
        {
            if (checkboxItem == DisablePath)
            {
                //var currentPath = PathManager.Paths[PathMainMenu.EditPath.Index];
                var currentPath = PathManager.Paths.FirstOrDefault(x => x.Name == PathMainMenu.EditPath.OptionText);
                if(currentPath == null)
                {
                    return;
                }

                if (DisablePath.Checked)
                {
                    currentPath.Disable();
                }
                else
                {
                    currentPath.Enable();
                }
            }
        }

        private static void EditPath_OnMenuOpen(UIMenu menu)
        {
            var scrollerItems = new List<UIMenuScrollerItem> {  };
            GameFiber.StartNew(() => UserInput.InitializeMenuMouseControl(menu, scrollerItems), "RNUI Mouse Input Fiber");
            if (CurrentPath == null)
            {
                Menu.SubtitleText = $"~o~Currently editing: ~r~[ERROR GETTING CURRENT PATH]";
                ChangePathName.Description = $"Change the path name from ~r~[ERROR] ~w~to something else.";
            }
            else
            {
                Menu.SubtitleText = $"~o~Currently editing: ~b~{CurrentPath.Name}";
                ChangePathName.Description = $"Change the path name from ~b~{CurrentPath.Name} ~w~to something else.";
            }
        }
    }
}
