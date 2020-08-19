using Rage;

namespace SceneManager
{
    public class ControlledVehicle
    {
        public Vehicle Vehicle;
        public string LicensePlate;
        public int Path;
        public int TotalWaypoints;
        public int CurrentWaypoint;
        public bool TasksAssigned;
        public bool DismissNow;
        public bool StoppedAtWaypoint;
        public bool Redirected;

        public ControlledVehicle(Vehicle vehicle, string licensePlate, int path, int totalWaypoints, int currentWaypoint, bool tasksAssigned, bool dismissNow, bool redirected)
        {
            Vehicle = vehicle;
            LicensePlate = licensePlate;
            Path = path;
            TotalWaypoints = totalWaypoints;
            CurrentWaypoint = currentWaypoint;
            TasksAssigned = tasksAssigned;
            DismissNow = dismissNow;
            Redirected = redirected;
        }
    }
}
