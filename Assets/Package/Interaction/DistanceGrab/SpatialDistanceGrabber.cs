using Foundry.Networking;
using UnityEngine;
using UnityEngine.Events;

namespace Foundry {
    [DefaultExecutionOrder(int.MaxValue)]
    public class SpatialDistanceGrabber : NetworkComponent {
        [Header("Hand")]
        [Tooltip("The primaryHand used to trigger pulling or flicking")]
        public SpatialHand primaryHand;
        [Tooltip("Whether the hands default highlight system should stop triggering highlights and grabs while this pointer is activated")]
        public bool overridePrimaryHighlight = true;
        public SpatialHandPose pointerPose;
        public float pointerPoseTime = 0.15f;

        [Header("Pointing Options")]
        public Transform forward;
        public float maxRange = 25;
        public LayerMask layers;
        public LineRenderer line;
        public Gradient invalidColor;
        public Gradient highlightColor;


        [Header("EVENTS")]
        public UnityEvent<SpatialDistanceGrabber> StartPoint;
        public UnityEvent<SpatialDistanceGrabber> StopPoint;
        public UnityEvent<SpatialDistanceGrabber, SpatialDistanceGrabbable> StartTarget;
        public UnityEvent<SpatialDistanceGrabber, SpatialDistanceGrabbable> StopTarget;
        public UnityEvent<SpatialDistanceGrabber, SpatialDistanceGrabbable> OnPull;

        //Currently no functionality supports selectiong but this will allow for future implementation of pulling and time delayed activations
        internal UnityEvent<SpatialDistanceGrabber, SpatialDistanceGrabbable> StartSelect;
        internal UnityEvent<SpatialDistanceGrabber, SpatialDistanceGrabbable> StopSelect;

        SpatialDistanceGrabbable targetingDistanceGrabbable;
        SpatialDistanceGrabbable selectingDistanceGrabbable;
        SpatialGrabbableChild hitGrabbableChild;

        bool pointing;
        bool selecting;
        bool inputPointing;
        bool pulling;
        RaycastHit targetHit;
        RaycastHit selectionHit;

        GameObject _hitPoint;
        GameObject hitPoint {
            get {
                if(!gameObject.activeInHierarchy)
                    return null;

                if(_hitPoint == null) {
                    _hitPoint = new GameObject();
                    _hitPoint.name = "Distance Hit Point";
                    return _hitPoint;
                }

                return _hitPoint;
            }
        }

        public override void OnConnected()
        {
            if(!IsOwner)
                enabled = false;
        }

        void OnEnable() {
            primaryHand.OnBeforeGrabbedEvent.AddListener(OnBeforeGrabbed);
        }

        void OnDisable() {
            primaryHand.OnBeforeGrabbedEvent.RemoveListener(OnBeforeGrabbed);
        }

        void OnBeforeGrabbed(SpatialHand hand, SpatialGrabbable grab) {
            StopPointing(); 
            CancelSelect();
        }

        void LateUpdate() {
            CheckDistanceGrabbable();
            CheckInput();
        }

        void OnDestroy() {
            Destroy(hitPoint);
        }

        

        void CheckInput() {

            bool currentPointValue = primaryHand.handType == SpatialHand.HandType.Left ?
                SpatialInputManager.instance.toggleLeftPointerXR.action.ReadValue<float>() > 0.6f :
                SpatialInputManager.instance.toggleRightPointerXR.action.ReadValue<float>() > 0.6f;

            if(currentPointValue && !inputPointing) {
                inputPointing = true;
                StartPointing();
            }
            else if(!currentPointValue && inputPointing) {
                inputPointing = false;
                StopPointing();
            }

            bool currentSelectionValue = primaryHand.handType == SpatialHand.HandType.Left ?
                SpatialInputManager.instance.grabLeftXR.action.ReadValue<float>() > 0.6f :
                SpatialInputManager.instance.grabRightXR.action.ReadValue<float>() > 0.6f;

            if(currentSelectionValue && !selecting) {
                selecting = true;
                SelectTarget();
            }
            else if(!currentSelectionValue && selecting) {
                selecting = false;
                CancelSelect();
            }
        }

        void CheckDistanceGrabbable() {
            if(!pulling && pointing && primaryHand.held == null) {
                bool didHit = Physics.SphereCast(forward.position, 0.03f, forward.forward, out targetHit, maxRange, layers);
                SpatialDistanceGrabbable hitGrabbable;

                if(didHit) {
                    if(targetHit.transform.TryGetComponent(out hitGrabbable) || (targetHit.rigidbody != null && targetHit.rigidbody.TryGetComponent(out hitGrabbable))) {
                        if(targetingDistanceGrabbable == null || hitGrabbable != targetingDistanceGrabbable)
                            StartTargeting(hitGrabbable);
                    }
                    else if(targetHit.transform.TryGetComponent(out hitGrabbableChild)) {
                        if(hitGrabbableChild.grabParent.transform.TryGetComponent(out hitGrabbable)) {
                            if(targetingDistanceGrabbable == null || hitGrabbable != targetingDistanceGrabbable) 
                                StartTargeting(hitGrabbable);
                        }
                    }
                    else if(targetingDistanceGrabbable != null && targetHit.transform.gameObject.GetInstanceID() != targetingDistanceGrabbable.gameObject.GetInstanceID())
                        StopTargeting();
                }
                else 
                    StopTargeting();
                if(line != null) {
                    if(didHit) {
                        line.positionCount = 2;
                        line.SetPositions(new Vector3[] { forward.position, targetHit.point });
                    }
                    else {
                        line.positionCount = 2;
                        line.SetPositions(new Vector3[] { forward.position, forward.position + forward.forward * maxRange });
                    }
                }
            }
            else if(targetingDistanceGrabbable != null) {
                StopTargeting();
            }
        }




