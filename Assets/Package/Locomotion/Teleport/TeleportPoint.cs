using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Foundry
{

    public class TeleportPoint : MonoBehaviour {
        public Transform teleportPoint;
        public bool alwaysShow = false;
        public bool matchPoint = true;
        public bool matchDirection = true;

        public UnityEvent<TeleportPoint, TeleportRaycaster> StartHighlight;
        public UnityEvent<TeleportPoint, TeleportRaycaster> StopHighlight;
        public UnityEvent<TeleportPoint, TeleportRaycaster> OnTeleport;


        public void Awake() {
            if(teleportPoint == null)
                teleportPoint = transform;
        }


        public virtual void StartHighlighting(TeleportRaycaster raycaster) {
            StartHighlight.Invoke(this, raycaster);
        }

        public virtual void StopHighlighting(TeleportRaycaster raycaster) {
            StopHighlight.Invoke(this, raycaster);
        }

        public virtual void Teleport(TeleportRaycaster raycaster) {
            OnTeleport.Invoke(this, raycaster);
        }
    }
}
