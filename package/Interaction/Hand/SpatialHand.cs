using System;
using System.Collections;
using System.Collections.Generic;
using Foundry.Networking;
using UnityEngine;
using UnityEngine.Events;

namespace Foundry
{
    public class HighlightData {
        SpatialGrabbable _currentTarget;
        public SpatialGrabbable currentTarget;
        public Collider collider;
        public Vector3 closestPoint;
        public float distance;
    }

    /// <summary>
    /// IK is set to execute at -500, so this should be set to execute after that, since the hand position will change after IK with the current system
    /// </summary>
    [Serializable] public class SpatialHandGrabEvent : UnityEvent<SpatialHand, SpatialGrabbable> { }
    [DefaultExecutionOrder(-400)]
    public class SpatialHand : NetworkComponent {

        public const string SpatialGrabbableLayerNameDefault = "FoundryGrabbable";
        public const string SpatialHandLayerNameDefault = "FoundryHand";

        public enum HandType
        {
            Right,
            Left,
            Both
        }


        [Header("Hand Settings")]
        public HandType handType;
        public Transform palmCenterTransform;
        public SpatialFinger index;
        public SpatialFinger middle;
        public SpatialFinger ring;
        public SpatialFinger pinky;
        public SpatialFinger thumb;
        public bool trackContacts = true;
        public float throwVelocityExpireTime = 0.125F;
        public float throwAngularVelocityExpireTime = 0.125F;
        public float throwStrength = 1F;
        public float throwAngularStrength = 1F;

        [Space]
        [Header("Highlight Settings")]
        public bool highlighterEnabled = true;
        public float grabRadius = 0.2f;
        public LayerMask grabMask = ~0;

        [Space]
        [Header("Event Settings")]
        public bool enableEvents = true;
        public SpatialHandGrabEvent OnGrabEvent;
        public SpatialHandGrabEvent OnReleaseEvent;
        public SpatialHandGrabEvent OnHighlightEvent;
        public SpatialHandGrabEvent OnStopHighlightEvent;

        [HideInInspector] public SpatialHandGrabEvent OnBeforeGrabbedEvent;
        [HideInInspector] public SpatialHandGrabEvent OnBeforeReleasedEvent;

        [HideInInspector] public NetworkProperty<NetworkId> NetworkHeld = new(NetworkId.Invalid);
        /// <summary>
        /// This event is called when a hand grabs in a networked scene. The NetworkId is the id of the grabbed object.
        /// </summary>
        [HideInInspector] public NetworkEvent<NetworkId> OnGrabNetworkEvent = new();
        /// <summary>
        /// This event is called when a hand releases in a networked scene. The NetworkId is the id of the released object.
        /// </summary>
        [HideInInspector] public NetworkEvent<NetworkId> OnReleaseNetworkEvent = new();

        [HideInInspector] public SpatialHandPoseAnimator poseAnimator;


        SpatialGrabbable _held;
        public NetworkProperty<PosRot> offset;
        public HighlightData highlightInfo { get; private set; }

        public Vector3 velocity;
        Vector3 lastPos;

        private float velocityFrameTime = 10F;

        private HandVelocityTracker _handVelocityTracker;

        public SpatialGrabbable held { get { return _held; }  }
        public SpatialGrabbable lastHeld;

        bool highlighting = false;



        private void Awake() {
            grabMask |= LayerMask.GetMask(SpatialGrabbableLayerNameDefault);

            if(!TryGetComponent(out poseAnimator))
                poseAnimator = gameObject.AddComponent<SpatialHandPoseAnimator>();

            if(highlightInfo == null)
                highlightInfo = new HighlightData();

            index.hand = this;
            middle.hand = this;
            ring.hand = this;
            pinky.hand = this;
            thumb.hand = this;

            _handVelocityTracker = new HandVelocityTracker(this);
        }

        private void Start()
        {
            // Set up network callbacks for grabbing and releasing
            if (NetworkManager.instance)
            {
                OnGrabNetworkEvent.AddListener((s, id) =>
                {
                    if(s == NetEventSource.Remote)
                        GrabNetwork(id);
                });
                
                OnReleaseNetworkEvent.AddListener((s, id) =>
                {
                    if(s == NetEventSource.Remote)
                        ReleaseNetwork(id);
                });
            }
        }

        #region Networking

        private NetworkTransform netTransform;

        public override void RegisterProperties(List<INetworkProperty> props)
        {
            props.Add(NetworkHeld);
            props.Add(offset);
            props.Add(OnGrabNetworkEvent);
            props.Add(OnReleaseNetworkEvent);
        }

        public override void OnConnected()
        {
            if(NetworkHeld.Value.IsValid())
                GrabNetwork(NetworkHeld.Value);
        }

