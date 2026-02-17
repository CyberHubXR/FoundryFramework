using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public class SpatialDistanceGrabbableDebugger : MonoBehaviour
    {
        public SpatialDistanceGrabbable grabbable;

        private void OnEnable() {
            grabbable.OnPull.AddListener(OnPull);
            grabbable.StartTargeting.AddListener(StartTargeting);
            grabbable.StopTargeting.AddListener(StopTargeting);
            grabbable.StartSelecting.AddListener(StartSelecting);
            grabbable.StopSelecting.AddListener(StopSelecting);
        }

        void OnPull(SpatialDistanceGrabber grabber, SpatialDistanceGrabbable grabbable) {
            Debug.Log("OnPull: " + grabber.name + " -> " + grabbable.name);
        }

        void StartTargeting(SpatialDistanceGrabber grabber, SpatialDistanceGrabbable grabbable) {
            Debug.Log("StartTargeting: " + grabber.name + " -> " + grabbable.name);
        }

        void StopTargeting(SpatialDistanceGrabber grabber, SpatialDistanceGrabbable grabbable) {
            Debug.Log("StopTargeting: " + grabber.name + " -> " + grabbable.name);
        }

        void StartSelecting(SpatialDistanceGrabber grabber, SpatialDistanceGrabbable grabbable) {
            Debug.Log("StartSelecting: " + grabber.name + " -> " + grabbable.name);
        }

        void StopSelecting(SpatialDistanceGrabber grabber, SpatialDistanceGrabbable grabbable) {
            Debug.Log("StopSelecting: " + grabber.name + " -> " + grabbable.name);
        }
    }
}
