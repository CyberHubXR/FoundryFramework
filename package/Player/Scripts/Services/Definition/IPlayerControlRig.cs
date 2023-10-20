using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    /// <summary>
    /// Interface for for implementing player control rig, this is how we handle different control schemes such as desktop and VR.
    /// </summary>
    public interface IPlayerControlRig
    {
        // Request movement and speed from a rig
        Transform transform { get; }
        bool enabled { get; set; }

        public Transform TrackerTransform(TrackerType type);

        // Request tracking mode
        public TrackingMode GetTrackingMode();

        /// <summary>
        /// Request the current positions of all trackers
        /// </summary>
        /// <param name="trackers">Array with a length of 6 representing some trackers to be updated</param>
        public void UpdateTrackers(TrackerPos[] trackers);
    }
}

