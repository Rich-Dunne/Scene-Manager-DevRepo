﻿using Rage;
using System.Collections.Generic;

namespace SceneManager
{


    public class Path
    { 
        private int _number { get; set; }
        private bool _isEnabled { get; set; }
        private State _state { get; set; }

        public int Number { get { return _number; } set { _number = value; } }
        public bool IsEnabled { get { return _isEnabled; } set { _isEnabled = value; } }
        public State State { get { return _state; } set { _state = value; } }
        public List<Waypoint> Waypoints = new List<Waypoint>();

        public Path(int pathNum, State pathState)
        {
            _number = pathNum;
            _state = pathState;
        }

        public void SetPathNumber(int pathNum)
        {
            _number = pathNum;
        }

        private void LowerWaypointBlipsOpacity()
        {
            foreach (Waypoint wp in Waypoints)
            {
                wp.Blip.Alpha = 0.5f;
                if (wp.CollectorRadiusBlip)
                {
                    wp.CollectorRadiusBlip.Alpha = 0.25f;
                }
            }
        }

        private void RestoreWaypointBlipsOpacity()
        {
            foreach (Waypoint wp in Waypoints)
            {
                if (wp.Blip)
                {
                    wp.Blip.Alpha = 1.0f;
                    if (wp.CollectorRadiusBlip)
                    {
                        wp.CollectorRadiusBlip.Alpha = 0.5f;
                    }
                }
            }
        }

        public void DisablePath()
        {
            _isEnabled = false;
            foreach(Waypoint wp in Waypoints)
            {
                wp.RemoveSpeedZone();
            }
            if (SettingsMenu.mapBlips.Checked)
            {
                LowerWaypointBlipsOpacity();
            }
        }

        public void EnablePath()
        {
            _isEnabled = true;
            foreach (Waypoint wp in Waypoints)
            {
                if (wp.IsCollector)
                {
                    wp.AddSpeedZone();
                }
            }
            if (SettingsMenu.mapBlips.Checked)
            {
                RestoreWaypointBlipsOpacity();
            }
        }




    }
}
