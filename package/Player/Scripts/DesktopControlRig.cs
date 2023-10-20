using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Foundry
{
    public class DesktopControlRig : MonoBehaviour, IPlayerControlRig {
        [Header("References")]
        public Transform neckPivot;
        public Transform cameraHolder;
        public Transform reachTarget;

        public enum CameraMode
        {
            Look,
            InteractUI
        }
        
        public CameraMode cameraMode = CameraMode.InteractUI;

        //private Player player;
        private Vector2 lookDir;

        void Start()
        {
            SpatialInputManager.ActivateActions(TrackingMode.OnePoint);
        }

       public void Update()
        {
            if (cameraMode == CameraMode.Look)
            {
                var input = SpatialInputManager.instance;
                Vector2 lookDelta = input.lookDesktop.action.ReadValue<Vector2>() * input.lookSpeed;
                lookDir.x = Mathf.Clamp(lookDir.x - lookDelta.y, -90f, 90f);
                lookDir.y = Mathf.Repeat(lookDir.y + lookDelta.x, 360);
                neckPivot.localRotation = Quaternion.Euler(lookDir.x, lookDir.y, 0);
            }
        }

        public void SetCameraMode(CameraMode mode)
        {
            if (mode == CameraMode.Look)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            
            cameraMode = mode;
        }

        public bool IsReaching()
        {
            return SpatialInputManager.instance.reachDesktop.action.ReadValue<float>() > 0.5f;
        }

        public bool IsGrabbing()
        {
            return SpatialInputManager.instance.grabDesktop.action.ReadValue<float>() > 0.5f;
        }

        public Transform TrackerTransform(TrackerType type) {
            switch(type) {
                case TrackerType.head:
                    return cameraHolder;
                case TrackerType.rightHand:
                    return reachTarget;
            }

            return null;
        }

        public TrackingMode GetTrackingMode()
        {
            return TrackingMode.OnePoint;
        }

        public void UpdateTrackers(TrackerPos[] trackers)
        {
            trackers[0].enabled = true;
            trackers[0].translation = Quaternion.Inverse(transform.rotation) * (neckPivot.position - transform.position);
            trackers[0].rotation = Quaternion.Inverse(transform.rotation) * neckPivot.rotation;
            
            trackers[2].enabled = IsReaching();
            trackers[2].translation = Quaternion.Inverse(transform.rotation) * (reachTarget.position - transform.position);
            trackers[2].rotation = Quaternion.Inverse(transform.rotation) * reachTarget.rotation;
        }

        public Vector3 TrackerVelocity(TrackerType type)
        {
            return Vector3.zero;
        }

        public Vector3 TrackerAngularVelocity(TrackerType type)
        {
            return Vector3.zero;
        }

    }
}
