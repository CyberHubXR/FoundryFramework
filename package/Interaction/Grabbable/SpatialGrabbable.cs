using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Foundry
{
    [System.Serializable]
    public enum InteractionType
    {
        parent,
        velocitytracked,
        kinematic
    }

    public class SpatialGrabbable : FoundryScript
    {
        public Rigidbody attachedRigidbody;
        public SpatialHand.HandType handType = SpatialHand.HandType.Both;
        public int maxHeldCount = 1;
        public bool isGrabbable = true;

        //VISIBLE EVENTS?
        [Space]
        public SpatialHandGrabEvent OnReleaseEvent;
        public SpatialHandGrabEvent OnGrabEvent;

        [Space]
        public SpatialHandGrabEvent OnFirstHighlightEvent;
        public SpatialHandGrabEvent OnFinalStopHighlightEvent;

        //Q: HIDDEN EVENTS? -> A: Dont want to fill the component with tons of minor events but these events can be useful in advanced scripts and should be accessed through code
        [HideInInspector]
        public SpatialHandGrabEvent OnBeforeReleasedEvent;
        [HideInInspector]
        public SpatialHandGrabEvent OnBeforeGrabbedEvent;

        [HideInInspector]
        public SpatialHandGrabEvent OnAnyHighlightEvent;
        [HideInInspector]
        public SpatialHandGrabEvent OnAnyStopHighlightEvent;

        [HideInInspector]
        public UnityPlacePointEvent OnPlacePointHighlightEvent;
        [HideInInspector]
        public UnityPlacePointEvent OnPlacePointUnhighlightEvent;
        [HideInInspector]
        public UnityPlacePointEvent OnPlacePointAddEvent;
        [HideInInspector]
        public UnityPlacePointEvent OnPlacePointRemoveEvent;


        public Transform rootTransform { get { return attachedRigidbody.transform; } }

        public PlacePoint placePoint { internal set; get; }
        public List<SpatialGrabbable> grabbableChildren { private set; get; }
        public List<SpatialGrabbable> grabbableParents { private set; get; }
        public List<SpatialHand> heldByHands { private set; get; }


        List<SpatialGrabbableChild> grabChildren = new List<SpatialGrabbableChild>();
        List<SpatialHand> highlightedByHands = new List<SpatialHand>();


        private void Awake() { 
            if (attachedRigidbody == null)
                TryGetComponent(out attachedRigidbody);

            foreach(var collider in GetComponentsInChildren<Collider>()) {
                collider.gameObject.layer = LayerMask.NameToLayer("FoundryGrabbable");
            }

            heldByHands = new List<SpatialHand>();
            grabbableChildren = new List<SpatialGrabbable>(GetComponentsInChildren<SpatialGrabbable>(true));
            if(grabbableChildren.Contains(this))
                grabbableChildren.Remove(this);

            grabbableParents = new List<SpatialGrabbable>(GetComponentsInParent<SpatialGrabbable>(true));
            if(grabbableParents.Contains(this))
                grabbableParents.Remove(this);
        }

        private void OnDestroy() {

            if(placePoint != null && !placePoint.disablePlacePointOnPlace)
                placePoint.Remove(this);
        }


        internal virtual void OnBeforeGrab(SpatialHand hand)
        {
            if (hand.enableEvents && OnBeforeGrabbedEvent != null)
                OnBeforeGrabbedEvent.Invoke(hand, this);

        }


        internal virtual void OnGrab(SpatialHand hand)
        {
            if (hand.enableEvents && OnGrabEvent != null)
                OnGrabEvent.Invoke(hand, this);

            placePoint?.Remove(this);
            heldByHands.Add(hand);
        }



        internal virtual void OnBeforeRelease(SpatialHand hand)
        {
            if (hand.enableEvents && OnBeforeReleasedEvent != null)
                OnBeforeReleasedEvent.Invoke(hand, this);
        }


        internal virtual void OnRelease(SpatialHand hand)
        {
            bool canPlace = placePoint != null && placePoint.CanPlace(this);

            if (hand.enableEvents && OnReleaseEvent != null)
                OnReleaseEvent.Invoke(hand, this);

            if(placePoint != null && canPlace)
                placePoint.Place(this);

            heldByHands.Remove(hand);
        }


        internal virtual void OnHighlight(SpatialHand hand)
        {
            if (hand.enableEvents)
            {
                if (highlightedByHands.Count == 0)
                    OnFirstHighlightEvent?.Invoke(hand, this);

                OnAnyHighlightEvent?.Invoke(hand, this);
            }

            highlightedByHands.Add(hand);

        }


        internal virtual void StopHighlight(SpatialHand hand)
        {
            if (hand.enableEvents)
            {
                OnAnyStopHighlightEvent?.Invoke(hand, this);

                highlightedByHands.Remove(hand);
            }

            if (highlightedByHands.Count == 0)
                OnFinalStopHighlightEvent?.Invoke(hand, this);
        }




        #region PUBLIC GETTERS

        public bool IsGrabbed => heldByHands.Count > 0;

        public bool CanGrab(SpatialHand spatialHand)
        {
            bool properHandType = handType == SpatialHand.HandType.Both || spatialHand.handType == SpatialHand.HandType.Both || spatialHand.handType == handType;
            bool maxHoldReached = heldByHands.Count >= maxHeldCount;
            return isGrabbable && properHandType && !maxHoldReached;
        }

        public int HeldCount()
        {
            return heldByHands.Count;
        }

        public List<SpatialHand> GetHoldingHands()
        {
            return heldByHands;
        }

        public List<SpatialHand> GetHighlightingHands()
        {
            return highlightedByHands;
        }

        #endregion




        #region PRIVATE SETTERS

        internal void SetGrabbableChild(SpatialGrabbableChild child) {
            if(!grabChildren.Contains(child)) {
                grabChildren.Add(child);
                child.grabParent = this;
            }
        }

        //Adds a reference script to child colliders so they can be grabbed
        void MakeChildrenGrabbable() {
            for(int i = 0; i < transform.childCount; i++) {
                AddChildGrabbableRecursive(transform.GetChild(i));
            }

            void AddChildGrabbableRecursive(Transform obj) {
                if(obj.CanGetComponent(out Collider col) && col.isTrigger == false && !obj.CanGetComponent<SpatialGrabbable>(out _) && !obj.CanGetComponent<SpatialGrabbableChild>(out _) && !obj.CanGetComponent<PlacePoint>(out _)) {
                    var child = obj.gameObject.AddComponent<SpatialGrabbableChild>();
                    SetGrabbableChild(child);
                }
                for(int i = 0; i < obj.childCount; i++) {
                    if(!obj.CanGetComponent<SpatialGrabbable>(out _))
                        AddChildGrabbableRecursive(obj.GetChild(i));
                }
            }
        }


        //Adds a reference script to child colliders so they can be grabbed
        void MakeChildrenUngrabbable() {
            for(int i = 0; i < transform.childCount; i++) {
                RemoveChildGrabbableRecursive(transform.GetChild(i));
            }

            void RemoveChildGrabbableRecursive(Transform obj) {
                if(obj.GetComponent<SpatialGrabbableChild>() && obj.GetComponent<SpatialGrabbableChild>().grabParent == this) {
                    Destroy(obj.gameObject.GetComponent<SpatialGrabbableChild>());
                }
                for(int i = 0; i < obj.childCount; i++) {
                    RemoveChildGrabbableRecursive(obj.GetChild(i));
                }
            }
        }

        public void ForceHandsRelease() {
            for(int i = heldByHands.Count - 1; i >= 0; i--) {
                heldByHands[i].Release();
            }
        }
        #endregion
    }
}