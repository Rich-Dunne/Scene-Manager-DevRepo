using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Rage;
using RAGENativeUI;
using RAGENativeUI.Elements;

namespace SceneManager
{
    public static class TrafficMenu
    {
        private static MenuPool _menuPool;
        private static UIMenu mainMenu, trafficMenu, coneMenu, pathCreationMenu;
        private static UIMenuItem navigateToTrafficMenu, navigateToConeMenu, createNewPath, trafficAddWaypoint, trafficRemoveWaypoint, trafficEndPath, deleteAllPaths, addCone, removeLastCone, removeNearestCone, removeAllCones, dismissDriver;
        private static UIMenuListItem deleteSinglePath, selectCone, waypointType, waypointSpeed, directDriver;

        private static List<dynamic> pathsNum = new List<dynamic>() { };
        private static List<PathData> paths = new List<PathData>() { };
        private static List<Rage.Object> cones = new List<Rage.Object>() { };

        private static List<dynamic> waypointSpeeds = new List<dynamic>() { 5, 10, 15, 20, 30, 40, 50, 60, 70 };
        private static List<dynamic> waypointTypes = new List<dynamic>() { "Drive To", "Stop" };
        private static VehicleDrivingFlags[] drivingFlags = new VehicleDrivingFlags[] { VehicleDrivingFlags.Normal, VehicleDrivingFlags.StopAtDestination };
        private static List<dynamic> coneList = new List<dynamic>() { "Large Striped Cone", "Large Cone", "Medium Striped Cone", "Medium Cone", "Roadpole A", "Roadpole B" };
        private static string[] coneObjectNames = new string[] { "prop_mp_cone_01", "prop_roadcone01c", "prop_mp_cone_02", "prop_mp_cone_03", "prop_roadpole_01a", "prop_roadpole_01b" };

        private static Rage.Object shadowCone;

        public static void CheckUserInput()
        {
            #pragma warning disable CS0618 // Type or member is obsolete, clear NUI squiggles in BuildMenu
            AppDomain.CurrentDomain.DomainUnload += MyTerminationHandler;

            BuildMenu();

            while (true)
            {
                // Keyboard
                if (EntryPoint.Settings.ModifierKey == System.Windows.Forms.Keys.None)
                {
                    if (Game.LocalPlayer.Character.IsOnFoot && Game.IsKeyDown(EntryPoint.Settings.ToggleKey) && !trafficMenu.Visible && !pathCreationMenu.Visible)
                    {
                        mainMenu.Visible = !mainMenu.Visible;
                    }
                }
                else if (Game.LocalPlayer.Character.IsOnFoot && Game.IsKeyDownRightNow(EntryPoint.Settings.ModifierKey) && Game.IsKeyDown(EntryPoint.Settings.ToggleKey) && !trafficMenu.Visible && !pathCreationMenu.Visible)
                {
                    mainMenu.Visible = !mainMenu.Visible;
                }

                // Controller
                if (EntryPoint.Settings.ModifierButton == ControllerButtons.None)
                {
                    if (Game.LocalPlayer.Character.IsOnFoot && Game.IsControllerButtonDown(EntryPoint.Settings.ToggleButton) && !trafficMenu.Visible && !pathCreationMenu.Visible)
                    {
                        mainMenu.Visible = !mainMenu.Visible;
                    }
                }
                else if (Game.LocalPlayer.Character.IsOnFoot && Game.IsControllerButtonDownRightNow(EntryPoint.Settings.ModifierButton) && Game.IsControllerButtonDown(EntryPoint.Settings.ToggleButton) && !trafficMenu.Visible && !pathCreationMenu.Visible)
                {
                    mainMenu.Visible = !mainMenu.Visible;
                }

                _menuPool.ProcessMenus();
                GameFiber.Yield();
            }
        }

