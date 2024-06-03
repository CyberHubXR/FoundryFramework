using Foundry.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Foundry
{
    [AddComponentMenu("Foundry/Interaction/SpatialTouchable")]
    public class SpatialTouchable : NetworkComponent
    {
        public float touchStayDelay;
        [Space(10)]
        public NetworkEvent<bool> OnStartTouch;
        public NetworkEvent<bool> OnStopTouch;
        public NetworkEvent<bool> OnTouchStay;
        
        public bool takeOwnershipOnTouch = true;

        public float touchTriggerPercent { get; set; }
        public bool isTouching { get { return touchTriggerPercent > 0; } }
        public bool isTouchStay { get { return touchTriggerPercent >= 1; } }

        // internal
        float delayTimer;

        protected virtual void Start()
        {
            delayTimer = touchStayDelay;
        }

        public virtual void StartTouch(SpatialTouch spatialTouch) 
        {
            if (takeOwnershipOnTouch && !IsOwner)
            {
                var playerObject = spatialTouch.SpatialHand.Object;
                if (playerObject.Owner == NetworkManager.State.localPlayerId)
                    Object.RequestOwnership();
            }
            if (IsOwner)
                OnStartTouch.Invoke(isTouching);
        }

        public virtual void StopTouch(SpatialTouch spatialTouch)
        {
            if (!IsOwner)
                return;
            touchTriggerPercent = 0;
            OnStopTouch.Invoke(isTouching);
            delayTimer = touchStayDelay;
        }

        public virtual void TouchStay(SpatialTouch spatialTouch)
        {
            if (!IsOwner)
                return;
            OnTouchStay.Invoke(isTouchStay);
        }

        public override void RegisterProperties(List<INetworkProperty> props, List<INetworkEvent> events)
        {
            events.Add(OnStartTouch);
            events.Add(OnStopTouch);
            events.Add(OnTouchStay);
        }

        public virtual void TouchUpdate(SpatialTouch spatialTouch)
        {
            if (!IsOwner)
                return;
            delayTimer -= Time.deltaTime;
            touchTriggerPercent = Mathf.Clamp((touchStayDelay - delayTimer) / touchStayDelay, 0, 1);

            if (delayTimer <= 0)
                TouchStay(spatialTouch);
        }
    }
}
