using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public class SpatialHandDebugEvents : MonoBehaviour
    {
        public SpatialHand hand;

        void OnEnable()
        {
            hand.OnBeforeGrabbedEvent.AddListener(OnBeforeGrabbedEvent);
            hand.OnGrabEvent.AddListener(OnGrab);
            hand.OnBeforeReleasedEvent.AddListener(OnBeforeReleasedEvent);
            hand.OnReleaseEvent.AddListener(OnReleaseEvent);
            hand.OnHighlightEvent.AddListener(OnHighlightEvent);
            hand.OnStopHighlightEvent.AddListener(OnStopHighlightEvent);
        }

        void OnDisable()
        {
            hand.OnBeforeGrabbedEvent.RemoveListener(OnBeforeGrabbedEvent);
            hand.OnGrabEvent.RemoveListener(OnGrab);
            hand.OnBeforeReleasedEvent.RemoveListener(OnBeforeReleasedEvent);
            hand.OnReleaseEvent.RemoveListener(OnReleaseEvent);
            hand.OnHighlightEvent.RemoveListener(OnHighlightEvent);
            hand.OnStopHighlightEvent.RemoveListener(OnStopHighlightEvent);
        }

        void OnBeforeGrabbedEvent(SpatialHand hand, SpatialGrabbable grabbable)
        {
            Debug.Log("HAND: ON BEFORE GRAB", gameObject);
        }

        void OnGrab(SpatialHand hand, SpatialGrabbable grabbable)
        {
            Debug.Log("HAND: ON GRAB", gameObject);
        }
        void OnBeforeReleasedEvent(SpatialHand hand, SpatialGrabbable grabbable)
        {
            Debug.Log("HAND: ON BEFORE RELEASE", gameObject);
        }

        void OnReleaseEvent(SpatialHand hand, SpatialGrabbable grabbable)
        {
            Debug.Log("GRABBABLE: ON RELEASE", gameObject);
        }
        void OnHighlightEvent(SpatialHand hand, SpatialGrabbable grabbable)
        {
            Debug.Log("HAND: ON HIGHLIGHT", gameObject);
        }

        void OnStopHighlightEvent(SpatialHand hand, SpatialGrabbable grabbable)
        {
            Debug.Log("HAND: ON STOP HIGHLIGHT", gameObject);
        }
    }
}