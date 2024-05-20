#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

using UnityEngine.Events;

using Foundry.RotationExtensions;
using System.Collections.Generic;
using Foundry.Networking;

namespace Foundry
{
    public class SpatialLever : SpatialTouchable
    {
        [Header("Lever Setup")]
        public Vector3 leverPivot;
        
        [Header("Lever Options")]
        public float leverMaxAngle = 90F;
        public float leverMinAngle = -90F;
        public float leverHingeFriction = 10F;
        public bool positiveOnly;

        [Header("Output")]
        public NetworkEvent<float> leverEvent; 

        private Vector3 leverPivotWorld;
        private Vector3 startObjectUp;

        [Range(-1, 1)] public float leverTurnAmount;

        private SpatialInputManager spatialInputManager;
        private bool grabbing;

        #if UNITY_EDITOR
        private void OnValidate()
        {
            leverPivotWorld = leverPivot + transform.position;
        }
        #endif

        new void Start() 
        {
            leverPivotWorld = leverPivot + transform.position;

            spatialInputManager = SpatialInputManager.instance;
            startObjectUp = transform.up;
        }

        public override void RegisterProperties(List<INetworkProperty> props, List<INetworkEvent> events)
        {
            events.Add(leverEvent);
        }

        public override void TouchUpdate(SpatialTouch spatialTouch)
        {
            base.TouchUpdate(spatialTouch);

            if (spatialTouch.SpatialHand.handType == SpatialHand.HandType.Right && spatialInputManager.grabRightXR.action.ReadValue<float>() > 0.5F || spatialTouch.SpatialHand.handType == SpatialHand.HandType.Left && spatialInputManager.grabLeftXR.action.ReadValue<float>() > 0.5F || spatialInputManager.grabDesktop.action.ReadValue<float>() > 0.5F)
            {
                grabbing = true;
                CalculateLeverAngle(spatialTouch);
            }
            else 
            {
                grabbing = false;
            }
        }

        void CalculateLeverAngle(SpatialTouch spatialTouch) 
        {
            Vector3 direction = spatialTouch.transform.position - leverPivotWorld;

            Quaternion leverAngle = Quaternion.LookRotation(transform.forward, direction);

            float leverOffset = RotationHelperExtensions.WrapAngle(leverAngle.eulerAngles.z) - RotationHelperExtensions.WrapAngle(transform.eulerAngles.z);
            float angleClampValue = Vector3.Angle(transform.up, startObjectUp);

            if (angleClampValue < leverMaxAngle && angleClampValue > leverMinAngle)
                RotationHelperExtensions.RotateAroundCustom(transform, leverPivotWorld, transform.forward, leverOffset);
        }

        private void Update()
        {
            ResetLever();
        }

        void ResetLever() 
        {
            if (!grabbing)
            {
                RotationHelperExtensions.RotateAroundLerp(transform, leverPivotWorld, transform.forward, -RotationHelperExtensions.WrapAngle(transform.eulerAngles.z), leverHingeFriction);
            }

            if (!isTouching)
            {
                RotationHelperExtensions.RotateAroundLerp(transform, leverPivotWorld, transform.forward, -RotationHelperExtensions.WrapAngle(transform.eulerAngles.z), leverHingeFriction);
            }

            leverTurnAmount = ComputeLeverAngle();
            leverEvent.Invoke(leverTurnAmount);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(leverPivotWorld, 0.1f);
        }

        float ComputeLeverAngle()
        {
            float rawAngle = Vector3.Angle(startObjectUp, transform.up);
            Vector3 leverAngleCross = Vector3.Cross(startObjectUp, transform.up);
            if (leverAngleCross.z > 0 && !positiveOnly) rawAngle *= -1;
            rawAngle = rawAngle / (leverMaxAngle - leverMinAngle);

            if (!positiveOnly) { return Mathf.Clamp(rawAngle * 2, -1, 1);  } else { return Mathf.Clamp(rawAngle * 2, 0, 1); }
        }
    }
}