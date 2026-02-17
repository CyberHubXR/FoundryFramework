using UnityEngine;
using System.Collections.Generic;
using Foundry.Networking;
using Foundry.RotationExtensions;

namespace Foundry
{
    [RequireComponent(typeof(BoxCollider))]
    public class SpatialDoor : SpatialTouchable
    {
        BoxCollider doorCollider;

        [Header("Door Options")]
        [Tooltip("A toggle to decide if the door closes by itself, almost like the door is weighted")]
        public bool closeDoorAutomatically = false;

        [Tooltip("How much “friction” is on the hinges, Higher numbers = bigger jumps in rotation when pushing")]
        public float doorHingeFriction = 10F;

        [Tooltip("How fast the door will close if closeDoorAutomatically")]
        public float doorHingeCloseFriction = 1F;

        [Tooltip("Is the door locked")] public bool doorIsLocked = false;

        [Header("Door Limits")] [Tooltip("Max angle the door can rotate too")]
        public float doorMaxAngle = 90F;

        [Tooltip("Minimum rotation the door can rotate too")]
        public float doorMinAngle = -90F;

        [Header("Setup")]
        [Tooltip(
            "The pivot / hinge position in local space relative to the door (gets converted to world space at runtime)")]
        public Vector3 doorPivot;

        [HideInInspector] public bool doorIsOpen = false;
        [HideInInspector] public float doorOpenAmount;

        //internal variables
        Transform doorPushPoint;
        SpatialTouch doorPusher;
        float doorPushDistance;

        bool touchingDoor;

        private void OnValidate()
        {
            doorCollider = GetComponent<BoxCollider>();
            doorCollider.isTrigger = true;
        }

        new private void Start()
        {
            doorPivot += transform.position;

            doorPushPoint = new GameObject("Door Push Point").transform;
            doorPushPoint.parent = transform;
        }

        public override void StartTouch(SpatialTouch spatialTouch)
        {
            base.StartTouch(spatialTouch);

            if (doorPusher == null)
            {
                doorPushPoint.position = spatialTouch.transform.position;
                doorPusher = spatialTouch;
            }
        }

        /// <summary>
        ///  A simple public method to set the door to be unlocked (doorIsLocked = false)
        /// <code>
        /// This is a virtual method and can be overwridden in a inherited class to add custom functionality.
        /// 
        /// public class SpaceShipDoor : SpatialDoor {
        ///     public override void UnlockDoorSpaceship () 
        ///     {
        ///         Base.UnlockDoor();
        ///         //custom functionality here
        ///     }
        /// }
        /// </code>
        /// </summary>
        public virtual void UnlockDoor()
        {
            doorIsLocked = false;
        }

        /// <summary>
        ///  A simple public method to set the door to be unlocked (doorIsLocked = false)
        /// <code>
        /// This is a virtual method and can be overwridden in a inherited class to add custom functionality.
        /// 
        /// public class SpaceShipDoor : SpatialDoor {
        ///     public override void LockDoorSpaceship () 
        ///     {
        ///         Base.LockDoor();
        ///         //custom functionality here
        ///     }
        /// }
        /// </code>
        /// </summary>
        public virtual void LockDoor()
        {
            doorIsLocked = true;
        }

        void CalculateDoorAngle(SpatialTouch hand)
        {
            if (doorIsLocked) return;

            //hands offset to pivot
            Vector3 angleOffset = hand.transform.position - doorPivot;
            angleOffset.y = 0;

            Quaternion doorAngle = Quaternion.LookRotation(angleOffset);

            float wrappedAngle = RotationHelperExtensions.WrapAngle(doorAngle.eulerAngles.y);
            float angleDifference = wrappedAngle - doorOpenAmount;

            Vector3 angleCross = Vector3.Cross(transform.right, angleOffset);
            if (angleCross.y > 0)
            {
                angleDifference = -angleDifference;
            }

            if (doorOpenAmount < doorMaxAngle && doorOpenAmount > doorMinAngle)
                RotationHelperExtensions.RotateAroundLerp(transform, doorPivot, Vector3.up, angleDifference,
                    doorHingeFriction);

        }

        private void Update()
        {
            if (doorPusher != null)
            {
                touchingDoor = true;

                doorPushDistance = Vector3.Distance(doorPushPoint.position, doorPusher.transform.position);

                CalculateDoorAngle(doorPusher);

                if (doorPushDistance > 0.2F)
                {
                    doorPusher = null;
                    touchingDoor = false;
                }
            }

            doorOpenAmount = RotationHelperExtensions.WrapAngle(transform.localEulerAngles.y);

            if (doorOpenAmount > -2 && doorOpenAmount < 2)
            {
                doorIsOpen = false;
            }
            else
            {
                doorIsOpen = true;
            }

            if (!touchingDoor && doorIsOpen && closeDoorAutomatically)
            {
                float t = Time.deltaTime * (doorHingeCloseFriction * .1F);
                float rotationAngle = Mathf.LerpAngle(0, -doorOpenAmount, t);

                transform.RotateAround(doorPivot, Vector3.up, rotationAngle);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0, 0, .9F);

            if (Application.isPlaying)
            {
                Gizmos.DrawCube(doorPivot, new Vector3(0.1F, doorCollider.size.y * 1.005F, 0.125F));
            }
            else
            {
                Gizmos.DrawCube(transform.position + doorPivot,
                    new Vector3(0.1F, doorCollider.size.y * 1.005F, 0.125F));
            }

            Gizmos.color = new Color(0, 1, 0, 0.2F);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(doorCollider.center, doorCollider.size * 1.005F);
        }
    }
}