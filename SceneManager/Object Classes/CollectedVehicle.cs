using Rage;
using System.Linq;

namespace SceneManager
{
    public class CollectedVehicle
    {
        private Ped _driver { get; set; }
        private Vehicle _vehicle { get; set; }
        private Path _path { get; set; }
        private Waypoint _currentWaypoint {get; set;}
        private bool _tasksAssigned { get; set; }
        private bool _stoppedAtWaypoint { get; set; }
        private bool _dismissed { get; set; } = false;
        private bool _skipWaypoint { get; set; } = false;

        public Ped Driver { get { return _driver; } set { _driver = value; } }
        public Vehicle Vehicle { get { return _vehicle; } set { _vehicle = value; } }
        public Path Path { get { return _path; } set { _path = value; } }
        public Waypoint CurrentWaypoint { get { return _currentWaypoint; } set { _currentWaypoint = value; } }
        public bool TasksAssigned { get { return _tasksAssigned; } set { _tasksAssigned = value; } }
        public bool StoppedAtWaypoint { get { return _stoppedAtWaypoint; } set { _stoppedAtWaypoint = value; } }
        public bool Dismissed { get { return _dismissed; } set { _dismissed = value; } }
        public bool SkipWaypoint { get { return _skipWaypoint; } set { _skipWaypoint = value; } }

        public CollectedVehicle(Vehicle vehicle, Path path, Waypoint currentWaypoint, bool tasksAssigned)
        {
            _vehicle = vehicle;
            _driver = vehicle.Driver;
            _path = path;
            _currentWaypoint = currentWaypoint;
            _tasksAssigned = tasksAssigned;
        }

        public CollectedVehicle(Vehicle vehicle, Path path, bool tasksAssigned)
        {
            _vehicle = vehicle;
            _driver = vehicle.Driver;
            _path = path;
            _tasksAssigned = tasksAssigned;
        }

        public void AssignPropertiesFromDirectedTask(Path path, Waypoint currentWaypoint, bool tasksAssigned, bool stoppedAtWaypoint)
        {
            _path = path;
            _currentWaypoint = currentWaypoint;
            _tasksAssigned = tasksAssigned;
            _stoppedAtWaypoint = stoppedAtWaypoint;
        }

        public void Dismiss()
        {
            GameFiber.StartNew(() =>
            {
                if (!_vehicle || !_driver)
                {
                    return;
                }
                _dismissed = true;
                _stoppedAtWaypoint = false;

                _driver.Tasks.Clear();
                _driver.Tasks.PerformDrivingManeuver(_vehicle, VehicleManeuver.GoForwardWithCustomSteeringAngle, 3);

                if (_driver.GetAttachedBlip())
                {
                    _driver.GetAttachedBlip().Delete();
                }

                // check if the vehicle is near any of the path's collector waypoints
                var nearestCollectorWaypoint = _path.Waypoints.Where(wp => wp.IsCollector && _vehicle.DistanceTo2D(wp.Position) <= wp.CollectorRadius * 2).FirstOrDefault();
                if (nearestCollectorWaypoint != null)
                {
                    while (nearestCollectorWaypoint != null && _vehicle && _driver && _vehicle.DistanceTo2D(nearestCollectorWaypoint.Position) <= nearestCollectorWaypoint.CollectorRadius * 2)
                    {
                        //Game.LogTrivial($"{_vehicle.Model.Name} is too close to the collector to be fully dismissed.");
                        GameFiber.Yield();
                    }
                }

                if (!_vehicle || !_driver)
                {
                    return;
                }

                VehicleCollector.collectedVehicles.Remove(this);
                Game.LogTrivial($"{_vehicle.Model.Name} dismissed successfully.");
                _driver.BlockPermanentEvents = false;
                _driver.Dismiss();
                _vehicle.IsSirenOn = false;
                _vehicle.IsSirenSilent = true;
                _vehicle.Dismiss();
            });
        }
    }
}