        private static void BuildMenu()
        {
            _menuPool = new MenuPool();

            // Instantiate menus
            mainMenu = new UIMenu("Scene Manager", "");
            trafficMenu = new UIMenu("Scene Manager", "~o~Traffic Menu");
            trafficMenu.ParentMenu = mainMenu;
            pathCreationMenu = new UIMenu("Scene Manager", "~o~Path Creation");
            pathCreationMenu.ParentMenu = trafficMenu;
            coneMenu = new UIMenu("Scene Manager", "~o~Cone Menu");
            coneMenu.ParentMenu = mainMenu;

            // Add menus to the pool
            _menuPool.Add(mainMenu);
            _menuPool.Add(trafficMenu);
            _menuPool.Add(coneMenu);
            _menuPool.Add(pathCreationMenu);

            // Add menu items to main menu and navigate each item to a submenu
            mainMenu.AddItem(navigateToTrafficMenu = new UIMenuItem("~o~Traffic Menu"));
            mainMenu.BindMenuToItem(trafficMenu, navigateToTrafficMenu);
            mainMenu.AddItem(navigateToConeMenu = new UIMenuItem("~o~Cone Menu"));
            mainMenu.BindMenuToItem(coneMenu, navigateToConeMenu);

            // Add menu items to trafficMenu
            trafficMenu.AddItem(createNewPath = new UIMenuItem("Create New Path"));
            trafficMenu.AddItem(deleteSinglePath = new UIMenuListItem("Delete Path", pathsNum, 0));
            deleteSinglePath.Enabled = false;
            trafficMenu.AddItem(deleteAllPaths = new UIMenuItem("Delete All Paths"));
            deleteAllPaths.Enabled = false;
            trafficMenu.AddItem(directDriver = new UIMenuListItem("Direct nearest driver to path", pathsNum, 0));
            directDriver.Enabled = false;
            trafficMenu.AddItem(dismissDriver = new UIMenuItem("Dismiss nearest driver"));

            // Add menu items to pathCreationMenu
            pathCreationMenu.AddItem(waypointType = new UIMenuListItem("Waypoint Type", waypointTypes, 0));
            pathCreationMenu.AddItem(waypointSpeed = new UIMenuListItem("Waypoint Speed", waypointSpeeds, 0));
            pathCreationMenu.AddItem(trafficAddWaypoint = new UIMenuItem("Add waypoint"));
            pathCreationMenu.AddItem(trafficRemoveWaypoint = new UIMenuItem("Remove last waypoint"));
            trafficRemoveWaypoint.Enabled = false;
            pathCreationMenu.AddItem(trafficEndPath = new UIMenuItem("End path creation"));

            // Add menu items to coneMenu
            coneMenu.AddItem(selectCone = new UIMenuListItem("Select Cone", coneList, 0));
            coneMenu.AddItem(addCone = new UIMenuItem("Add Cone"));
            coneMenu.AddItem(removeLastCone = new UIMenuItem("Remove Last Cone"));
            removeLastCone.Enabled = false;
            coneMenu.AddItem(removeNearestCone = new UIMenuItem("Remove Nearest Cone"));
            removeNearestCone.Enabled = false;
            coneMenu.AddItem(removeAllCones = new UIMenuItem("Remove All Cones"));
            removeAllCones.Enabled = false;

            mainMenu.RefreshIndex();
            trafficMenu.RefreshIndex();
            pathCreationMenu.RefreshIndex();
            coneMenu.RefreshIndex();

            // Event handlers for when a menu item is selected
            mainMenu.OnItemSelect += MainMenu_OnItemSelected;
            trafficMenu.OnItemSelect += TrafficMenu_OnItemSelected;
            pathCreationMenu.OnItemSelect += PathCreation_OnItemSelected;
            coneMenu.OnListChange += ConeMenu_OnListChange;
            coneMenu.OnItemSelect += ConeMenu_OnItemSelected;

            // Disable mouse control for the menus
            mainMenu.MouseControlsEnabled = false;
            mainMenu.AllowCameraMovement = true;
            trafficMenu.MouseControlsEnabled = false;
            trafficMenu.AllowCameraMovement = true;
            pathCreationMenu.MouseControlsEnabled = false;
            pathCreationMenu.AllowCameraMovement = true;
            coneMenu.MouseControlsEnabled = false;
            coneMenu.AllowCameraMovement = true;
        }

        private static void RebuildTrafficMenu()
        {
            // The traffic menu has to be "refreshed" in some instances to show changes, so we do that here
            _menuPool.CloseAllMenus();
            trafficMenu.Clear();
            trafficMenu.AddItem(createNewPath = new UIMenuItem("Create New Path"));
            trafficMenu.AddItem(deleteSinglePath = new UIMenuListItem("Delete Path", pathsNum, 0));
            trafficMenu.AddItem(deleteAllPaths = new UIMenuItem("Delete All Paths"));
            trafficMenu.AddItem(directDriver = new UIMenuListItem("Direct nearest driver to path", pathsNum, 0));
            trafficMenu.AddItem(dismissDriver = new UIMenuItem("Dismiss nearest driver"));

            if (paths.Count == 8)
            {
                createNewPath.Enabled = false;
            }
            if (paths.Count == 0)
            {
                deleteSinglePath.Enabled = false;
                deleteAllPaths.Enabled = false;
                directDriver.Enabled = false;
            }
            _menuPool.RefreshIndex();
            trafficMenu.Visible = true;
        }

