using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    [RequireComponent(typeof(TeleportPoint))]
    public class TeleportPointDebugger : MonoBehaviour
    {
        TeleportPoint teleportPoint;

        private void OnEnable() {
            teleportPoint = GetComponent<TeleportPoint>();
            teleportPoint.StartHighlight.AddListener(OnHighlight);
            teleportPoint.StopHighlight.AddListener(StopHighlight);
            teleportPoint.OnTeleport.AddListener(OnTeleport);

        }

        private void OnDisable() {
            teleportPoint.StartHighlight.RemoveListener(OnHighlight);
            teleportPoint.StopHighlight.RemoveListener(StopHighlight);
            teleportPoint.OnTeleport.RemoveListener(OnTeleport);
        }

        void OnHighlight(TeleportPoint point, TeleportRaycaster teleporter) {
            Debug.Log("Starting Highlight " + point.name + " - From: " + teleporter.name, teleporter);
        }
        void StopHighlight(TeleportPoint point, TeleportRaycaster teleporter) {
            Debug.Log("Stopping Highlight " + point.name + " - From: " + teleporter.name, teleporter);

        }
        void OnTeleport(TeleportPoint point, TeleportRaycaster teleporter) {
            Debug.Log("Teleporting " + point.name + " - From: " + teleporter.name, teleporter);
        }
    }
}
