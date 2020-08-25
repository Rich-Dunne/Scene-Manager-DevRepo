using Rage;

namespace SceneManager
{
    public class CollectedVehicle
    {
        public Vehicle Vehicle { get; private set; }
        public string LicensePlate { get; private set; }
        public int Path { get; private set; }
        public int TotalWaypoints { get; private set; }
        public int CurrentWaypoint { get; private set; }
        public bool TasksAssigned { get; private set; }
        public bool DismissNow { get; private set; }
        public bool StoppedAtWaypoint { get; private set; }
        public bool Redirected { get; private set; }

        public CollectedVehicle(Vehicle vehicle, string licensePlate, int path, int totalWaypoints, int currentWaypoint, bool tasksAssigned, bool dismissNow, bool redirected)
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

        public void AssignPropertiesFromDirectedTask(int pathNum, int totalPathWaypoints, int currentWaypoint, bool tasksAssigned, bool dismiss, bool stoppedAtWaypoint)//, bool redirected)
        {
            Path = pathNum;
            TotalWaypoints = totalPathWaypoints;
            CurrentWaypoint = currentWaypoint;
            TasksAssigned = tasksAssigned;
            DismissNow = dismiss;
            StoppedAtWaypoint = stoppedAtWaypoint;
            //Redirected = redirected;
        }

        public void SetCurrentWaypoint(int currentWaypoint)
        {
            CurrentWaypoint = currentWaypoint;
        }

        public void SetTasksAssigned(bool tasksAssigned)
        {
            TasksAssigned = tasksAssigned;
        }

        public void SetDismissNow(bool dismissNow)
        {
            DismissNow = dismissNow;
        }

        public void SetStoppedAtWaypoint(bool stoppedAtWaypoint)
        {
            StoppedAtWaypoint = stoppedAtWaypoint;
        }

        public void SetRedirected(bool redirected)
        {
            Redirected = redirected;
        }
    }
}
