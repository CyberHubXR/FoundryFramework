using System;
using System.Collections;
using System.Collections.Generic;
using Foundry.Networking;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Foundry {
    public enum PlacePointNameType
    {
        name,
        tag
    }

    [Serializable]
    public class UnityPlacePointEvent : UnityEvent<PlacePoint, SpatialGrabbable> { }
    //You can override this by turning the radius to zero, and using any other trigger collider
    public class PlacePoint : NetworkComponent {

        [Header("Place Settings")]
        public bool showPlaceSettings = true;
        [Tooltip("Snaps an object to the point at start, leave empty for no target")]
        public SpatialGrabbable startPlaced;
        [Tooltip("This will make the point place the object as soon as it enters the radius, instead of on release")]
        public Transform placedOffset;
        [Tooltip("The radius of the place point (relative to scale)")]
        public float placeRadius = 0.1f;
        [Tooltip("The local offset of the enter radius of the place point (not the offset of the placement)")]
        public Vector3 radiusOffset;

        [Space]
        [Tooltip("This will make the point place the object as soon as it enters the radius, instead of on release")]
        public bool parentOnPlace = true;
        [Tooltip("This will make the point place the object as soon as it enters the radius, instead of on release")]
        public bool forcePlace = false;
        [Space]
        [Tooltip("Whether or not the placed object should be disabled on placement (this will hide the placed object and leave the place point active for a new object)")]
        public bool destroyObjectOnPlace = false;
        [Tooltip("Whether or not the SpatialGrabbable should be disabled on place")]
        public bool disableGrabOnPlace = false;
        [Tooltip("Whether or not this place point should be disabled on placement. It will maintain its connection and can no longer accept new items. Causes less overhead if true")]
        public bool disablePlacePointOnPlace = false;
        [Space]

        [Tooltip("If true and will force release on place")]
        public bool makePlacedKinematic = true;

        [Tooltip("The rigidbody to attach the placed grabbable to - leave empty means no joint")]
        public Rigidbody placedJointLink;
        public float jointBreakForce = 1000;

        [Header("Place Requirements")]
        [Tooltip("Whether or not to only allow placement of an object while it's being held (or released)")]
        public bool heldPlaceOnly = false;

        [Tooltip("Whether the placeNames should compare names or tags")]
        public PlacePointNameType nameCompareType;
        [Tooltip("Will allow placement for any SpatialGrabbable with a name containing this array of strings, leave blank for any SpatialGrabbable allowed")]
        public string[] placeNames;
        [Tooltip("Will prevent placement for any name containing this array of strings")]
        public string[] blacklistNames;

        [Tooltip("(Unless empty) Will only allow placement any object contained here")]
        public List<SpatialGrabbable> onlyAllows;
        [Tooltip("Will NOT allow placement any object contained here")]
        public List<SpatialGrabbable> dontAllows;

        [Tooltip("The layer that this place point will check for placeable objects, if none will default to SpatialGrabbable")]
        public LayerMask placeLayers;

        [Space]
        public UnityPlacePointEvent OnPlace;
        public UnityPlacePointEvent OnRemove;
        public UnityPlacePointEvent OnHighlight;
        public UnityPlacePointEvent OnStopHighlight;
        
        [Header("Networked Events")]
        public NetworkEvent<NetworkId> OnPlaceNetworked = new();
        public NetworkEvent<NetworkId> OnRemoveNetworked = new();
        

        public SpatialGrabbable highlightingObj { get; protected set; } = null;
        public SpatialGrabbable placedObject { get; protected set; } = null;
        public SpatialGrabbable lastPlacedObject { get; protected set; } = null;

        private NetworkProperty<NetworkId> networkedPlacedObject = new(NetworkId.Invalid);

        //How far the placed object has to be moved to count to auto remove from point so something else can take its place
        protected float removalDistance = 0.05f;
        protected float lastPlacedTime;
        protected Vector3 placePosition;
        protected Transform originParent;
        protected bool placingFrame;
        protected CollisionDetectionMode placedObjDetectionMode;
        float tickRate = 0.02f;
        Collider[] collidersNonAlloc = new Collider[30];

        
        public override void RegisterProperties(List<INetworkProperty> props, List<INetworkEvent> events) {
            props.Add(networkedPlacedObject);
            events.Add(OnPlaceNetworked);
            events.Add(OnRemoveNetworked);
        }
        
        public override void OnConnected()
        {
            OnPlaceNetworked.AddListener((s,id)=>
            {
                if(s == NetEventSource.Remote)
                    PlaceFromNetwork(id);
            });
            
            if (!networkedPlacedObject.Value.IsValid())
                return;
            PlaceFromNetwork(networkedPlacedObject.Value);
        }
        
        protected void PlaceFromNetwork(NetworkId id)
        {
            var o = NetworkManager.GetObjectById(id);
            if (!o)
            {
                Remove();
                return;
            }
            
            if(o.gameObject == placedObject?.gameObject)
                return;
            if (o.TryGetComponent(out SpatialGrabbable grabbable))
                Place(grabbable);
        }
        
        protected virtual void Start(){
            if (placedOffset == null)
                placedOffset = transform;

            if(placeLayers == 0)
                placeLayers = LayerMask.GetMask(SpatialHand.SpatialGrabbableLayerNameDefault);

            SetStartPlaced();
        }

        Coroutine checkRoutine;
        protected virtual void OnEnable() {
            if (placedOffset == null)
                placedOffset = transform;

            checkRoutine = StartCoroutine(CheckPlaceObjectLoop());
        }

        protected virtual void OnDisable() {
            Remove();
            StopCoroutine(checkRoutine);   
        }


        int lastOverlapCount = 0;
        SpatialGrabbable tempGrabbable;
        protected virtual IEnumerator CheckPlaceObjectLoop() {
            var scale = Mathf.Abs(transform.lossyScale.x < transform.lossyScale.y ? transform.lossyScale.x : transform.lossyScale.y);
            scale = Mathf.Abs(scale < transform.lossyScale.z ? scale : transform.lossyScale.z);

            CheckPlaceObject(placeRadius, scale);

            yield return new WaitForSeconds(UnityEngine.Random.Range(0f, tickRate));
            while(gameObject.activeInHierarchy) {
                    CheckPlaceObject(placeRadius, scale);

                yield return new WaitForSeconds(tickRate);
            }
        }

        void CheckPlaceObject(float radius, float scale) {
            if(!disablePlacePointOnPlace && placedObject != null && !IsStillOverlapping(placedObject, scale))
                Remove(placedObject);

            if(placedObject == null && highlightingObj == null) {
                var overlapCenterPos = placedOffset.position + transform.rotation * radiusOffset;
                var overlaps = Physics.OverlapSphereNonAlloc(overlapCenterPos, radius * scale, collidersNonAlloc, placeLayers);
                if(overlaps != lastOverlapCount) {
                    var updateOverlaps = true;
                    for(int i = 0; i < overlaps; i++) {
                        if(collidersNonAlloc[i].gameObject.HasGrabbable(out tempGrabbable)) {
                            // To continuously check distance & check tempSpatialGrabbable state 
                            updateOverlaps = false;

                            if(CanPlace(tempGrabbable)) {
                                var existingPlacePoint = tempGrabbable.placePoint;
                                if(existingPlacePoint) {
                                    // Get positions
                                    var grabbablePos = tempGrabbable.transform.position;
                                    var concurrentCenterPos = existingPlacePoint.placedOffset.position +
                                                                existingPlacePoint.transform.rotation *
                                                                existingPlacePoint.radiusOffset;

                                    // Calculate distance
                                    var concurrentDist = Vector3.Distance(concurrentCenterPos,
                                        grabbablePos);
                                    var currentDist = Vector3.Distance(overlapCenterPos, grabbablePos);

                                    // If current further => continue
                                    if(currentDist >= concurrentDist) {
                                        continue;
                                    }

                                    // Otherwise => replace
                                    existingPlacePoint.StopHighlight(tempGrabbable);
                                }

                                Highlight(tempGrabbable);
                                break;
                            }
                        }
                    }

                    if(updateOverlaps) {
                        lastOverlapCount = overlaps;
                    }
                }
            }
            else if(highlightingObj != null) {
                if(!IsStillOverlapping(highlightingObj, scale)) {
                    StopHighlight(highlightingObj);
                }
            }
        }

        public virtual bool CanPlace(SpatialGrabbable placeObj) {


            if(placedObject != null) {
                return false;
            }

            //if(placeObj.placePoint != null && placeObj.placePoint != this) {
            //    return false;
            //}

            if(heldPlaceOnly && placeObj.HeldCount() == 0) {
                return false;
            }

            if(onlyAllows.Count > 0 && !onlyAllows.Contains(placeObj)) {
                return false;
            }

            if(dontAllows.Count > 0 && dontAllows.Contains(placeObj)) {
                return false;
            }

            if(placeNames.Length == 0 && blacklistNames.Length == 0) {
                return true;
            }

            if (blacklistNames.Length > 0)
                foreach(var badName in blacklistNames)
                {
                    if (nameCompareType == PlacePointNameType.name && placeObj.name.Contains(badName))
                        return false;
                    if (nameCompareType == PlacePointNameType.tag && placeObj.CompareTag(badName))
                        return false;
                }

            if (placeNames.Length > 0)
                foreach (var placeName in placeNames)
                {
                    if (nameCompareType == PlacePointNameType.name && placeObj.name.Contains(placeName))
                        return true;
                    if (nameCompareType == PlacePointNameType.tag && placeObj.CompareTag(placeName))
                        return true;
                }
            else
                return true;

            return false;
        }

        public virtual void TryPlace(SpatialGrabbable placeObj) {
            if(CanPlace(placeObj))
                Place(placeObj);
        }

        public virtual void Place(SpatialGrabbable placeObj) {
            if (placedObject != null)
                return;

            if(placeObj.placePoint != null && placeObj.placePoint != this)
                placeObj.placePoint.Remove(placeObj);

            placedObject = placeObj;
            placedObject.placePoint = (this); 
            
            // Network the placed object
            if (placeObj.TryGetComponent(out NetworkObject netObject) && netObject.IsOwner)
            {
                // Make sure we own this object so we can change values
                Object?.RequestOwnership();
            
                var id = netObject.Id;
                networkedPlacedObject.Value = id;
                OnPlaceNetworked.Invoke(netObject.Id);
            }
            
            if (placeObj.TryGetComponent(out NetworkTransform networkTransform) && networkTransform.IsOwner)
            {
                // Make sure we own this object so we can change values
                Object?.RequestOwnership();
            
                networkTransform.Teleport(placedOffset.position, placedOffset.rotation);
            }
            

            if(placeObj.HeldCount() > 0) {
                placeObj.ForceHandsRelease();
                foreach(var grab in placeObj.grabbableParents)
                    grab.ForceHandsRelease();
                foreach(var grab in placeObj.grabbableChildren)
                    grab.ForceHandsRelease();
            }

            placingFrame = true;
            originParent = placeObj.transform.parent;

            placeObj.rootTransform.position = placedOffset.position;
            placeObj.rootTransform.rotation = placedOffset.rotation;

            if (placeObj.attachedRigidbody != null)
            {
                placeObj.attachedRigidbody.linearVelocity = Vector3.zero;
                placeObj.attachedRigidbody.angularVelocity = Vector3.zero;
                placedObjDetectionMode = placeObj.attachedRigidbody.collisionDetectionMode;

                if (makePlacedKinematic)
                {
                    placeObj.attachedRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    placeObj.attachedRigidbody.isKinematic = true;
                }

            }

            foreach(var grab in placeObj.grabbableChildren) 
                grab.OnGrabEvent.AddListener(OnRelativeGrabbed);
            foreach(var grab in placeObj.grabbableParents) 
                grab.OnGrabEvent.AddListener(OnRelativeGrabbed);

            StopHighlight(placeObj);
            
            placePosition = placedObject.rootTransform.position;

            placeObj.OnPlacePointAddEvent?.Invoke(this, placeObj);
            foreach(var grabChild in placeObj.grabbableChildren)
                grabChild.OnPlacePointAddEvent?.Invoke(this, grabChild);
            foreach(var garb in placedObject.grabbableParents)
                garb.OnPlacePointAddEvent?.Invoke(this, garb);

            OnPlace?.Invoke(this, placeObj);
            lastPlacedTime = Time.time;

            if (parentOnPlace)
                placedObject.rootTransform.parent = transform;

            if (disablePlacePointOnPlace)
                enabled = false;

            if (disableGrabOnPlace || disablePlacePointOnPlace)
                placeObj.isGrabbable = false;

            if (destroyObjectOnPlace)
                Destroy(placedObject.gameObject);
        }

        public virtual void Remove(SpatialGrabbable placeObj) {
            if (placeObj == null || placeObj != placedObject || disablePlacePointOnPlace)
                return;

            if (placeObj.attachedRigidbody != null){
                placeObj.attachedRigidbody.collisionDetectionMode = placedObjDetectionMode;
            }

            foreach(var grab in placeObj.grabbableChildren)
                grab.OnGrabEvent.RemoveListener(OnRelativeGrabbed);
            foreach(var grab in placeObj.grabbableParents) 
                grab.OnGrabEvent.RemoveListener(OnRelativeGrabbed);

            placedObject.OnPlacePointRemoveEvent?.Invoke(this, highlightingObj);
            foreach(var grabChild in placedObject.grabbableChildren)
                grabChild.OnPlacePointRemoveEvent?.Invoke(this, grabChild);
            foreach(var garb in placedObject.grabbableParents)
                garb.OnPlacePointRemoveEvent?.Invoke(this, garb);
            if (networkedPlacedObject.Value.IsValid())
            {
                OnRemoveNetworked.Invoke(networkedPlacedObject.Value);
                networkedPlacedObject.Value = NetworkId.Invalid;
            }
                

            OnRemove?.Invoke(this, placeObj);

            Highlight(placeObj);

            
            if (/*!placeObj.parentOnGrab &&*/ parentOnPlace && gameObject.activeInHierarchy)
                placeObj.transform.parent = originParent;

            lastPlacedObject = placedObject;
            placedObject = null;
        }

        public void Remove() {
            if(placedObject != null)
                Remove(placedObject);
        }

        internal virtual void Highlight(SpatialGrabbable from) {
            if(highlightingObj == null){
                from.placePoint = (this);
                foreach(var garb in from.grabbableParents)
                    garb.placePoint = (this);
                foreach(var garb in from.grabbableChildren)
                    garb.placePoint = (this);

                highlightingObj = from;
                highlightingObj.OnPlacePointHighlightEvent?.Invoke(this, highlightingObj);
                foreach(var grabChild in highlightingObj.grabbableChildren)
                    grabChild.OnPlacePointHighlightEvent?.Invoke(this, grabChild);
                foreach(var garb in highlightingObj.grabbableParents)
                    garb.OnPlacePointHighlightEvent?.Invoke(this, garb);

                OnHighlight?.Invoke(this, from);

                if(placedObject == null && forcePlace)
                    Place(from);
            }
        }

        internal virtual void StopHighlight(SpatialGrabbable from) {
            if(highlightingObj != null) {
                highlightingObj.OnPlacePointUnhighlightEvent?.Invoke(this, highlightingObj);
                foreach(var grabChild in highlightingObj.grabbableChildren)
                    grabChild.OnPlacePointUnhighlightEvent?.Invoke(this, grabChild);
                foreach(var garb in highlightingObj.grabbableParents)
                    garb.OnPlacePointUnhighlightEvent?.Invoke(this, garb);

                highlightingObj = null;
                OnStopHighlight?.Invoke(this, from);


                if (placedObject == null)
                    from.placePoint = (null);
                foreach(var garb in from.grabbableParents)
                    garb.placePoint = (null);
                foreach(var garb in from.grabbableChildren)
                    garb.placePoint = (null);
            }
        }



        protected bool IsStillOverlapping(SpatialGrabbable from, float scale = 1){
            var sphereCheck = Physics.OverlapSphereNonAlloc(placedOffset.position + placedOffset.rotation * radiusOffset, placeRadius * scale, collidersNonAlloc, placeLayers);
            for (int i = 0; i < sphereCheck; i++){
                if (collidersNonAlloc[i].attachedRigidbody == from.attachedRigidbody) {
                    return true;
                }
            }
            return false;
        }



        public virtual void SetStartPlaced() {
            if(startPlaced != null) {
                if(startPlaced.gameObject.scene.IsValid()) {
                    startPlaced.placePoint = (this);
                    Highlight(startPlaced);
                    Place(startPlaced);
                }
                else {
                    var instance = GameObject.Instantiate(startPlaced);
                    instance.placePoint = (this);
                    Highlight(instance);
                    Place(instance);

                }
            }
        }
        
        public SpatialGrabbable GetPlacedObject() {
            return placedObject;
        }

        protected virtual void OnRelativeGrabbed(SpatialHand pSpatialHand, SpatialGrabbable pSpatialGrabbable)
        {
            Remove();
        }

        protected virtual void OnJointBreak(float breakForce) {
            if(placedObject != null)
                Remove(placedObject);
        }

        void OnDrawGizmos() {
            if(placedOffset == null)
                placedOffset = transform;
            Gizmos.color = Color.white; 
            var scale = Mathf.Abs(transform.lossyScale.x < transform.lossyScale.y ? transform.lossyScale.x : transform.lossyScale.y);
            scale = Mathf.Abs(scale < transform.lossyScale.z ? scale : transform.lossyScale.z);

            Gizmos.DrawWireSphere(transform.rotation * radiusOffset + placedOffset.position, placeRadius* scale);
        }

    }
}
