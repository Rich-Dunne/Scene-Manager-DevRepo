# Scene Manager

## INSTALLATION
Drag and drop the contents from within the downloaded GTA V folder into where your GTA V is installed.
![How to drag and drop](https://i.imgur.com/nnxqgtn.jpg)

## HOW TO USE SCENE MANAGER
### Using the Menus:
Menu options with gold colored text are selectable, which means when you select these menu items, something will happen (opening a new menu, adding a waypoint, placing a barrier, etc).  Menu options with white colored text are interactable (can be scrolled through, for example), but nothing will happen if you try to select them.

![Menus](https://i.imgur.com/dOCtdQN.jpg)

### Creating Paths:
1.  To create paths, open the Scene Manager menu by pressing Left Shift + T (default).  Select "Path Menu," then "Create New Path."  
2.  In the Path Creation menu, you can specify different waypoint options, as well as add and remove waypoints.  The settings for each waypoint are how the AI will drive to **that** waypoint from the **previous** waypoint.
3.  Adding a waypoint will create a waypoint at your player's position.
4.  The first waypoint of a path **must** be a collector waypoint.  If you have 3D waypoints enabled, the blue marker is the collection radius and the orange marker is the speed zone radius.
5.  As you add and remove waypoints, blips and world markers will appear and disappear if enabled in your settings.  Blips are numbered to correspond with their path.  Blips and markers are also colored to designate the following:  Blue are collector waypoints, green are Drive waypoints, and red are Stop waypoints.
6.  The path will activate once you select "End path creation," and cars will automatically follow the path when they are within the collection radius.
7.  After creating paths, you can delete them via the Path Menu.

![Creating a path](https://i.imgur.com/h52Y2SY.jpg)
![Finishg a path](https://i.imgur.com/MGGfGQB.jpg)

### Other Path Menu Options
1.  The "Direct nearest driver to path" option in the Path Manager Main Menu allows you to manually direct the nearest vehicle to a path of your choice.  You may either direct a driver to the nearest path's first waypoint or the nearest waypoint in front of the driver.

2.  The "Dismiss nearest driver" option in the Path Manager Main Menu allows you to dismiss vehicles from their path, their current waypoint, or from their current position.  Dismissing from a path will clear all driver tasks and they will no longer be controlled by Scene Manager.  Dismissing from their current waypoint will either give the driver their next waypoint task, or dismiss them from the path if they were already going to the path's final waypoint.  The Dismiss from World option will delete the vehicle and all occupants from the world.

![Other path menu options](https://i.imgur.com/3tpiitR.jpg)

### Editing Paths and Waypoints
1.  In the Edit Path menu, you can disable and delete individual paths.  When a path is disabled, it will not collect any new vehicles at collector waypoints.  However, vehicles can still be manually directed to disabled paths.
2.  In the Edit Waypoint menu, you can change the settings of each path's individual waypoints.  
3.  You can add new waypoints to the end of the path by selecting the Add as New Waypoint option in the Edit Waypoint menu.  It is recommended to have the Update Waypoint Position option checked as you do this since it will create a 3D marker (if enabled in the settings) around your player so you can see the waypoint's settings as you change them.

![Edit path waypoints](https://i.imgur.com/V8q5wvp.jpg)

### Barrier Management
1.  To place barriers, open the Scene Manager menu by pressing Left Shift + T (default).
2.  In the Barrier Menu, you may scroll through different types of road barriers, as well as place and remove barriers.
3.  While the barrier menu is open and Spawn Barrier or Rotate Barrier are highlighted, you will see a "shadow" barrier.  The shadow barrier shows the barrier you currently have selected, its rotation, and the position it will be spawned at.
4.  The spawn position is wherever you aim your mouse.  If you aim your mouse too far away, the shadow barrier will disappear and you won't be able to spawn the barrier.  You can change the maximum spawn distance in the plugin's .ini file.
5.  The AI will not drive around barriers on their own.  Barrier placement should be done in conjunction with your paths.

![Barrier menu](https://i.imgur.com/kxMGhIF.jpg)
![Placing barriers](https://i.imgur.com/FanVlGP.jpg)

### Other Notes
1.  Paths and barriers will remain in the world after you die.  Be sure to delete them when you're done!
2.  The first waypoint of a path must be a collector waypoint.  If you edit the only waypoint of a path to not be a collector, it will automatically be turned into a collector.
3.  Some settings in Scene Manager's .ini file can be updated in-game using the Settings menu.  Saving these settings in-game will update the .ini so you don't have to change the settings every time you load the plugin.
4.  You can add custom barrier objects to the barrier menu by adding them to the Barriers section of the .ini file.  A link is provided in the .ini file to find object model names.

![Scene overview](https://i.imgur.com/lB2pdh6.jpg)

## CREDITS
* Author: Rich
* Additional credit:  PNWParksFan for code assistance and extensive testing, Sereous, OJdoesIt, Vincentsgm, EchoWolf, FactionsBrutus