        void GrabNetwork(NetworkId id)
        {
            var netObj = NetworkManager.GetObjectById(id);
            if (netObj && netObj.TryGetComponent(out SpatialGrabbable grabbable))
                ForceGrab(grabbable, true);
        }
        
        void ReleaseNetwork(NetworkId id)
        {
            if (held)
                Release();
        }

        #endregion


        #region GRAB/RELEASE
        bool cancelGrab = false;
        bool cancelRelease = false;
        public bool ForceGrab(SpatialGrabbable grabbable, bool ignoreConditions = false)
        {
            if (CanGrab(grabbable) || ignoreConditions)
            {
                StopHighlight();
                Release();

                if (enableEvents)
                    OnBeforeGrabbedEvent?.Invoke(this, grabbable);
                grabbable.OnBeforeGrab(this);

                if (!cancelGrab)
                {
                    CreateGrabConnection(grabbable);
                    
                    if (enableEvents)
                        OnGrabEvent?.Invoke(this, grabbable);
                    grabbable.OnGrab(this);
                }
                bool wasGrabbed = !cancelGrab;
                cancelGrab = false;
                return wasGrabbed;
            }
            else
            {
                Debug.Log(transform.name + " cannot grab this target", grabbable.gameObject);
                return false;
            }
        }

        public bool CanGrab(SpatialGrabbable grabbable)
        {
            return grabbable.CanGrab(this);
        }

        /// <summary>Attempts to grab the current highlight target</summary>
        public void Grab()
        {
            if (highlightInfo.currentTarget != null && highlighterEnabled)
            {
                ForceGrab(highlightInfo.currentTarget);
            }
        }

        /// <summary>This will cancel the next grab. Should be called from the OnBeforeGrabEvent</summary>
        public void CancelGrab()
        {
            cancelGrab = true;
        }


        void CreateGrabConnection(SpatialGrabbable grabbable)
        {
            _held = grabbable;
            if (grabbable.TryGetComponent(out Rigidbody body))
            {
                body.isKinematic = true;
            }

            
            grabbable.TryGetComponent(out netTransform);
            
            if (IsOwner)
            {
                offset.Value = new PosRot
                {
                    pos = Quaternion.Inverse(transform.rotation) * (grabbable.transform.position - transform.position),
                    rot = Quaternion.Inverse(transform.rotation) * grabbable.transform.rotation
                };
                
                if (grabbable.TryGetComponent(out NetworkObject netObj))
                {
                    netObj.RequestOwnership();
                    NetworkHeld.Value = netObj.NetworkedGraphId.Value;
                    OnGrabNetworkEvent.InvokeRemote(NetworkHeld.Value);
                }
            }
        }
        

        /// <summary>Releases the current held grabbable</summary>
        public void Release()
        {
            if (!held)
                return;

            if (enableEvents && OnBeforeReleasedEvent != null)
                OnBeforeReleasedEvent.Invoke(this, held);
            held.OnBeforeRelease(this);


            if (!cancelRelease)
            {
                lastHeld = held;
                BreakGrabConnection(held);
                    
                if (enableEvents && OnReleaseEvent != null)
                    OnReleaseEvent.Invoke(this, lastHeld);
                lastHeld.OnRelease(this);
                highlightInfo.currentTarget = null;
            }
            cancelRelease = false;

        }

        /// <summary>This will cancel the next grab. Should be called from the OnBeforeRelease</summary>
        public void CancelRelease()
        {
            cancelRelease = true;
        }


        void BreakGrabConnection(SpatialGrabbable grabbable)
        {
            if (held != null)
            {
                if (grabbable.TryGetComponent(out Rigidbody body))
                {
                    body.isKinematic = false;

                    if (IsOwner)
                    {
                        //Throw the object
                        body.velocity = _handVelocityTracker.ThrowVelocity();
                        body.angularVelocity = _handVelocityTracker.ThrowAngularVelocity();
                    }
                }

                if (IsOwner)
                {

                    OnReleaseNetworkEvent.InvokeRemote(NetworkHeld.Value);
                    NetworkHeld.Value = NetworkId.Invalid;
                }
                

                netTransform = null;
                _held = null;
            }
        }


        void FixedUpdate()
        {
            _handVelocityTracker.UpdateThrowing();

            if(trackContacts)
                UpdateHighlight();
            if(_held)
            {
                Vector3 targetPos = transform.position + transform.rotation * offset.Value.pos;
                Quaternion targetRot = transform.rotation * offset.Value.rot;

                var nt = netTransform.transform;
                nt.position = targetPos;
                nt.rotation = targetRot;

                var lo = netTransform.lerpObject;
                lo.position = targetPos;
                lo.rotation = targetRot;
            }
        }

