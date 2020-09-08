using Rage;

namespace SceneManager
{
    public class CollectedVehicle
    {
        private Ped _driver { get; set; }
        private Vehicle _vehicle { get; set; }
        private Path _path { get; set; }
        //private int _path { get; set; } // Should change this to a Path object
        private int _currentWaypoint { get; set; }
        private bool _tasksAssigned { get; set; }
        private bool _stoppedAtWaypoint { get; set; }

        public Ped Driver { get { return _driver; } set { _driver = value; } }
        public Vehicle Vehicle { get { return _vehicle; } set { _vehicle = value; } }
        public Path Path { get { return _path; } set { _path = value; } }
        public int CurrentWaypoint { get { return _currentWaypoint; } set { _currentWaypoint = value; } }
        public bool TasksAssigned { get { return _tasksAssigned; } set { _tasksAssigned = value; } }
        public bool StoppedAtWaypoint { get { return _stoppedAtWaypoint; } set { _stoppedAtWaypoint = value; } }

        public CollectedVehicle(Vehicle vehicle, Path path, int totalWaypoints, int currentWaypoint, bool tasksAssigned)
        {
            Vehicle = vehicle;
            Driver = vehicle.Driver;
            Path = path;
            CurrentWaypoint = currentWaypoint;
            TasksAssigned = tasksAssigned;
        }

        public void AssignPropertiesFromDirectedTask(Path path, int totalPathWaypoints, int currentWaypoint, bool tasksAssigned, bool stoppedAtWaypoint)
        {
            Path = path;
            CurrentWaypoint = currentWaypoint;
            TasksAssigned = tasksAssigned;
            StoppedAtWaypoint = stoppedAtWaypoint;
        }
    }
}
