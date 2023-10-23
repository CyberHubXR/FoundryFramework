using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry.Networking
{
    /// <summary>
    /// Base class for components that are networked.
    /// </summary>
    public abstract class NetworkComponent : FoundryScript
    {
        /// <summary>
        /// The network object that owns this component.
        /// </summary>
        public NetworkObject Object { get; set; }
        
        /// <summary>
        /// The active network provider.
        /// </summary>
        public INetworkProvider NetworkProvider => Object.NetworkProvider;
        
        /// <summary>
        /// If this component is owned by the local client, and thus can set properties.
        /// </summary>
        public bool IsOwner => !Object || Object.IsOwner;

        /// <summary>
        /// Called once on startup to register properties for this component.
        /// </summary>
        /// <param name="props"></param>
        public virtual void RegisterProperties(List<INetworkProperty> props)
        {
            
        }
        
        /// <summary>
        /// Called once we are connected to the network and all network objects are in a valid state.
        /// </summary>
        public virtual void OnConnected()
        {
            
        }

        
        protected virtual void OnValidate()
        {
            #if UNITY_EDITOR
            if (Object == null)
            {
                Object = GetComponentInParent<NetworkObject>();
                if(Object)
                    Object.UpdateComponents();
            }
            #endif
        }
    }
}