        void LateUpdate()
        {
            if (netTransform)
            {
                Vector3 targetPos = transform.position + transform.rotation * offset.Value.pos;
                Quaternion targetRot = transform.rotation * offset.Value.rot;

                var nt = netTransform.transform;
                nt.position = targetPos;
                nt.rotation = targetRot;

                var lo = netTransform.lerpObject;
                lo.position = targetPos;
                lo.rotation = targetRot;
            }
        }

        IEnumerator CalculateVelocity()
        {
            velocity = (transform.position - lastPos) / Time.fixedDeltaTime;
            yield return new WaitForSeconds(velocityFrameTime);
            lastPos = transform.position;
        }



        #endregion




        #region HIGHLIGHT

        public void EnableHighlighter() {
            highlighterEnabled = true;
        }

        public void DisableHighlighter() {
            highlighterEnabled = false;
            StopHighlight();
        }

        public void UpdateHighlightInfo(Collider highlightCollider, float distance, Vector3 closestPoint, SpatialGrabbable grabTarget) {

            highlightInfo.collider = highlightCollider;
            highlightInfo.distance = distance;
            highlightInfo.closestPoint = closestPoint;
            highlightInfo.currentTarget = grabTarget;
        }

        Collider[] grabbableCollidersNonAlloc = new Collider[256];
        public void UpdateHighlight()
        {
            if(!highlighterEnabled || held != null)
                return;

            var newHighlight = new HighlightData();
            newHighlight.distance = float.MaxValue;
            newHighlight.closestPoint = Vector3.one * int.MaxValue;
            //newHighlight.currentTarget = null;

            Vector3 newPoint;
            float newDistance;
            int overlapCount = Physics.OverlapSphereNonAlloc(palmCenterTransform.position + palmCenterTransform.forward * grabRadius / 2f, grabRadius, grabbableCollidersNonAlloc, grabMask);
            SpatialGrabbable grab;
            for (int i = 0; i < overlapCount; i++)
            {
                if ((grabbableCollidersNonAlloc[i].attachedRigidbody != null && grabbableCollidersNonAlloc[i].attachedRigidbody.TryGetComponent(out grab)) || grabbableCollidersNonAlloc[i].TryGetComponent(out grab))
                {
                    var closestPointTarget = palmCenterTransform.position + palmCenterTransform.forward * grabRadius / 4f;
                    newPoint = grabbableCollidersNonAlloc[i].ClosestPoint(palmCenterTransform.transform.position);
                    newDistance = Vector3.Distance(closestPointTarget, newPoint);
                    if (newDistance < newHighlight.distance) {
                        newHighlight.collider = grabbableCollidersNonAlloc[i];
                        newHighlight.distance = newDistance;
                        newHighlight.closestPoint = newPoint;
                        newHighlight.currentTarget = grab;
                    }
                }
            }

            if(newHighlight.currentTarget != highlightInfo.currentTarget) {     
                Highlight(newHighlight);
            }
            //If highlight has been turned off and a highlight target exists
            else if (newHighlight.currentTarget == null){
                StopHighlight();
            }

        }

        public void Highlight(HighlightData newHighlightData)
        {
            if (newHighlightData.currentTarget != null)
            {
                //If a new target is found and we have a current target, stop highlighting the last target
                if (highlightInfo.currentTarget != null && newHighlightData.currentTarget.GetInstanceID() != highlightInfo.currentTarget.GetInstanceID())
                    StopHighlight();

                highlightInfo.collider = newHighlightData.collider;
                highlightInfo.distance = newHighlightData.distance;
                highlightInfo.closestPoint = newHighlightData.closestPoint;
                highlightInfo.currentTarget = newHighlightData.currentTarget;

                if (enableEvents && OnHighlightEvent != null)
                    OnHighlightEvent.Invoke(this, newHighlightData.currentTarget);
                highlightInfo.currentTarget.OnHighlight(this);
                highlighting = true;

            }
            else if (newHighlightData.currentTarget == null)
            {
                //If a new target is not found, stop highlighting the last target
                if (highlightInfo.currentTarget != null)
                    StopHighlight();
            }
        }

        
        public void SetPose(SpatialHandPose pose) {
            poseAnimator.SetPose(pose, 0.2f, true, true);
        }

        public void ClearPose() {
            poseAnimator.ClearPose();
        }



        public void StopHighlight(){
            if (highlightInfo.currentTarget != null){
                if (enableEvents && OnStopHighlightEvent != null)
                    OnStopHighlightEvent.Invoke(this, highlightInfo.currentTarget);
                highlightInfo.currentTarget.StopHighlight(this);
                highlightInfo.currentTarget = null;
                highlighting = false;
            }
        }

        public bool IsHighlighting() {
            return highlighting;
        }

        #endregion


        private void OnDrawGizmosSelected()
        {
            if (palmCenterTransform != null)
                Gizmos.DrawWireSphere(palmCenterTransform.position + palmCenterTransform.forward * grabRadius / 2f, grabRadius);
        }

    }
}