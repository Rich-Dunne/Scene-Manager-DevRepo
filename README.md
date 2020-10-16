# Scene Manager

## INSTALLATION
Drag and drop the contents from within the downloaded GTA V folder into where your GTA V is installed.

## HOW-TO USE
### Using the Menus:
Menu options with gold colored text are selectable, which means when you select these menu items, something will happen (opening a new menu, adding a waypoint, placing a barrier, etc).  Menu options with white colored text are interactable (can be scrolled through, for example), but nothing will happen if you try to select them.

### Creating Paths:
1.  To create paths, open the Scene Manager menu by pressing Left Shift + T (default).  Select "Path Menu," then "Create New Path."  
2.  In the Path Creation menu, you can specify different waypoint options, as well as add and remove waypoints.  The settings for each waypoint are how the AI will drive to **that** waypoint from the **previous** waypoint.
3.  Adding a waypoint will create a waypoint at your player's position.
4.  As you add and remove waypoints, blips and world markers will appear and disappear if enabled in your settings.  Blips are numbered to correspond with their path.  Blips and markers are also colored to designate the following:  Blue are collector waypoints, green are Drive-To waypoints, and red are Stop waypoints.
5.  The path will activate once you select "End path creation," and cars will automatically follow the path when they are close to the first waypoint.
6.  After creating paths, you can delete them via the Path Menu.

### Other Path Menu Options
1.  The "Direct nearest driver to path" option in the Path Manager Main Menu allows you to manually direct the nearest vehicle to a path of your choice.  You may either direct a driver to the nearest path's first waypoint or the nearest waypoint in front of the driver.

2.  The "Dismiss nearest driver" option in the Path Manager Main Menu allows you to dismiss vehicles from their path, their current waypoint, or from their current position.  Dismissing from a path will clear all driver tasks and they will no longer be controlled by Scene Manager.  Dismissing from their current waypoint will either give the driver their next waypoint task, or dismiss them from the path if they were already going to the path's final waypoint.  Dismissing from current position should be used for vehicles which might be stuck, but are not controlled by Scene Manager

### Editing Paths and Waypoints
1.  In the Edit Path menu, you can disable and delete individual paths.  When a path is disabled, it will not collect any new vehicles at collector waypoints.  However, vehicles can still be manually directed to disabled paths.
2.  In the Edit Waypoint menu, you can change the settings of each path's individual waypoints.  
3.  You can add new waypoints to the end of the path by selecting the Add as New Waypoint option in the Edit Waypoint menu.  It is recommended to have the Update Waypoint Position option checked as you do this since it will create a 3D marker (if enabled in the settings) around your player so you can see the waypoint's settings as you change them.

### Barrier Management
1.  To place barriers, open the Scene Manager menu by pressing Left Shift + T (default).
2.  In the Barrier Menu, you may scroll through different types of road barriers, as well as place and remove barriers.
3.  The AI will not drive around barriers on their own.  Barrier placement should be in conjunction with your paths.

### Other Notes
1.  Paths and barriers will remain in the world after you die.  Be sure to delete them when you're done!
2.  The first waypoint of a path must be a collector waypoint.  If you edit the only waypoint of a path to not be a collector, it will automatically be turned into a collector.
3.  Some settings in Scene Manager's .ini file can be updated in-game using the Settings menu.  Saving these settings in-game will update the .ini so you don't have to change the settings every time you load the plugin.
4.  You can add custom barrier objects to the barrier menu by adding them to the Barriers section of the .ini file.  A link is provided in the .ini file to find object model names.

## CREDITS
* Author: Rich
* Additional credit:  PNWParksFan for code assistance and extensive testing, Sereous, OJdoesIt, Vincentsgm, EchoWolf, FactionsBrutus
