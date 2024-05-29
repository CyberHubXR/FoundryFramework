using System;
using System.Collections;
using System.Collections.Generic;
using CyberHub.Brane;
using UnityEngine;

namespace Foundry.Networking
{
    
    [DisallowMultipleComponent]
    public class NetworkTransform : NetworkComponent
    {
        public Transform lerpObject;

        public NetworkProperty<Vector3> position;
        public NetworkProperty<Quaternion> rotation;
        public NetworkProperty<Vector3> scale;

        public void Start()
        {
            if (!lerpObject)
                lerpObject = transform;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            
        }

        public override void OnConnected()
        {
            
        }

        public override void RegisterProperties(List<INetworkProperty> props, List<INetworkEvent> events)
        {
            position.Value = transform.position;
            props.Add(position);
            rotation.Value = transform.rotation;
            props.Add(rotation);
            scale.Value = transform.localScale;
            props.Add(scale);
        }

        void Update()
        {
            if (IsOwner)
            {
                position.Value = transform.position;
                rotation.Value = transform.rotation;
                scale.Value = transform.localScale;
            }
            else
            {
                transform.position = position.Value;
                transform.rotation = rotation.Value;
                transform.localScale = scale.Value;
            }
        }
    }
    
}