        private static void DeletePath(PathData path, int index, UIMenuItem selectedItem)
        {
            // Before deleting a path, we need to dismiss any vehicles controlled by that path and remove the vehicles from ControlledVehicles
            //Game.LogTrivial($"Deleting path {index+1}");
            Game.LogTrivial($"Deleting path {path.WaypointData[0].Path}");
            var matchingVehicle = TrafficPathing.ControlledVehicles.Where(cv => cv.Value.Path == path.WaypointData[0].Path).ToList();
            Game.LogTrivial($"Running foreach loop");
            foreach (KeyValuePair<string, ControlledVehicle> cv in matchingVehicle)
            {
                if (cv.Value.Vehicle.Exists() && cv.Value.Vehicle.IsValid() && cv.Value.Vehicle.Driver.Exists() && cv.Value.Vehicle.Driver.IsValid())
                {
                    cv.Value.DismissNow = true;
                    cv.Value.Vehicle.Driver.Tasks.Clear();
                    cv.Value.Vehicle.Driver.Dismiss();
                    TrafficPathing.ControlledVehicles.Remove(cv.Value.LicensePlate);
                    //Game.LogTrivial($"{cv.vehicle.Model.Name} cleared from path {cv.path}");
                }
            }
            Game.LogTrivial($"Remove all vehicles in the path");
            //TrafficPathing.ControlledVehicles.RemoveAll(cv => cv.Path == path.WaypointData[0].Path);

            // Remove the speed zone so cars don't continue to be affected after the path is deleted
            foreach (WaypointData wd in path.WaypointData)
            {
                if (wd == path.WaypointData[0])
                {
                    World.RemoveSpeedZone(wd.YieldZone);
                }
                wd.WaypointBlip.Delete();
            }
            path.WaypointData.Clear();               

            // Manipulating the menu to reflect specific paths being deleted
            if (selectedItem == deleteSinglePath)
            {
                Game.LogTrivial($"Path {path.PathNum} deleted.");
                Game.DisplayNotification($"~o~Scene Manager\n~w~Path {path.PathNum} deleted.");
                paths.RemoveAt(index);
                //Game.LogTrivial("pathsNum count: " + pathsNum.Count);
                //Game.LogTrivial("index: " + index);
                pathsNum.RemoveAt(index);
                RebuildTrafficMenu();
            }
        }

        private static void TrafficMenu_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == createNewPath)
            {
                trafficMenu.Visible = false;
                pathCreationMenu.Visible = true;

                // For each element in paths, determine if the element exists but is not finished yet, or if it doesn't exist, create it.
                for(int i = 0; i <= paths.Count; i++)
                {
                    if (paths.ElementAtOrDefault(i) != null && paths[i].PathFinished == false)
                    {
                        //Game.LogTrivial($"pathFinished: {paths[i].PathFinished}");
                        Game.LogTrivial($"Resuming path {i+1}");
                        Game.DisplayNotification($"~o~Scene Manager\n~y~[Creating]~w~ Resuming path {i+1}");
                        break;
                    }
                    else if (paths.ElementAtOrDefault(i) == null)
                    {
                        Game.LogTrivial($"Creating path {i+1}");
                        Game.DisplayNotification($"~o~Scene Manager\n~y~[Creating]~w~ Path {i+1} started.");
                        paths.Insert(i, new PathData(i+1,false));// { pathNum = i+1, pathFinished = false });
                        trafficRemoveWaypoint.Enabled = false;
                        break;
                    }
                }
            }

            if (selectedItem == deleteSinglePath)
            {
                //Game.LogTrivial("pathsNum has " + pathsNum.Count + " items before deleting a path.");
                //Game.LogTrivial("deletePath index is " + deletePath.Index);
                //Game.LogTrivial("deletePath selectedPath is " + deletePath.IndexToItem(deletePath.Index));
                DeletePath(paths[deleteSinglePath.Index], deleteSinglePath.Index, deleteSinglePath);
            }

