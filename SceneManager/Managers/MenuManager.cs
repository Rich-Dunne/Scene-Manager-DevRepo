using RAGENativeUI;
using SceneManager.Menus;
using SceneManager.Utils;
using Rage;
using System.Linq;
using RAGENativeUI.Elements;
using System.Drawing;

namespace SceneManager.Managers
{
    // The only reason this class should change is to modify how menus are are being handled
    internal static class MenuManager
    {
        internal static MenuPool MenuPool { get; } = new MenuPool();

        internal static void InitializeMenus()
        {
            MainMenu.Initialize();
            SettingsMenu.Initialize();
            ImportPathMenu.Initialize();
            PathMainMenu.Initialize();
            PathCreationMenu.Initialize();
            ExportPathMenu.Initialize();
            DriverMenu.Initialize();
            BarrierMenu.Initialize();
            EditPathMenu.Initialize();
            EditWaypointMenu.Initialize();

            BuildMenus();
            ColorMenuItems();
            DefineMenuMouseSettings();
        }

        private static void DefineMenuMouseSettings()
        {
            foreach (UIMenu menu in MenuPool)
            {
                menu.MouseControlsEnabled = false;
                menu.AllowCameraMovement = true;
            }
        }

        internal static void BuildMenus()
        {
            MainMenu.Build();
            SettingsMenu.Build();
            ImportPathMenu.Build();
            ExportPathMenu.Build();
            DriverMenu.Build();
            PathMainMenu.Build();
            PathCreationMenu.Build();
            EditPathMenu.Build();
            BarrierMenu.Build();
            foreach(UIMenu menu in MenuPool)
            {
                //Game.LogTrivial($"Setting with of {menu.SubtitleText} menu.");
                SetMenuWidth(menu);
            }
        }

        internal static void ColorMenuItems()
        {
            foreach(UIMenuItem menuItem in MenuPool.SelectMany(x => x.MenuItems))
            {
                if (menuItem.Enabled)
                {
                    menuItem.HighlightedBackColor = menuItem.ForeColor;
                }
                if(!menuItem.Enabled)
                {
                    menuItem.HighlightedBackColor = Color.DarkGray;
                    menuItem.DisabledForeColor = Color.Gray;
                }
            }
        }

        internal static void ProcessMenus()
        {
            while (MenuPool.Any(x => x.Visible))
            {
                MenuPool.ProcessMenus();
                ColorMenuItems();
                GameFiber.Yield();
            }
        }

        internal static void SetMenuWidth(UIMenu menu)
        {
            float MINIMUM_WIDTH = 0.25f;
            //float PADDING = 0.00390625f * 2; // typical padding used in RNUI
            float widthToAssign = MINIMUM_WIDTH;
            //float scrollerItemWidth = 0;
            //float totalWidth = 0;

            //Game.LogTrivial($"~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            foreach(UIMenuItem menuItem in menu.MenuItems)
            {
                //Game.LogTrivial($"========== Menu Item: {menuItem.Text}");
                float textWidth = menuItem.GetTextWidth();
                //Game.LogTrivial($"Menu Item Text Width: {textWidth}");

                float newWidth = textWidth;
                if (menuItem.GetType() == typeof(UIMenuListScrollerItem<string>))
                {
                    var scrollerItem = menuItem as UIMenuListScrollerItem<string>;
                    float selectedItemTextWidth = scrollerItem.GetSelectedItemTextWidth();
                    //Game.LogTrivial($"Menu Item Scroller Text Width: {selectedItemTextWidth}");
                    
                    //totalWidth = textWidth + selectedItemTextWidth;
                    //Game.LogTrivial($"Total Width: {totalWidth}");

                    newWidth += selectedItemTextWidth * 1.3f;
                    //Game.LogTrivial($"========== New Width from Longer Selected Item Text: {newWidth}");
                }

                if (menuItem.LeftBadge != UIMenuItem.BadgeStyle.None)
                {
                    newWidth += 0.02f;
                }

                if(newWidth > 0.25f && menuItem.LeftBadge != UIMenuItem.BadgeStyle.None)
                {
                    newWidth += 0.02f;
                }

                if (newWidth > widthToAssign)
                {
                    widthToAssign = newWidth;
                }
            }
            menu.Width = widthToAssign;
        }
    }
}
