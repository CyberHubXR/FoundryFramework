using System;
using System.Collections;
using System.Collections.Generic;
using CyberHub.Brane;
using Foundry.Networking;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.InputSystem;


namespace Foundry
{

    [Serializable]
    public class TrackerRefs
    {
        public Transform head;
        public Transform leftHand;
        public Transform rightHand;
        public Transform waist;
        public Transform leftFoot;
        public Transform rightFoot;
    }
    
    [RequireComponent(typeof(CharacterController))]
    public class Player : NetworkComponent
    {
        [System.Serializable]
        public struct MovementSettings
        {
            public float speed;

            public float sprintSpeed;

            [Tooltip("Stick player to ground by adding downward vector")]
            public float downforce;

            [Tooltip("Amount to exend collider beyond head tracker")]
            public float headPadding;
        }

        // Adjust the value for sprinting as needed ** currently set to 1.5f 
        [SerializeField] private float sprintSpeedMultiplier = 1.5f;
        
        [Header("Movement")]
        public bool movementEnabled = true;
        public MovementSettings movementSettings;

        [Header("Hands")]
        public SpatialHand leftHand;
        public SpatialHand rightHand;
        
        [Header("Trackers")]
        public TrackerRefs trackers;

        [Header("Avatar")]
        public Avatar avatar;

        [Header("Events")] 
        public bool enableEvents = true;
        public UnityEvent<Player, Vector3, Quaternion> onBeforeTeleport;
        public UnityEvent<Player> onAfterTeleport;
        
        [FormerlySerializedAs("networkId")] [HideInInspector]
        public NetworkProperty<UInt64> playerId = new(UInt64.MaxValue);

        private NetworkArray<bool> enabledTrackers = new(6);
        
        private CharacterController controller;
        private IPlayerControlRig controlRig;
        
        private NetworkProperty<TrackingMode> trackingMode = new NetworkProperty<TrackingMode>(TrackingMode.OnePoint);

        [SerializeField] private TeleportRaycaster _teleportRaycaster;
        private bool _teleportPreview = false;
        
        //Used within the before teleport event to cancel the teleport
        private bool cancelTeleport = false;
        
        // Keep track of whether the player is currently sprinting
        private bool isSprinting = false;

        private NetworkProperty<Vector3> virtualVelocity = new(Vector3.zero);
        
        public override void RegisterProperties(List<INetworkProperty> properties, List<INetworkEvent> events)
        {
            properties.Add(playerId);
            properties.Add(trackingMode);
            properties.Add(enabledTrackers);
            properties.Add(virtualVelocity);
        }

        void Awake()
        {
            controller = GetComponent<CharacterController>();

            gameObject.layer = LayerMask.NameToLayer("FoundryPlayer");
            
            // We'll set all of these active later depending on the tracking mode
            trackers.leftHand.gameObject.SetActive(false);
            trackers.rightHand.gameObject.SetActive(false);
            trackers.waist.gameObject.SetActive(false);
            trackers.leftFoot.gameObject.SetActive(false);
            trackers.rightFoot.gameObject.SetActive(false);

            trackingMode.OnValueChanged += tm =>
            {
                if(avatar)
                    avatar.SetTrackingMode(tm);
            };
        }

        void Start()
        {
            // If this is an offline local player we can just borrow the rig
            if (!NetworkManager.instance)
                BorrowControlRig();
        }

        public override void OnConnected()
        {
            if (IsOwner)
            {
                playerId.Value = NetworkManager.instance.LocalPlayerId;
                BorrowControlRig();
            }
        }

        private void LoadControlRig()
        {
            controlRig.transform.SetParent(transform, false);
            controlRig.transform.localPosition = Vector3.zero;
            controlRig.transform.localRotation = Quaternion.identity;
            
            //If this is a desktop rig, change the camera mode
            if(controlRig is DesktopControlRig)
                ((DesktopControlRig)controlRig).SetCameraMode(DesktopControlRig.CameraMode.Look);
            
            trackingMode.Value = controlRig.GetTrackingMode();
        }

        public void BorrowControlRig()
        {
            var rigManager = BraneApp.GetService<IPlayerRigManager>();
            if (rigManager.Rig != null)
            {
                controlRig = rigManager.BorrowPlayerRig();
                LoadControlRig();
            }
            else
            {
                rigManager.PlayerRigCreated += rig =>
                {
                    controlRig = rigManager.BorrowPlayerRig();
                    LoadControlRig();
                };
            }
        }

