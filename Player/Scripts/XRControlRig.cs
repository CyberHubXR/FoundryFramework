using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using InputDevice = UnityEngine.XR.InputDevice;

namespace Foundry
{
    public class XRControlRig : MonoBehaviour, IPlayerControlRig
    {
        public ControllerConfig controllerConfig;
        [Header("References")]
        public Transform head;
        public Transform leftController;
        public Transform rightController;
        public Transform waistTracker;
        public Transform leftFootTracker;
        public Transform rightFootTracker;

        [Header("Input")]
        public InputAction headsetSensor;

        private TrackingMode trackingMode = TrackingMode.ThreePoint;

        private TeleportRaycaster teleportRaycaster;

        private Vector2 lastRotationInput;

        [System.Serializable]
        public struct Offsets
        {
            public PosRot head;
            public PosRot leftHand;
            public PosRot rightHand;
            public PosRot waist;
            public PosRot leftFoot;
            public PosRot rightFoot;
        }
        [Tooltip("These offsets are applied to the controllers and head to account for the physical tracker's position and rotation, automatically configured if a controller config is set")]
        public Offsets offsets;
        
        void Start()
        {
            Debug.Assert(head, "Head object reference not set!");
            Debug.Assert(leftController, "Left controller object reference not set!");
            Debug.Assert(rightController, "Right controller object reference not set!");
            SpatialInputManager.ActivateActions(trackingMode);
            GetControllerOffsets();
            UpdateTrackingMode();

            headsetSensor.Enable();
            
            // Tracking mode only seems to get set if the headset is currently active, so we need to set it again when the headset is turned on
            headsetSensor.performed += ctx =>
            {
                UpdateTrackingMode();
            };
        }

        void UpdateTrackingMode()
        {
            
            List<XRInputSubsystem> subsystems = new();
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var s in subsystems)
                s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Device);
            foreach (var s in subsystems)
                s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
        }

        public void Update()
        {
            trackingMode = waistTracker.localPosition != Vector3.zero ? TrackingMode.SixPoint : TrackingMode.ThreePoint;
            UpdateInput();
        }

        private void UpdateInput()
        {
            var input = SpatialInputManager.instance;
            Vector2 rotation = input.turnXR.action.ReadValue<Vector2>();
            if(Mathf.Abs(lastRotationInput.x) < 0.5f && Mathf.Abs(rotation.x) >= 0.5f)
            {
                if (rotation.x > 0)
                    transform.RotateAround(head.position, Vector3.up, input.snapTurnAngle);
                else
                    transform.RotateAround(head.position, Vector3.up, -input.snapTurnAngle);
            }
            
            lastRotationInput = rotation;
        }
        
        void OnEnable()
        {
            //Account for if controllers were not turned on when the app launched 
            InputDevices.deviceConnected += DeviceConnected;
        }

        void OnDisable()
        {
            InputDevices.deviceConnected -= DeviceConnected;
        }

        void GetControllerOffsets()
        {
            var device = controllerConfig.GetCurrentDeviceOffsets();
            offsets.head = device.headOffset;
            offsets.leftHand = device.controllerOffsets.leftOffset;
            offsets.rightHand = device.controllerOffsets.rightOffset;
        }

        void DeviceConnected(InputDevice device)
        {
            if ((device.characteristics & InputDeviceCharacteristics.HeldInHand) > 0)
                GetControllerOffsets();
            
            List<XRInputSubsystem> subsystems = new();
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var s in subsystems)
            {
                s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
                s.TryRecenter();
            }
        }

        public void UpdateTrackerPos(ref TrackerPos pos, Transform tracker, in PosRot offset)
        {
            pos.enabled = true;
            Transform parent = transform.parent;
            
            Quaternion parentRot = Quaternion.Inverse(parent.rotation);
            Vector3 localPos = parentRot * (tracker.position - parent.position);
            Quaternion localRot = parentRot * tracker.rotation;

            pos.translation = localPos + localRot * offset.pos;
            pos.rotation = localRot * offset.rot;
        }

        public Transform TrackerTransform(TrackerType type) {
            switch(type) {
                case TrackerType.head:
                    return head;

                case TrackerType.leftHand:
                    return leftController;

                case TrackerType.rightHand:
                    return rightController;

                case TrackerType.waist:
                    return waistTracker;

                case TrackerType.leftFoot:
                    return leftFootTracker;

                case TrackerType.rightFoot:
                    return rightFootTracker;
            }

            return null;
        }
        public TrackingMode GetTrackingMode()
        {
            return trackingMode;
        }

        public void UpdateTrackers(TrackerPos[] trackers)
        {
            UpdateTrackerPos(ref trackers[0], head, offsets.head);
            if (trackingMode > TrackingMode.OnePoint)
            {
                UpdateTrackerPos(ref trackers[1], leftController, offsets.leftHand);
                UpdateTrackerPos(ref trackers[2], rightController, offsets.rightHand);
            }

            if (trackingMode > TrackingMode.ThreePoint)
            {
                UpdateTrackerPos(ref trackers[3], waistTracker, offsets.waist);
                UpdateTrackerPos(ref trackers[4], leftFootTracker, offsets.leftFoot);
                UpdateTrackerPos(ref trackers[5], rightFootTracker, offsets.rightFoot);
            }
        }
    }
}
