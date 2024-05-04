using System;
using System.Collections.Generic;
using CyberHub.Brane;
using UnityEngine;

namespace Foundry.Networking
{
    /// <summary>
    /// Base class for networked transform properties. Network providers will implement a custom property drawer for this.
    /// </summary> 
    [Serializable]
    public class NetworkTransformProperties : MappedProperties
    {
        
    }
    
    /// <summary>
    /// MonoBehaviour base for network providers to implement to provide a network transform api.
    /// This must be a MonoBehaviour so that references are not lost when the network object is instantiate
    /// </summary>
    public abstract class NetworkTransformAPI : MonoBehaviour
    {
        public abstract void Teleport(Vector3 position, Quaternion rotation);

        public abstract void OnConnected(NetworkTransform netTransform);
    }
    
    [DisallowMultipleComponent]
    public class NetworkTransform : NetworkComponent
    {
        public NetworkTransformProperties props;
        public Transform lerpObject;
        
        [HideInInspector]
        public MonoBehaviour nativeScript;
        
        /// <summary>
        /// When this object is bound to a network object, this will be set to the api for that object.
        /// </summary>
        [HideInInspector]
        public NetworkTransformAPI api;

        public void Start()
        {
            if (!lerpObject)
                lerpObject = transform;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if(api == null)
                Debug.LogWarning("Teleport called on NetworkTransform before api was set! Make sure you wait until OnConnected is called before calling Teleport.");
            api.Teleport(position, rotation);
        }

        public override void OnConnected()
        {
            api.OnConnected(this);
        }
    }
    
}