        private void OnDestroy()
        {
            if (controlRig != null)
            {
                var rigManager = BraneApp.GetService<IPlayerRigManager>();
                rigManager.ReturnPlayerRig();
            }
        }

        /// <summary>
        /// Grab a reference to the spawned control rig
        /// </summary>
        /// <returns></returns>
        /// <seealso cref="IPlayerControlRig"/>
        public IPlayerControlRig ControlRig()
        {
            return controlRig;
        }


        public void UpdateTrackers()
        {
            if (controlRig != null)
            {
                // Request the current positions of all trackers
                var targetPoses = new TrackerPos[6];
                controlRig.UpdateTrackers(targetPoses);

                for (int i = 0; i < 6; i++)
                    enabledTrackers[i] = targetPoses[i].enabled;
                // Update the tracker positions
                trackers.head.localPosition = targetPoses[0].translation;
                trackers.head.localRotation = targetPoses[0].rotation;

                if (targetPoses[1].enabled)
                {
                    trackers.leftHand.localPosition = targetPoses[1].translation;
                    trackers.leftHand.localRotation = targetPoses[1].rotation;
                }

                if (targetPoses[2].enabled)
                {
                    trackers.rightHand.localPosition = targetPoses[2].translation;
                    trackers.rightHand.localRotation = targetPoses[2].rotation;
                }

                if (targetPoses[3].enabled)
                {
                    trackers.waist.localPosition = targetPoses[3].translation;
                    trackers.waist.localRotation = targetPoses[3].rotation;
                }

                if (targetPoses[4].enabled)
                {
                    trackers.leftFoot.localPosition = targetPoses[4].translation;
                    trackers.leftFoot.localRotation = targetPoses[4].rotation;
                }

                if(targetPoses[5].enabled)
                {
                    trackers.rightFoot.localPosition = targetPoses[5].translation;
                    trackers.rightFoot.localRotation = targetPoses[5].rotation;
                }
            }
            trackers.head.gameObject.SetActive(enabledTrackers[0]);
            trackers.leftHand.gameObject.SetActive(enabledTrackers[1]);
            trackers.rightHand.gameObject.SetActive(enabledTrackers[2]);
            trackers.waist.gameObject.SetActive(enabledTrackers[3]);
            trackers.leftFoot.gameObject.SetActive(enabledTrackers[4]);
            trackers.rightFoot.gameObject.SetActive(enabledTrackers[5]);
            
            UpdateColliderSize();
        }

        public void UpdateColliderSize()
        {
            Vector3 headDelta = trackers.head.localPosition;
            float height = Mathf.Max(controller.radius, headDelta.y + movementSettings.headPadding);
            controller.height = height;
            controller.center = new Vector3
            {
                x = headDelta.x,
                y = height / 2 + controller.skinWidth,
                z = headDelta.z
            };
        }

        public Vector3 GetMovement()
        {
            if (controlRig == null)
                return Vector3.zero;
            var input = SpatialInputManager.instance;
            Vector2 movementInput = SpatialInputManager.movementInput;
            Vector3 movement = new Vector3(movementInput.x, 0, movementInput.y);
            Quaternion movementReferenceRot = Quaternion.identity;
            if (controlRig.GetTrackingMode() == TrackingMode.OnePoint) 
                movementReferenceRot = trackers.head.rotation;
            else switch (input.movementReference)
            {
                case SpatialInputManager.MovementReference.Head:
                    movementReferenceRot = trackers.head.rotation;
                    break;
                case SpatialInputManager.MovementReference.LeftHand:
                    movementReferenceRot = trackers.leftHand.rotation;
                    break;
                case SpatialInputManager.MovementReference.RightHand:
                    movementReferenceRot = trackers.rightHand.rotation;
                    break;
            }
            
            // Read sprint input values for desktop and VR modes
            bool isSprintingDesktop = input.sprintDesktop.action.ReadValue<float>() > 0;
            bool isSprintingXR = input.sprintXR.action.ReadValue<float>() > 0;
            
            // Calculate the current speed based on sprinting status
            float currentSpeed = movementSettings.speed;
            if (isSprintingDesktop || isSprintingXR)
                currentSpeed *= sprintSpeedMultiplier;

            movement = Quaternion.AngleAxis(movementReferenceRot.eulerAngles.y, Vector3.up) * movement;
            return movement * currentSpeed;
        }