            if (selectedItem == deleteAllPaths)
            {
                // Iterate through each item in paths and delete it
                for(int i = 0; i < paths.Count; i++)
                {
                    DeletePath(paths[i], i, deleteAllPaths);
                }
                pathsNum.Clear();
                paths.Clear();
                RebuildTrafficMenu();
                Game.LogTrivial($"All paths deleted");
                Game.DisplayNotification($"~o~Scene Manager\n~w~All paths deleted.");
            }

            if (selectedItem == directDriver)
            {
                // Sometimes GetNearbyVehicles will cause a crash for some reason, so keeping it in a try/catch will prevent that.
                try
                {
                    foreach (Vehicle v in Game.LocalPlayer.Character.GetNearbyVehicles(5))
                    {
                        if (v.Exists() && v.IsValid() && v.HasDriver && v.Driver.IsAlive)
                        {
                            // Check if there's a matching vehicle in ControlledVehicles.  If so, check if it has tasks and proceed, else add it to the collection and assign tasks
                            var matchingVehicle = TrafficPathing.ControlledVehicles.Where(cv => cv.Value.Vehicle == v).ToList();
                            if (matchingVehicle.ElementAtOrDefault(0).Value != null && matchingVehicle[0].Value.TasksAssigned)
                            {
                                Game.LogTrivial($"[Direct Driver] {v.Model.Name} already in collection with tasks.  Clearing tasks.");
                                v.Driver.Tasks.Clear();
                                matchingVehicle[0].Value.Path = paths[directDriver.Index].WaypointData[0].Path;
                                matchingVehicle[0].Value.TotalWaypoints = paths[directDriver.Index].WaypointData.Count;
                                matchingVehicle[0].Value.CurrentWaypoint = 1;
                                matchingVehicle[0].Value.DismissNow = true;
                                matchingVehicle[0].Value.StoppedAtWaypoint = false;
                                matchingVehicle[0].Value.Redirected = true;
                                GameFiber DirectTaskFiber = new GameFiber(() => TrafficPathing.DirectTask(matchingVehicle[0].Value, paths[directDriver.Index].WaypointData));
                                DirectTaskFiber.Start();
                            }
                            else if(matchingVehicle.ElementAtOrDefault(0).Value != null && !matchingVehicle[0].Value.TasksAssigned)
                            {
                                Game.LogTrivial($"[Direct Driver] {v.Model.Name} already in collection, but with no tasks.");
                                v.Driver.Tasks.Clear();
                                matchingVehicle[0].Value.Path = paths[directDriver.Index].WaypointData[0].Path;
                                matchingVehicle[0].Value.TotalWaypoints = paths[directDriver.Index].WaypointData.Count;
                                matchingVehicle[0].Value.CurrentWaypoint = 1;
                                matchingVehicle[0].Value.DismissNow = true;
                                matchingVehicle[0].Value.StoppedAtWaypoint = false;
                                matchingVehicle[0].Value.Redirected = true;
                                GameFiber DirectTaskFiber = new GameFiber(() => TrafficPathing.DirectTask(matchingVehicle[0].Value, paths[directDriver.Index].WaypointData));
                                DirectTaskFiber.Start();
                            }
                            else
                            {
                                TrafficPathing.ControlledVehicles.Add(v.LicensePlate, new ControlledVehicle(v, v.LicensePlate, paths[directDriver.Index].WaypointData[0].Path, paths[directDriver.Index].WaypointData.Count, 1, false, false, true));
                                Game.LogTrivial($"[Direct Driver] {v.Model.Name} not in collection, adding to collection for path {paths[directDriver.Index].WaypointData[0].Path} with {paths[directDriver.Index].WaypointData.Count} waypoints");

                                GameFiber DirectTaskFiber = new GameFiber(() => TrafficPathing.DirectTask(TrafficPathing.ControlledVehicles[v.LicensePlate], paths[directDriver.Index].WaypointData));
                                DirectTaskFiber.Start();
                            }
                            Game.LogTrivial($"Directed driver of {v.Model.Name} to path {paths[directDriver.Index].WaypointData[0].Path}.");
                            break;
                        }
                    }
                }
                catch
                {
                    Game.LogTrivial($"No vehicles nearby");
                }

            }