        public virtual void StartPointing() {
            if(primaryHand.held != null)
                return;

            pointing = true;
            StartPoint?.Invoke(this);
            if(pointerPose != null) {
                primaryHand.poseAnimator.SetPose(pointerPose, pointerPoseTime, true, true);
            }
            line.enabled = true;
            if(overridePrimaryHighlight)
                primaryHand.DisableHighlighter();
        }

        public virtual void StopPointing() {
            pointing = false;
            line.enabled = false;
            StopPoint?.Invoke(this);
            StopTargeting();
            if(pointerPose != null) {
                primaryHand.poseAnimator.ClearPose();
            }

            if(overridePrimaryHighlight)
                primaryHand.EnableHighlighter();
        }



        public virtual void StartTargeting(SpatialDistanceGrabbable target) {
            if(target.enabled && primaryHand.CanGrab(target.grabbable)) {
                if(targetingDistanceGrabbable != null)
                    StopTargeting();
                targetingDistanceGrabbable = target;

                targetingDistanceGrabbable?.StartTargeting?.Invoke(this, target);
                StartTarget?.Invoke(this, target);

                primaryHand.Highlight(new HighlightData() {
                    collider = targetHit.collider,
                    currentTarget = target.grabbable,
                    closestPoint = targetHit.point,
                    distance = Vector3.Distance(primaryHand.palmCenterTransform.position, targetHit.point)
                });

                line.colorGradient = highlightColor;
            }
        }

        public virtual void StopTargeting() {
            targetingDistanceGrabbable?.StopTargeting?.Invoke(this, targetingDistanceGrabbable);
            if(targetingDistanceGrabbable != null) {
                StopTarget?.Invoke(this, targetingDistanceGrabbable);
            }
            else if(selectingDistanceGrabbable != null) {
                StopTarget?.Invoke(this, selectingDistanceGrabbable);
            }

            primaryHand.StopHighlight();
            targetingDistanceGrabbable = null;


            line.colorGradient = invalidColor;
        }

        public virtual void SelectTarget() {
            if(targetingDistanceGrabbable != null) {
                pulling = true;
                selectionHit = targetHit;
                hitPoint.transform.position = selectionHit.point;
                hitPoint.transform.parent = selectionHit.transform;
                selectingDistanceGrabbable = targetingDistanceGrabbable;

                selectingDistanceGrabbable?.StartSelecting?.Invoke(this, selectingDistanceGrabbable);
                targetingDistanceGrabbable?.StopTargeting?.Invoke(this, selectingDistanceGrabbable);
                StartSelect?.Invoke(this, selectingDistanceGrabbable);

                ActivatePull();
                StopPointing();
                targetingDistanceGrabbable = null;
            }
        }

        public virtual void CancelSelect() {
            StopTargeting();
            pulling = false;

            selectingDistanceGrabbable?.StopSelecting?.Invoke(this, selectingDistanceGrabbable);

            if(selectingDistanceGrabbable != null)
                StopSelect?.Invoke(this, selectingDistanceGrabbable);
            selectingDistanceGrabbable = null;
        }

        public virtual void ActivatePull() 
        {
            if(selectingDistanceGrabbable != null) 
            {
                selectionHit.point = hitPoint.transform.position;

                OnPull?.Invoke(this, selectingDistanceGrabbable);
                selectingDistanceGrabbable.OnPull?.Invoke(this, selectingDistanceGrabbable);


                var hitToCenterDistance = Vector3.Distance(selectionHit.point, selectionHit.transform.position);
                selectingDistanceGrabbable.grabbable.transform.position -= selectionHit.point - primaryHand.palmCenterTransform.position;
                selectingDistanceGrabbable.grabbable.transform.position += primaryHand.palmCenterTransform.forward * hitToCenterDistance;
                selectingDistanceGrabbable.grabbable.attachedRigidbody.position = selectingDistanceGrabbable.grabbable.transform.position;

                var closestPoint = selectionHit.collider.ClosestPoint(primaryHand.palmCenterTransform.transform.position);
                primaryHand.UpdateHighlightInfo(selectionHit.collider, Vector3.Distance(primaryHand.palmCenterTransform.position, targetHit.point), closestPoint, selectingDistanceGrabbable.grabbable);

                primaryHand.ForceGrab(selectingDistanceGrabbable.grabbable);

                selectingDistanceGrabbable?.CancelTarget();
                CancelSelect();
            }
        }
    }
}
