using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    [RequireComponent(typeof(TeleportPoint))]
    public class TeleportPointAnimation : InteractionAnimations
    {
        TeleportPoint teleportPoint;

        protected override void OnEnable() {
            base.OnEnable();
            teleportPoint = GetComponent<TeleportPoint>();
            teleportPoint.StartHighlight.AddListener(StartHighlight);
            teleportPoint.StopHighlight.AddListener(StopHighlight);
        }

        protected override void OnDisable() {
            base.OnDisable();
            teleportPoint.StartHighlight.RemoveListener(StartHighlight);
            teleportPoint.StopHighlight.RemoveListener(StopHighlight);
        }

        void StartHighlight(TeleportPoint teleportPoint, TeleportRaycaster teleporter) {
            highlightStartTime = Time.time;
            highlighting = true;
        }

        void StopHighlight(TeleportPoint teleportPoint, TeleportRaycaster teleporter) {
            highlightStopTime = Time.time;
            highlighting = false;
        }

    }
}