        public void Move(Vector3 movement, float deltaTime)
        {
            controller.Move(movement * deltaTime + Vector3.down * movementSettings.downforce);
            virtualVelocity.Value = movement;
        }
        
        public TeleportRaycaster TeleportRaycaster()
        {
            return _teleportRaycaster;
        }

        public void UpdateTeleportRaycaster()
        {
            if (!_teleportRaycaster)
                return;
            float input = SpatialInputManager.instance.turnXR.action.ReadValue<Vector2>().y;

            if (_teleportPreview && input <= 0.5f)
            {
                if (_teleportRaycaster.TryTeleport(out Vector3 point, out Vector3 forward))
                    TeleportLook(point, forward);
            }
            _teleportPreview = input > 0.5f;

            if(input >= 0.5f)
                _teleportRaycaster.ActivateRaycaster();
            else
                _teleportRaycaster.DeactivateRaycaster();
        }

        public void Update()
        {
            UpdateTrackers();
            if (IsOwner)
            {
                if (leftHand)
                {
                    if (SpatialInputManager.instance.grabLeftXR.action.WasPressedThisFrame())
                        leftHand.Grab();
                    else if (SpatialInputManager.instance.grabLeftXR.action.WasReleasedThisFrame())
                        leftHand.Release();
                }

                if (rightHand)
                {
                    if (SpatialInputManager.instance.grabRightXR.action.WasPressedThisFrame() || 
                        SpatialInputManager.instance.grabDesktop.action.WasPressedThisFrame())
                        rightHand.Grab();
                    else if (SpatialInputManager.instance.grabRightXR.action.WasReleasedThisFrame() ||
                             SpatialInputManager.instance.grabDesktop.action.WasReleasedThisFrame())
                        rightHand.Release();
                }
                
            }

            if(IsOwner && movementEnabled)
                Move(GetMovement(), Time.deltaTime);
            if (avatar)
                avatar.SetVirtualVelocity(virtualVelocity.Value);
            if(IsOwner)
                UpdateTeleportRaycaster();
        }

        public void LateUpdate()
        {
            UpdateTrackers();
        }


        /// <summary>
        /// Cancels the current teleport event
        /// Should only be called within functions listening to the before teleport event
        /// </summary>
        public void CancelTeleport()
        {
            cancelTeleport = true;
        }
        
        /// <summary>
        /// Move player to a position
        /// </summary>
        /// <param name="position"></param>
        public void Teleport(Vector3 position) {

            TeleportLook(position, trackers.head.forward);
        }

        /// <summary>
        /// Move player to a position and face a direction
        /// </summary>
        /// <param name="position">position avatar should be standing at</param>
        /// <param name="forward">direction player should be looking</param>
        public void TeleportLook(Vector3 position, Vector3 forward) {
            TeleportLook(position, forward, transform.up);
        }
        
        /// <summary>
        /// Move player to a position and face a direction
        /// </summary>
        /// <param name="position">position avatar should be standing at</param>
        /// <param name="forward">direction player should be looking</param>
        /// <param name="up">up direction of the avatar</param>
        public void TeleportLook(Vector3 position, Vector3 forward, Vector3 up) {
            if(forward == Vector3.zero) {
                Teleport(position);
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, up), up);

            Quaternion headRotOffset = Quaternion.Euler(0, -trackers.head.localRotation.eulerAngles.y, 0);
            targetRotation *= headRotOffset;
            
            Vector3 headPosOffset = trackers.head.localPosition;
            headPosOffset.y = 0;
            
            Vector3 targetPos = position - targetRotation * headPosOffset;
            
            TeleportRaw(targetPos, targetRotation);
        }

        /// <summary>
        /// Handles teleporting the player to a new position and rotation, does not take into account any offset from the head tracker
        /// </summary>
        /// <param name="position">position that the player object will be moved too</param>
        /// <param name="rotation">rotation that the player root will set to</param>
        public void TeleportRaw(Vector3 position, Quaternion rotation)
        {
            cancelTeleport = false;
            if(enableEvents)
                onBeforeTeleport.Invoke(this, position, rotation);
            if (cancelTeleport)
                return;
            
            transform.position = position;
            transform.rotation = rotation;

            if (gameObject.TryGetComponent(out NetworkTransform t))
            {
                t.Teleport(position, rotation);
            }

            onAfterTeleport.Invoke(this);
        }
    }
}
