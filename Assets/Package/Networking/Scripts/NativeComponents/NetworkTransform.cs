using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Foundry.Networking
{
    
    [DisallowMultipleComponent]
    public class NetworkTransform : NetworkComponent
    {
        public bool localTransform = true;

        public NetworkProperty<Vector3> netPosition;
        private Vector3 lastUpdateTime;
        private float lerpDuration => 1f / NetworkManager.TickRate;
        private Vector3 posLerpStart;
        private Vector3 posLerpEnd;
        public NetworkProperty<Quaternion> netRotation;
        private Quaternion rotLerpStart;
        private Quaternion rotLerpEnd;
        public NetworkProperty<Vector3> netScale;
        
        public Vector3 currentVelocity;
        public Vector3 currentAngularVelocity;

        public Vector3 position
        {
            get => localTransform ? transform.localPosition : transform.position;
            set
            {
                if (localTransform)
                    transform.localPosition = value;
                else
                    transform.position = value;
            }
        }
        
        public Quaternion rotation
        {
            get => localTransform ? transform.localRotation : transform.rotation;
            set
            {
                if (localTransform)
                    transform.localRotation = value;
                else
                    transform.rotation = value;
            }
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;
            currentVelocity = Vector3.zero;
            currentAngularVelocity = Vector3.zero;
            if (IsOwner)
            {
                netPosition.Value = this.position;
                netRotation.Value = this.rotation;
            }
        }

        public override void OnConnected()
        {
            lastUpdateTime.x = Time.time;
            lastUpdateTime.y = Time.time;
            netPosition.OnValueChanged += (v) =>
            {
                if (lastUpdateTime.x != Time.time)
                {
                    lastUpdateTime.x = Time.time;
                }
                posLerpStart = posLerpEnd;
                posLerpEnd = v;
            };
            netRotation.OnValueChanged += (v) =>
            {
                if (lastUpdateTime.y != Time.time)
                {
                    lastUpdateTime.y = Time.time;
                }
                rotLerpStart = rotLerpEnd;
                rotLerpEnd = v;
            };
            if (!IsOwner)
            {
                position = netPosition.Value;
                rotation = netRotation.Value;
                posLerpStart = netPosition.Value;
                posLerpEnd = netPosition.Value;
                rotLerpStart = netRotation.Value;
                rotLerpEnd = netRotation.Value;
            }
        }

        public override void RegisterProperties(List<INetworkProperty> props, List<INetworkEvent> events)
        {
            netPosition.Value = transform.position;
            props.Add(netPosition);
            netRotation.Value = transform.rotation;
            props.Add(netRotation);
            netScale.Value = transform.localScale;
            props.Add(netScale);
        }

        void Update()
        {
            if (IsOwner)
            {
                netPosition.Value = position;
                netRotation.Value = rotation;
                netScale.Value = transform.localScale;
            }
            else
            {
                var timeSinceLastUpdatePos = Time.time - lastUpdateTime.x;
                var timeSinceLastUpdateRot = Time.time - lastUpdateTime.y;
                
                Vector3 targetPos = Vector3.Lerp(posLerpStart, posLerpEnd, timeSinceLastUpdatePos / lerpDuration);
                Quaternion targetRot = Quaternion.Slerp(rotLerpStart, rotLerpEnd, timeSinceLastUpdateRot / lerpDuration);
                
                position = Vector3.SmoothDamp(position, targetPos, ref currentVelocity, lerpDuration);
                rotation = Quaternion.Euler(smoothDampAngle(rotation.eulerAngles, targetRot.eulerAngles, ref currentAngularVelocity, lerpDuration));
                transform.localScale = netScale.Value;
            }
        }
        
        Vector3 smoothDampAngle(Vector3 current, Vector3 target, ref Vector3 currentVelocity, float smoothTime)
        {
            return new Vector3(
                Mathf.SmoothDampAngle(current.x, target.x, ref currentVelocity.x, smoothTime),
                Mathf.SmoothDampAngle(current.y, target.y, ref currentVelocity.y, smoothTime),
                Mathf.SmoothDampAngle(current.z, target.z, ref currentVelocity.z, smoothTime)
            );
        }
    }
    
}
