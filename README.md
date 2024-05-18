# Scene Manager

## INSTALLATION
Drag and drop the contents from within the downloaded GTA V folder into where your GTA V is installed.  Ensure the plugin is set to load on startup in the RagePluginHook settings or load it manually via the in-game console.
![How to drag and drop](https://i.imgur.com/OzQWv2B.jpg)

## GET SUPPORT AND REPORT PROBLEMS
For the fastest support, [join my Discord](https://discord.gg/cUQaTNQ) and ask your question in the **correct category/channel**.  For slower support, [use this thread on the LSPDFR forums](https://www.lcpdfr.com/forums/topic/107730-richs-plugin-support-thread/).

## HOW TO USE SCENE MANAGER
### Using the Menus:

The default keybind to open the menu is Left Shift and T.

Menu options with gold-colored text are selectable, which means when you select these menu items, something will happen (opening a new menu, adding a waypoint, placing a barrier, etc).  Menu options with white-colored text are interactable (can be scrolled through, for example), but nothing will happen if you try to select them.

![Menus](https://i.imgur.com/GQNZrm4.jpg)

### Creating Paths:
1.  To create paths, open the Scene Manager menu by pressing Left Shift + T (default).  Select "Path Menu," then "Create New Path."  
2.  In the Path Creation menu, you can specify different waypoint options, as well as add and remove waypoints.  The settings for each waypoint are how the AI will drive to **that** waypoint from the **previous** waypoint.
3.  Adding a waypoint will create a waypoint at your mouse position in the world (where you're aiming).
4.  The first waypoint of a path **must** be a collector waypoint.  If you have 3D waypoints enabled, the blue marker is the collection radius and the orange marker is the speed zone radius.  **NOTE:** Not every waypoint has to be a collector waypoint.  Too many collector waypoints will impact the performance of lower-end systems. 
5.  As you add and remove waypoints, blips and world markers will appear and disappear if enabled in your settings.  Blips are numbered to correspond with their path.  Blips and markers are also colored to designate the following:  Blue are collector waypoints, green are Drive waypoints, and red are Stop waypoints.
6.  The path will activate once you select "End path creation," and cars will automatically follow the path when they are within the collection radius.
7.  After creating paths, you can delete them via the Path Menu.

![Creating a path](https://i.imgur.com/v7s1QMf.jpg)
![Finishg a path](https://i.imgur.com/75OhNS5.jpg)

### Other Path Menu Options
1.  The "Direct nearest driver to path" option in the Path Manager Main Menu allows you to manually direct the nearest vehicle to a path of your choice.  You may either direct a driver to the nearest path's first waypoint or the nearest waypoint in front of the driver.

2.  The "Dismiss nearest driver" option in the Path Manager Main Menu allows you to dismiss vehicles from their path, their current waypoint, or from their current position.  Dismissing from a path will clear all driver tasks and they will no longer be controlled by the Scene Manager.  Dismissing from their current waypoint will either give the driver their next waypoint task or dismiss them from the path if they were already going to the path's final waypoint.  The Dismiss from World option will delete the vehicle and all occupants from the world.

![Other path menu options](https://i.imgur.com/4PIbnYM.jpg)

### Editing Paths and Waypoints
1.  In the Edit Path menu, you can disable and delete individual paths.  When a path is disabled, it will not collect any new vehicles at collector waypoints.  However, vehicles can still be manually directed to disabled paths.
2.  In the Edit Waypoint menu, you can change the settings of each path's individual waypoints.  
3.  You can add new waypoints to the end of the path by selecting the Add as New Waypoint option in the Edit Waypoint menu.  You should have the Update Waypoint Position option checked as you do this since it will create a 3D marker (if enabled in the settings) around your player so you can see the waypoint's settings as you change them.

![Edit path waypoints](https://i.imgur.com/DzwuK4t.jpg)

### Barrier Management
1.  To place barriers, open the Scene Manager menu by pressing Left Shift + T (default).
2.  In the Barrier Menu, you may scroll through different types of road barriers, as well as place and remove barriers.
3.  While the barrier menu is open and Spawn Barrier or Rotate Barrier are highlighted, you will see a "shadow" barrier.  The shadow barrier shows the barrier you currently have selected, its rotation, and the position it will be spawned.
4.  The spawn position is wherever you aim your mouse.  If you aim your mouse too far away, the shadow barrier will disappear and you won't be able to spawn the barrier.  You can change the maximum spawn distance in the plugin's .ini file.
5.  The AI will not drive around barriers on its own.  Barrier placement should be done in conjunction with your paths.

![Barrier menu](https://i.imgur.com/Sp3uc7c.jpg)
![Placing barriers](https://i.imgur.com/8YXzlWN.jpg)

### Other Notes
1.  Paths and barriers will remain in the world after you die.  Be sure to delete them when you're done!
2.  The first waypoint of a path **must** be a collector waypoint.  If you edit the only waypoint of a path to not be a collector, it will automatically be turned into a collector.
3.  Some settings in Scene Manager's .ini file can be updated in-game using the Settings menu.  Saving these settings in-game will update the .ini so you don't have to change the settings every time you load the plugin.
4.  You can add custom barrier objects to the barrier menu by adding them to the Barriers section of the .ini file.  A link is provided in the .ini file to find object model names.
5.  You should have the 3D Waypoints setting enabled anytime you work with waypoints.

![Scene overview](https://i.imgur.com/Rd5Z5qe.jpg)

## Using the API
For plugin developers who would like to utilize the Scene Manager API, you must first resolve the assembly in your plugin since Scene Manager is not in the GTA V root directory.  Including the following code when your plugin is initialized should work:

```
private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
{
    if (args.Name.StartsWith("SceneManager"))
    {
        return Assembly.Load(File.ReadAllBytes(@"Plugins\SceneManager.dll"));
    }

    return null;
}
```

As of V2.3.3, the API includes two functions: `LoadPathsFromFile` and `DeleteLoadedPaths`.  `LoadPathsFromFile` has one mandatory parameter, the file name for the path, and an optional parameter where you can specify the file path location.  By default, all paths are loaded from `SceneManager/Saved Paths`.  When you load paths through the API, they are loaded using a separate instance of Scene Manager, so you cannot interact with them using the traditional Scene Manager menu.

When you're finished with the path, you should call the `DeleteLoadedPaths` method to clean everything up.

## CREDITS
* Author: Rich
* Additional credit:  PNWParksFan for code assistance and extensive testing, Sereous, OJdoesIt, Vincentsgm, EchoWolf, FactionsBrutus
