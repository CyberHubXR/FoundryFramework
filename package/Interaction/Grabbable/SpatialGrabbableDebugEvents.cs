using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public class SpatialGrabbableDebugEvents : MonoBehaviour
    {
        public SpatialGrabbable grabbable;

        void OnEnable()
        {
            grabbable.OnBeforeGrabbedEvent.AddListener(OnBeforeGrabbedEvent);
            grabbable.OnGrabEvent.AddListener(OnGrab);
            grabbable.OnBeforeReleasedEvent.AddListener(OnBeforeReleasedEvent);
            grabbable.OnReleaseEvent.AddListener(OnReleaseEvent);
            grabbable.OnAnyHighlightEvent.AddListener(OnHighlightEvent);
            grabbable.OnAnyStopHighlightEvent.AddListener(OnStopHighlightEvent);
            grabbable.OnFirstHighlightEvent.AddListener(OnFistHighlightEvent);
            grabbable.OnFinalStopHighlightEvent.AddListener(OnFinalStopHighlightEvent);
        }

        void OnDisable()
        {
            grabbable.OnBeforeGrabbedEvent.RemoveListener(OnBeforeGrabbedEvent);
            grabbable.OnGrabEvent.RemoveListener(OnGrab);
            grabbable.OnBeforeReleasedEvent.RemoveListener(OnBeforeReleasedEvent);
            grabbable.OnReleaseEvent.RemoveListener(OnReleaseEvent);
            grabbable.OnAnyHighlightEvent.RemoveListener(OnHighlightEvent);
            grabbable.OnAnyStopHighlightEvent.RemoveListener(OnStopHighlightEvent);
            grabbable.OnFirstHighlightEvent.RemoveListener(OnFistHighlightEvent);
            grabbable.OnFinalStopHighlightEvent.RemoveListener(OnFinalStopHighlightEvent);
        }

        void OnBeforeGrabbedEvent(SpatialHand hand, SpatialGrabbable grabbable)
        {
            Debug.Log("GRABBABLE: ON BEFORE GRAB", gameObject);
        }

        void OnGrab(SpatialHand hand, SpatialGrabbable grabbable)
        {
            Debug.Log("GRABBABLE: ON GRAB", gameObject);
        }
        void OnBeforeReleasedEvent(SpatialHand hand, SpatialGrabbable grabbable)
        {
            Debug.Log("GRABBABLE: ON BEFORE RELEASE", gameObject);
        }

        void OnReleaseEvent(SpatialHand hand, SpatialGrabbable grabbable)
        {
            Debug.Log("GRABBABLE: ON RELEASE", gameObject);
        }

        void OnHighlightEvent(SpatialHand hand, SpatialGrabbable grabbable)
        {
            Debug.Log("GRABBABLE: ON HIGHLIGHT", gameObject);
        }

        void OnStopHighlightEvent(SpatialHand hand, SpatialGrabbable grabbable)
        {
            Debug.Log("GRABBABLE: ON STOP HIGHLIGHT", gameObject);
        }

        void OnFistHighlightEvent(SpatialHand hand, SpatialGrabbable grabbable)
        {
            Debug.Log("GRABBABLE: ON FIRST HIGHLIGHT", gameObject);
        }

        void OnFinalStopHighlightEvent(SpatialHand hand, SpatialGrabbable grabbable)
        {
            Debug.Log("GRABBABLE: ON STOP FINAL HIGHLIGHT", gameObject);
        }
    }
}