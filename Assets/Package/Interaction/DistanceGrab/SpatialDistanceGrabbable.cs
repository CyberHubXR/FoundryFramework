using Foundry;
using UnityEngine;
using UnityEngine.Events;

namespace Foundry 
{
    [RequireComponent(typeof(SpatialGrabbable))]
    public class SpatialDistanceGrabbable : MonoBehaviour{
        
        public bool triggerGrabbableHighlight = true;
        public UnityEvent<SpatialDistanceGrabber, SpatialDistanceGrabbable> OnPull;
        public UnityEvent<SpatialDistanceGrabber, SpatialDistanceGrabbable> StartTargeting;
        public UnityEvent<SpatialDistanceGrabber, SpatialDistanceGrabbable> StopTargeting;

        //Currently no functionality supports selectiong but this will allow for future implementation of pulling and time delayed activations
        internal UnityEvent<SpatialDistanceGrabber, SpatialDistanceGrabbable> StartSelecting;
        internal UnityEvent<SpatialDistanceGrabber, SpatialDistanceGrabbable> StopSelecting;

        internal SpatialGrabbable grabbable;

        bool wasHighlightEnabled;
        Transform target = null;

        void OnEnable() {
            grabbable = GetComponent<SpatialGrabbable>();
        }



        public void SetTarget(Transform theObject) { target = theObject;  }
        public void CancelTarget() { target = null;  }
    }
}
