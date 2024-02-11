using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public abstract class Avatar : MonoBehaviour
    {
        public TrackingMode trackingMode;
        protected Player player;
        
        protected virtual void Awake()
        {
            player = GetComponentInParent<Player>();
        }

        public virtual void Start()
        {
            
        }

        public virtual void SetTrackingMode(TrackingMode mode)
        {
            trackingMode = mode;
        }
        public abstract void SetVirtualVelocity(Vector3 velocity);
    }
}