            if (selectedItem == dismissDriver)
            {
                // Check for nearby vehicles, and if the vehicle is being controlled, release it
                GameFiber.StartNew(delegate {
                    foreach (Vehicle v in Game.LocalPlayer.Character.GetNearbyVehicles(5))
                    {
                        try
                        {
                            if (v.Exists() && v.IsValid() && v.HasDriver && v.Driver.IsAlive)
                            {
                                var matchingVehicle = TrafficPathing.ControlledVehicles.Where(cv => cv.Value.Vehicle == v).ToList();
                                if (matchingVehicle.ElementAtOrDefault(0).Value != null && matchingVehicle[0].Value.CurrentWaypoint < matchingVehicle[0].Value.TotalWaypoints && !matchingVehicle[0].Value.StoppedAtWaypoint)
                                {
                                    matchingVehicle[0].Value.DismissNow = true;
                                    v.Driver.Tasks.Clear();
                                    v.Driver.Dismiss();
                                    Game.LogTrivial($"Dismissed driver of {v.Model.Name} from the path");
                                }
                                else if (matchingVehicle.ElementAtOrDefault(0).Value != null && matchingVehicle[0].Value.CurrentWaypoint < matchingVehicle[0].Value.TotalWaypoints)
                                {
                                    matchingVehicle[0].Value.StoppedAtWaypoint = false;
                                    Game.LogTrivial($"Dismissed driver of {v.Model.Name} from waypoint {matchingVehicle[0].Value.CurrentWaypoint}");
                                }
                                else if (matchingVehicle.ElementAtOrDefault(0).Value != null && matchingVehicle[0].Value.CurrentWaypoint == matchingVehicle[0].Value.TotalWaypoints)
                                {
                                    matchingVehicle[0].Value.StoppedAtWaypoint = false;
                                    matchingVehicle[0].Value.DismissNow = true;
                                    v.Driver.Tasks.Clear();
                                    v.Driver.Dismiss();
                                    Game.LogTrivial($"Dismissed driver of {v.Model.Name} from final waypoint and ultimately the path");
                                }
                                else if (matchingVehicle.ElementAtOrDefault(0).Value != null)
                                {
                                    matchingVehicle[0].Value.DismissNow = true;
                                    v.Driver.Tasks.Clear();
                                    v.Driver.Dismiss();
                                    Game.LogTrivial($"Dismissed driver of {v.Model.Name} from path {matchingVehicle[0].Value.Path}");
                                }
                                else
                                {
                                    v.Driver.Tasks.Clear();
                                    v.Driver.Dismiss();
                                    Game.LogTrivial($"Dismissed driver of {v.Model.Name} (was not in collection)");
                                }

                                break;
                            }
                        }
                        catch
                        {
                            Game.LogTrivial($"Something went wrong getting nearby vehicles to dismiss");
                        }
                    }
                });
            }
        }

        private static void PathCreation_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == trafficAddWaypoint)
            {
                // Loop through each path and find the first one which isn't finished
                for (int i = 0; i < paths.Count; i++)
                {
                    if (paths.ElementAtOrDefault(i) != null && !paths.ElementAtOrDefault(i).PathFinished)
                    {
                        // Create a waypoint blip and set the sprite based on the current path number
                        Blip blip = new Blip(Game.LocalPlayer.Character.Position);
                        blip.Scale = 0.5f;
                        switch (i)
                        {
                            case 0:
                                blip.Sprite = BlipSprite.Numbered1;
                                break;
                            case 1:
                                blip.Sprite = BlipSprite.Numbered2;
                                break;
                            case 2:
                                blip.Sprite = BlipSprite.Numbered3;
                                break;
                            case 3:
                                blip.Sprite = BlipSprite.Numbered4;
                                break;
                            case 4:
                                blip.Sprite = BlipSprite.Numbered5;
                                break;
                            case 5:
                                blip.Sprite = BlipSprite.Numbered6;
                                break;
                            case 6:
                                blip.Sprite = BlipSprite.Numbered7;
                                break;
                            case 7:
                                blip.Sprite = BlipSprite.Numbered8;
                                break;
                        }
                        
                        // If it's the first waypoint, make the blip orange, else make it yellow
                        if (paths[i].WaypointData.Count == 0)
                        {
                            blip.Color = Color.Orange;
                        }
                        else
                        {
                            blip.Color = Color.Yellow;
                        }

                        // Add the waypoint data to the path
                        paths[i].WaypointData.Add(new WaypointData(i+1, Game.LocalPlayer.Character.Position, waypointSpeeds[waypointSpeed.Index], drivingFlags[waypointType.Index], blip));
                        Game.LogTrivial($"[Path {i+1}] {drivingFlags[waypointType.Index].ToString()} waypoint added");
                    }
                }

                // Refresh the trafficMenu after a waypoint is added in order to show Continue Creating Current Path instead of Create New Path
                trafficRemoveWaypoint.Enabled = true;
                trafficMenu.Clear();
                trafficMenu.AddItem(createNewPath = new UIMenuItem("Continue Creating Current Path"));
                trafficMenu.AddItem(deleteSinglePath = new UIMenuListItem("Delete Path", pathsNum, 0));
                trafficMenu.AddItem(deleteAllPaths = new UIMenuItem("Delete All Paths"));
                trafficMenu.AddItem(directDriver = new UIMenuListItem("Direct nearest driver to path", pathsNum, 0));
                trafficMenu.AddItem(dismissDriver = new UIMenuItem("Dismiss nearest driver"));
                if (pathsNum.Count == 8)
                {
                    createNewPath.Enabled = false;
                }
                if (pathsNum.Count == 0)
                {
                    deleteSinglePath.Enabled = false;
                    deleteAllPaths.Enabled = false;
                    directDriver.Enabled = false;
                }
                //_menuPool.RefreshIndex(); // Disabling this to stop resetting waypoint menu after waypoint is added
            }

            if (selectedItem == trafficRemoveWaypoint)
            {
                // Loop through each path and find the first one which isn't finished, then delete the path's last waypoint and corresponding blip
                for (int i = 0; i < paths.Count; i++)
                {
                    if (paths.ElementAtOrDefault(i) != null && !paths[i].PathFinished)
                    {
                        Game.LogTrivial($"[Path {i+1}] {paths[i].WaypointData.Last().DrivingFlag.ToString()} waypoint removed");
                        paths[i].WaypointData.Last().WaypointBlip.Delete();
                        paths[i].WaypointData.RemoveAt(paths[i].WaypointData.IndexOf(paths[i].WaypointData.Last()));

                        // If the path has no waypoints, disable the menu option to remove a waypoint
                        if (paths[i].WaypointData.Count == 0)
                        {
                            trafficRemoveWaypoint.Enabled = false;
                        }
                    }
                }
            }

            if (selectedItem == trafficEndPath)
            {
                // Loop through each path and find the first one which isn't finished
                for (int i = 0; i < paths.Count; i++)
                {
                    if (paths.ElementAtOrDefault(i) != null && !paths[i].PathFinished)
                    {
                        // If the path has one stop waypoint or at least two waypoints, finish the path and start the vehicle collector loop, else show user the error and delete any waypoints they made and clear the invalid path
                        if (paths[i].WaypointData.Count >= 2 || (paths[i].WaypointData.Count == 1 && paths[i].WaypointData[0].DrivingFlag == VehicleDrivingFlags.StopAtDestination))
                        {
                            Game.LogTrivial($"[Path Creation] Path {i+1} finished with {paths[i].WaypointData.Count} waypoints.");
                            Game.DisplayNotification($"~o~Scene Manager\n~g~[Success]~w~ Path {i+1} complete.");
                            paths[i].WaypointData.Last().WaypointBlip.Color = Color.OrangeRed;
                            paths[i].PathFinished = true;
                            paths[i].PathNum = i + 1;
                            pathsNum.Insert(i, paths[i].PathNum);

                            GameFiber InitialWaypointVehicleCollectorFiber = new GameFiber(() => TrafficPathing.InitialWaypointVehicleCollector(paths[i].WaypointData));
                            InitialWaypointVehicleCollectorFiber.Start();
                            break;
                        }
                        else
                        {
                            Game.LogTrivial($"[Path Error] A minimum of 2 waypoints is required.");
                            Game.DisplayNotification($"~o~Scene Manager\n~r~[Error]~w~ A minimum of 2 waypoints or one stop waypoint is required to create a path.");
                            foreach (WaypointData wd in paths[i].WaypointData)
                            {
                                wd.WaypointBlip.Delete();
                            }
                            paths[i].WaypointData.Clear();
                            paths.RemoveAt(i);
                            break;
                        }
                    }
                }

                // "Refresh" the menu to reflect the new path
                RebuildTrafficMenu();
            }
        }

        private static void ConeMenu_OnListChange(UIMenu sender, UIMenuListItem listItem, int index)
        {
            if (shadowCone.Exists())
            {
                shadowCone.IsVisible = false;
            }

            shadowCone = new Rage.Object(coneObjectNames[selectCone.Index], Game.LocalPlayer.Character.GetOffsetPosition(new Vector3(0f, 3f, -1f)));
            shadowCone.Opacity = 70f;
            shadowCone.IsCollisionEnabled = false;
        }

        private static void MainMenu_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == navigateToConeMenu)
            {
                if (EntryPoint.Settings.EnableHints)
                {
                    Game.DisplayNotification($"~o~Scene Manager\n~y~[Hint]~y~ ~w~It's easier to place cones in first-person view.");
                }

                shadowCone = new Rage.Object(coneObjectNames[selectCone.Index], Game.LocalPlayer.Character.GetOffsetPosition(new Vector3(0f, 3f, -1f)));
                shadowCone.IsCollisionEnabled = false;
                shadowCone.Opacity = 70f;

                GameFiber ConeHoverFiber = new GameFiber(() => ConeHover(sender, selectedItem, index));
                ConeHoverFiber.Start();
            }
        }

        private static void ConeHover(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            // Run a loop to show currently selected cone hovering in front of player
            //Game.LogTrivial("Creating shadow cone");

            while (coneMenu.Visible)
            {
                shadowCone.Position = Game.LocalPlayer.Character.GetOffsetPosition(new Vector3(0f, 3f, -1f));
                GameFiber.Yield();
            }
        }

        private static void ConeMenu_OnItemSelected(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            Rage.Object cone;
            if (selectedItem == addCone)
            {
                // Attach some invisible object to the cone which the AI try to drive around
                // Barrier, new rotate option in menu, barrier rotates with cone and becomes invisible similar to ASC when created
                cone = new Rage.Object(shadowCone.Model, shadowCone.Position);
                cones.Add(cone);
            }

            if (selectedItem == removeLastCone)
            {
                //Game.LogTrivial($"cones count before deletion: {cones.Count}");
                cones[cones.Count - 1].Delete();
                cones.RemoveAt(cones.Count - 1);
                //Game.LogTrivial($"cones count after deletion: {cones.Count}");
            }

            if (selectedItem == removeNearestCone)
            {
                cones = cones.OrderBy(o => o.DistanceTo(Game.LocalPlayer.Character)).ToList();
                cones[0].Delete();
                cones.RemoveAt(0);
                //Game.LogTrivial($"cones count: {cones.Count}");
            }

            if (selectedItem == removeAllCones)
            {
                foreach (Rage.Object c in cones)
                {
                    c.Delete();
                }
                if (cones.Count > 0)
                {
                    cones.Clear();
                }
                //Game.LogTrivial($"cones count: {cones.Count}");
            }

            coneMenu.RemoveItemAt(2);
            coneMenu.RemoveItemAt(2);
            coneMenu.RemoveItemAt(2);
            coneMenu.AddItem(removeLastCone = new UIMenuItem("Remove Last Cone"));
            if (cones.Count == 0)
            {
                removeLastCone.Enabled = false;
            }
            coneMenu.AddItem(removeNearestCone = new UIMenuItem("Remove Nearest Cone"));
            if (cones.Count == 0)
            {
                removeNearestCone.Enabled = false;
            }
            coneMenu.AddItem(removeAllCones = new UIMenuItem("Remove All Cones"));
            if (cones.Count == 0)
            {
                removeAllCones.Enabled = false;
            }
        }

        private static void MyTerminationHandler(object sender, EventArgs e)
        {
            // Clean up paths
            for (int i = 0; i < paths.Count; i++)
            {
                DeletePath(paths[i], i, deleteAllPaths);
            }

            // Clean up cones
            foreach (Rage.Object cone in cones)
            {
                if (cone.IsValid() && cone.Exists())
                {
                    cone.Delete();
                }
            }

            // Clear everything
            cones.Clear();
            TrafficPathing.ControlledVehicles.Clear();

            Game.LogTrivial($"Scene Manager has been terminated.");
            Game.DisplayNotification($"~o~Scene Manager\n~r~[Notice]~w~ The plugin has shut down.");
        }

    }
}
